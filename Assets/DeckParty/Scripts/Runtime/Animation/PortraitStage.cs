using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty
{
    /// <summary>The animation states a character controller can have. Missing states fall back (see <see cref="CharacterAnimDriver"/>).</summary>
    public enum CharacterState
    {
        /// <summary>Viewers without an action (and the fallback for everything).</summary>
        Idle,
        /// <summary>Viewers with an action that hasn't been used tonight (also the shop, Audience and pickers).</summary>
        IdlePreActive,
        /// <summary>Plays once when the action is used, then moves to <see cref="IdlePostActive"/>.</summary>
        Active,
        /// <summary>After the action has been used tonight.</summary>
        IdlePostActive,
        /// <summary>The end-of-stream scoring sequence on successful streams.</summary>
        Score,
        /// <summary>Optional: plays once when the viewer joins chat during a stream.</summary>
        Enter,
        /// <summary>Optional: kicked or sent back to the queue.</summary>
        Exit,
        /// <summary>Optional: hit by another viewer's action.</summary>
        Hit,
        /// <summary>Optional: victory.</summary>
        Celebrate,
        /// <summary>Streamers: picked on the select screen.</summary>
        Select,
        /// <summary>Streamers: one Swatting Risk from being swatted.</summary>
        Anxious,
    }

    /// <summary>
    /// Renders animated characters (rigged PSB prefabs, sprite-swap prefabs, or a controller animating the portrait) for
    /// the UI. Rigged sprites can't draw inside a canvas, so each visible animated card rents a cell of one shared render
    /// texture: its character instance stands in that cell on a hidden layer, one camera renders them all, and the card's
    /// RawImage shows its cell. Cards without animation cost nothing.
    /// </summary>
    public sealed class PortraitStage : MonoBehaviour
    {
        public const string LayerName = "CardPortraits";
        const float Spacing = 10f;
        const float FillFraction = 0.9f;

        [Tooltip("Optional unlit sprite material, so characters render at full brightness without 2D lights.")]
        public Material unlitMaterial;
        [Tooltip("Cell resolution below 1440p (doubles to 1.5x at 1440p and above).")]
        public int cellSize = 256;

        static PortraitStage s_Instance;
        static bool s_Failed;

        Camera m_Camera;
        RenderTexture m_Texture;
        int m_Columns;
        int m_CellSize;
        readonly List<Slot> m_Slots = new List<Slot>();
        int m_Layer;

        public sealed class Slot
        {
            public int Cell;
            public GameObject Instance;
            public CharacterAnimDriver Driver;
            public Rect Uv;
        }

        public static PortraitStage Instance
        {
            get
            {
                if (s_Instance != null || s_Failed) return s_Instance;
                var prefab = GameDatabase.Instance.portraitStage;
                GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("PortraitStage");
                go.name = "PortraitStage";
                DontDestroyOnLoad(go);
                s_Instance = go.GetComponent<PortraitStage>();
                if (s_Instance == null) s_Instance = go.AddComponent<PortraitStage>();
                if (!s_Instance.Setup())
                {
                    s_Failed = true;
                    Destroy(go);
                    s_Instance = null;
                }
                return s_Instance;
            }
        }

        bool Setup()
        {
            m_Layer = LayerMask.NameToLayer(LayerName);
            if (m_Layer < 0) m_Layer = 31;

            m_CellSize = Screen.height >= 1400 ? Mathf.RoundToInt(cellSize * 1.5f) : cellSize;
            m_Columns = m_CellSize > 256 ? 6 : 8;
            int size = m_Columns * m_CellSize;
            m_Texture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32) { name = "PortraitStageRT", antiAliasing = 1 };
            if (!m_Texture.Create()) return false;

            m_Camera = GetComponentInChildren<Camera>();
            if (m_Camera == null)
            {
                var cameraGo = new GameObject("StageCamera");
                cameraGo.transform.SetParent(transform, false);
                m_Camera = cameraGo.AddComponent<Camera>();
            }
            transform.position = new Vector3(0f, 10000f, 0f);
            m_Camera.orthographic = true;
            m_Camera.orthographicSize = m_Columns * Spacing * 0.5f;
            m_Camera.transform.localPosition = new Vector3(m_Columns * Spacing * 0.5f, -m_Columns * Spacing * 0.5f, -10f);
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            m_Camera.cullingMask = 1 << m_Layer;
            m_Camera.targetTexture = m_Texture;
            m_Camera.depth = -50f;
            m_Camera.allowHDR = false;
            m_Camera.allowMSAA = false;
            m_Camera.enabled = false;
            return true;
        }

        public Texture Texture => m_Texture;

        /// <summary>Spawns <paramref name="art"/>'s character in a free cell. Null when the stage is full (the card falls back to its sprite).</summary>
        public Slot Rent(GameObject prefab, RuntimeAnimatorController controller, Sprite fallbackSprite, float zoom, Vector2 offset)
        {
            int cell = FreeCell();
            if (cell < 0) return null;

            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, transform);
            }
            else
            {
                // Controller only: it animates a sprite renderer at the root (author sprite-swap clips on a SpriteRenderer).
                instance = new GameObject("Portrait", typeof(SpriteRenderer));
                instance.transform.SetParent(transform, false);
                instance.GetComponent<SpriteRenderer>().sprite = fallbackSprite;
            }
            instance.name = "Cell " + cell;
            SetLayer(instance, m_Layer);

            var animator = instance.GetComponentInChildren<Animator>();
            if (animator == null && controller != null) animator = instance.AddComponent<Animator>();
            if (animator != null)
            {
                if (controller != null) animator.runtimeAnimatorController = controller;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            if (unlitMaterial != null)
                foreach (var r in instance.GetComponentsInChildren<SpriteRenderer>(true)) r.sharedMaterial = unlitMaterial;

            Place(instance, cell, zoom, offset);
            var driver = instance.GetComponent<CharacterAnimDriver>();
            if (driver == null) driver = instance.AddComponent<CharacterAnimDriver>();
            var slot = new Slot
            {
                Cell = cell,
                Instance = instance,
                Driver = driver,
                Uv = CellUv(cell),
            };
            slot.Driver.Bind(animator);
            m_Slots.Add(slot);
            m_Camera.enabled = true;
            return slot;
        }

        public void Release(Slot slot)
        {
            if (slot == null) return;
            m_Slots.Remove(slot);
            if (slot.Instance != null) Destroy(slot.Instance);
            if (m_Camera != null) m_Camera.enabled = m_Slots.Count > 0;
        }

        int FreeCell()
        {
            int total = m_Columns * m_Columns;
            for (int cell = 0; cell < total; cell++)
            {
                bool used = false;
                foreach (var slot in m_Slots)
                    if (slot.Cell == cell) { used = true; break; }
                if (!used) return cell;
            }
            return -1;
        }

        Vector3 CellCentre(int cell)
        {
            int col = cell % m_Columns, row = cell / m_Columns;
            return new Vector3((col + 0.5f) * Spacing, -(row + 0.5f) * Spacing, 0f);
        }

        Rect CellUv(int cell)
        {
            int col = cell % m_Columns, row = cell / m_Columns;
            float size = 1f / m_Columns;
            return new Rect(col * size, 1f - (row + 1) * size, size, size);
        }

        /// <summary>Fits the character's sprites into its cell (bottom-anchored), then applies the database framing.</summary>
        void Place(GameObject instance, int cell, float zoom, Vector2 offset)
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale = Vector3.one;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                instance.transform.localPosition = CellCentre(cell);
                return;
            }
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, 0.001f);
            float scale = Spacing * FillFraction / largest * Mathf.Max(0.05f, zoom);
            instance.transform.localScale = Vector3.one * scale;

            var centre = CellCentre(cell);
            var boundsCentre = (bounds.center - transform.position) * scale;
            instance.transform.localPosition = centre - new Vector3(boundsCentre.x, boundsCentre.y, 0f) + new Vector3(offset.x, offset.y, 0f) * Spacing;
        }

        static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayer(child.gameObject, layer);
        }

        void OnDestroy()
        {
            if (m_Texture != null)
            {
                m_Texture.Release();
                Destroy(m_Texture);
            }
            if (s_Instance == this) s_Instance = null;
        }
    }

    /// <summary>
    /// Plays a character's states by name so controllers need no parameters. One-shot states (Active, Score, Enter, Exit,
    /// Hit, Celebrate, Select) return to the right idle when they finish, whether or not the controller has transitions.
    /// </summary>
    public sealed class CharacterAnimDriver : MonoBehaviour
    {
        Animator m_Animator;
        CharacterState m_Idle = CharacterState.Idle;
        bool m_OneShot;
        int m_OneShotHash;

        public static string StateName(CharacterState state)
        {
            switch (state)
            {
                case CharacterState.IdlePreActive: return "Idle_Pre_Active";
                case CharacterState.IdlePostActive: return "Idle_Post_Active";
                default: return state.ToString();
            }
        }

        static CharacterState[] Fallbacks(CharacterState state)
        {
            switch (state)
            {
                case CharacterState.IdlePreActive: return new[] { CharacterState.IdlePreActive, CharacterState.Idle };
                case CharacterState.IdlePostActive: return new[] { CharacterState.IdlePostActive, CharacterState.Idle };
                case CharacterState.Score: return new[] { CharacterState.Score, CharacterState.Celebrate };
                case CharacterState.Celebrate: return new[] { CharacterState.Celebrate, CharacterState.Score };
                case CharacterState.Anxious: return new[] { CharacterState.Anxious, CharacterState.Idle };
                default: return new[] { state };
            }
        }

        static bool IsOneShot(CharacterState state) =>
            state == CharacterState.Active || state == CharacterState.Score || state == CharacterState.Enter || state == CharacterState.Exit ||
            state == CharacterState.Hit || state == CharacterState.Celebrate || state == CharacterState.Select;

        public void Bind(Animator animator) => m_Animator = animator;

        /// <summary>The state to rest in (Idle, Idle_Pre_Active, Idle_Post_Active...). Applied now unless a one-shot is playing.</summary>
        public void SetIdle(CharacterState idle)
        {
            m_Idle = idle;
            if (!m_OneShot) Play(idle);
        }

        /// <summary>Plays a state if the controller has it (or a fallback). Returns false when nothing matched.</summary>
        public bool Play(CharacterState state)
        {
            if (m_Animator == null || m_Animator.runtimeAnimatorController == null) return false;
            foreach (var candidate in Fallbacks(state))
            {
                int hash = Animator.StringToHash(StateName(candidate));
                if (!m_Animator.HasState(0, hash)) continue;
                m_Animator.CrossFadeInFixedTime(hash, 0.08f, 0);
                m_OneShot = IsOneShot(state);
                m_OneShotHash = hash;
                if (state == CharacterState.Active) m_Idle = CharacterState.IdlePostActive;
                return true;
            }
            return false;
        }

        void Update()
        {
            if (!m_OneShot || m_Animator == null) return;
            var info = m_Animator.GetCurrentAnimatorStateInfo(0);
            if (m_Animator.IsInTransition(0) || info.shortNameHash != m_OneShotHash) return;
            if (info.normalizedTime >= 1f)
            {
                m_OneShot = false;
                Play(m_Idle);
            }
        }
    }
}
