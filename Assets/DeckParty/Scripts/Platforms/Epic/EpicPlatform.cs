// Epic code only exists when the EOS Plugin for Unity is installed (DECKPARTY_EOS, a version define in DeckParty.Epic.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_EOS && UNITY_STANDALONE
using System;
using Epic.OnlineServices;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace DeckParty.Platforms.Epic
{
    /// <summary>
    /// Epic Online Services behind <see cref="IOnlinePlatform"/>, on the EOS Plugin for Unity (package
    /// com.playeveryware.eos). The plugin's EOSManager starts and ticks the SDK with the product settings from
    /// EOS Plugin > EOS Configuration; this class signs the player in (<c>EpicPlatform.Login.cs</c>), runs EOS lobbies
    /// (<c>EpicPlatform.Lobbies.cs</c>), friends, invites, presence and the Epic overlay (<c>EpicPlatform.Friends.cs</c>)
    /// and hands Netcode its transport (<see cref="EpicP2PTransport"/>).
    /// <para>
    /// Signing in is asynchronous, so <see cref="Initialize"/> returns while it's still going (<see cref="IsSigningIn"/>) and
    /// <see cref="ReadyChanged"/> fires when it's done. At start-up only the silent ways are tried (the Epic Games
    /// Launcher's exchange code, the remembered login, the Developer Authentication Tool); the first time the
    /// Multiplayer screen opens, the account portal (a login window) and then this device's id.
    /// </para>
    /// <para>
    /// User ids in this platform: product user ids (EOS Connect) for lobby members, connections and players; Epic account
    /// ids for friends (invites look up the friend's product user id).
    /// </para>
    /// </summary>
    public sealed partial class EpicPlatform : IOnlinePlatform
    {
        /// <summary>The running platform (the transport needs the local product user id).</summary>
        internal static EpicPlatform Instance { get; private set; }

        EpicPlatformSettings m_Settings;
        bool m_SdkRunning;
        bool m_Ready;
        bool m_SigningIn;
        bool m_FinalFailure;
        bool m_StartupSignInDone;
        /// <summary>The Multiplayer screen asked for a sign-in while a silent one was running.</summary>
        bool m_InteractiveWanted;
        string m_LastError;
        PlatformUser m_LocalUser;
        ProductUserId m_LocalProductUser;
        EpicAccountId m_LocalAccount;
        string m_ProductId;
        EpicPump m_Pump;
        bool m_FriendsDirty;
        float m_NextFriendsEvent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            Instance = null;
            OnlinePlatforms.Register(OnlinePlatformKind.Epic, () => new EpicPlatform());
        }

        public OnlinePlatformKind Kind => OnlinePlatformKind.Epic;
        public string DisplayName => "Epic Games";

        /// <summary>Friends, the overlay's invites and presence need an Epic account; a device-id sign-in only has lobbies and play.</summary>
        public PlatformFeatures Features
        {
            get
            {
                var features = PlatformFeatures.Lobbies | PlatformFeatures.LobbySearch | PlatformFeatures.QuickMatch;
                if (HasEpicAccount) features |= PlatformFeatures.Friends | PlatformFeatures.InviteOverlay | PlatformFeatures.RichPresence;
                return features;
            }
        }

        /// <summary>Signed in, and the SDK is still up (it shuts down on its own when the application quits).</summary>
        public bool IsReady => m_Ready && SdkRunning;
        public bool IsSigningIn => m_SigningIn;
        public string LastError => m_LastError;
        public bool InitFailureIsFinal => m_FinalFailure;
        public PlatformUser LocalUser => m_LocalUser;

        /// <summary>The SDK is up (EOSManager created the platform interface and hasn't shut it down).</summary>
        internal bool SdkRunning => m_SdkRunning && PlatformInterfaceOrNull() != null;

        /// <summary>
        /// One of the SDK's interfaces through EOSManager, or null when the SDK isn't up. EOSManager's getters can throw
        /// (e.g. its native render plugin failed at start-up on Windows), so every call in this assembly goes through here.
        /// </summary>
        internal static T Sdk<T>(Func<EOSManager.EOSSingleton, T> getter) where T : class
        {
            try
            {
                return getter(EOSManager.Instance);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static global::Epic.OnlineServices.Platform.PlatformInterface PlatformInterfaceOrNull() => Sdk(m => m.GetEOSPlatformInterface());
        internal ProductUserId LocalProductUser => m_LocalProductUser;
        internal bool HasEpicAccount => m_LocalAccount != null && m_LocalAccount.IsValid();

        public event Action ReadyChanged;
        public event Action FriendsChanged;
        public event Action<string> JoinRequested;

        /// <summary>EOS has no avatar images, so this never fires.</summary>
        public event Action<string> AvatarLoaded
        {
            add { }
            remove { }
        }

        // ================================================================== start-up and shutdown

        public bool Initialize(OnlinePlatformSettings settings, out string error)
        {
            error = null;
            if (IsReady) return true;
            m_Settings = settings.epic ?? new EpicPlatformSettings();
            Instance = this;

            if (!m_SdkRunning && !StartSdk(out error)) return false;
            if (m_SigningIn)
            {
                // The Multiplayer screen opened during the silent start-up sign-in: if that finds no login, open the window next.
                if (m_StartupSignInDone) m_InteractiveWanted = true;
                return true;
            }

            // The first call (start-up) signs in silently; later ones (the Multiplayer screen) may open a login window.
            bool interactive = m_StartupSignInDone;
            m_StartupSignInDone = true;
            BeginSignIn(interactive);
            return true;
        }

        /// <summary>Creates the plugin's EOSManager (unless a scene already has one), which loads and starts the SDK.</summary>
        bool StartSdk(out string error)
        {
            error = null;
            try
            {
                if (PlatformInterfaceOrNull() == null && UnityEngine.Object.FindFirstObjectByType<EOSManager>() == null)
                {
                    var go = new GameObject("[EOSManager]");
                    // EOSManager.Awake starts the SDK from the EOS Configuration and keeps itself across scenes.
                    go.AddComponent<EOSManager>();
                }
            }
            catch (DllNotFoundException e)
            {
                m_FinalFailure = true;
                error = "epic.error_library";
                Debug.LogError($"[DeckParty] The EOS SDK library couldn't be loaded: {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (PlatformInterfaceOrNull() == null)
            {
                // Usually the product settings aren't filled in (EOS Plugin > EOS Configuration), or the library is missing.
                m_FinalFailure = true;
                error = "epic.not_configured";
                Debug.LogError("[DeckParty] Epic Online Services didn't start. Fill in EOS Plugin > EOS Configuration (Product, Sandbox, " +
                               "Deployment, Client ID / Secret) and check the Console for the plugin's own errors.");
                return false;
            }

            m_SdkRunning = true;
            m_FinalFailure = false;
            try
            {
                m_ProductId = EOSManager.Instance.GetProductId();
            }
            catch (Exception)
            {
                m_ProductId = null;
            }
            if (m_Pump == null) m_Pump = EpicPump.Create(this);
            return true;
        }

        public void Shutdown()
        {
            try
            {
                if (IsReady)
                {
                    LeaveLobby();
                    ClearPresence();
                }
                RemoveNotifications();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (m_Pump != null) UnityEngine.Object.Destroy(m_Pump.gameObject);
            m_Pump = null;
            m_Ready = false;
            m_SigningIn = false;
            m_SdkRunning = false;
            if (Instance == this) Instance = null;
            // The plugin's EOSManager shuts the SDK down itself when the application quits.
        }

        /// <summary>Called by the pump every frame (the SDK itself is ticked by EOSManager).</summary>
        internal void Tick()
        {
            if (!IsReady) return;
            FlushLobbyUpdates();
            TickFriends();
            // Friends' presence arrives in bursts: tell the UI at most twice a second.
            if (m_FriendsDirty && Time.unscaledTime >= m_NextFriendsEvent)
            {
                m_FriendsDirty = false;
                m_NextFriendsEvent = Time.unscaledTime + 0.5f;
                Raise(FriendsChanged);
            }
        }

        void MarkFriendsChanged() => m_FriendsDirty = true;

        void RaiseReadyChanged() => Raise(ReadyChanged);

        void RequestJoin(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId)) return;
            try
            {
                JoinRequested?.Invoke(lobbyId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>The Epic Games Launcher doesn't start games into a lobby, so there's never one.</summary>
        public string TakeLaunchJoinRequest() => null;

        static void Raise(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ================================================================== Netcode transport

        public Unity.Netcode.NetworkTransport CreateTransport(GameObject networkManagerObject)
        {
            var transport = networkManagerObject.GetComponent<EpicP2PTransport>();
            return transport != null ? transport : networkManagerObject.AddComponent<EpicP2PTransport>();
        }

        public void PrepareHostTransport(Unity.Netcode.NetworkTransport transport)
        {
            // Only players who are in our EOS lobby may open a connection: an invite-only lobby really is invite-only.
            if (transport is EpicP2PTransport epic) epic.ConfigureHost(IsLobbyMember);
        }

        public void PrepareClientTransport(Unity.Netcode.NetworkTransport transport, string hostUserId)
        {
            if (transport is EpicP2PTransport epic) epic.ConfigureClient(hostUserId);
        }
    }
}
#endif
