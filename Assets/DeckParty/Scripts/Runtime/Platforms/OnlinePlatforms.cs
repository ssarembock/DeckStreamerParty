using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckParty.Platforms
{
    /// <summary>
    /// Starts the online platform the project is set to (Project Settings > Deck Party > Online Platform) before the
    /// first scene loads, and hands it to the rest of the game. <see cref="Current"/> is null when multiplayer runs on
    /// the Default setup (Unity Relay join codes and Direct IP): no platform selected, the selected one isn't
    /// implemented, or it couldn't start and <see cref="OnlinePlatformSettings.fallbackToDefault"/> is on.
    /// <para>
    /// Platform assemblies register themselves with <see cref="Register"/> from a
    /// [RuntimeInitializeOnLoadMethod(SubsystemRegistration)] method (see SteamPlatform and EpicPlatform), so this
    /// assembly never references Steamworks or the EOS SDK, and the project compiles without them.
    /// </para>
    /// Command line override (any build): <c>-onlineplatform default</c>, <c>-onlineplatform steam</c> or <c>-onlineplatform epic</c>.
    /// </summary>
    public static class OnlinePlatforms
    {
        const string CommandLineSwitch = "-onlineplatform";

        // Filled by platform assemblies during SubsystemRegistration; never cleared (registering again just replaces).
        static readonly Dictionary<OnlinePlatformKind, Func<IOnlinePlatform>> s_Factories = new Dictionary<OnlinePlatformKind, Func<IOnlinePlatform>>();

        static IOnlinePlatform s_Platform;
        static string s_PendingJoin;
        static bool s_QuitHooked;
        /// <summary>The platform's events are hooked (once per platform instance).</summary>
        static bool s_Hooked;
        /// <summary>"Ready" was handled for the current sign-in (launch join, presence, log line).</summary>
        static bool s_ReadyHandled;

        /// <summary>The platform the project (or the command line) asked for.</summary>
        public static OnlinePlatformKind Selected { get; private set; }

        /// <summary>The running platform, or null when multiplayer uses the Default setup.</summary>
        public static IOnlinePlatform Current => s_Platform != null && s_Platform.IsReady ? s_Platform : null;

        /// <summary>A platform is running multiplayer (Steam or Epic lobbies, plus join codes).</summary>
        public static bool Active => Current != null;

        /// <summary>The selected platform has started and is still signing the player in (Epic): the menus wait for it.</summary>
        public static bool SigningIn => s_Platform != null && !s_Platform.IsReady && s_Platform.IsSigningIn;

        /// <summary>The platform object, even before it's ready (its name and sign-in state for the Multiplayer screen).</summary>
        public static IOnlinePlatform Pending => s_Platform;

        /// <summary>Why the selected platform isn't running (a localization key), or null.</summary>
        public static string Problem { get; private set; }

        /// <summary>The selected platform isn't running and the Default setup isn't allowed to stand in: online play is off.</summary>
        public static bool OnlineBlocked => Selected != OnlinePlatformKind.Default && Current == null && !SigningIn && !OnlinePlatformSettings.Current.fallbackToDefault;

        /// <summary>Players are named after their platform account (Steam's "Use Steam names", Epic's "Use Epic names").</summary>
        public static bool UsePlatformNames
        {
            get
            {
                var platform = Current;
                if (platform == null) return false;
                var settings = OnlinePlatformSettings.Current;
                switch (platform.Kind)
                {
                    case OnlinePlatformKind.Steam: return settings.steam.useSteamNames;
                    case OnlinePlatformKind.Epic: return settings.epic.useEpicNames;
                    default: return false;
                }
            }
        }

        /// <summary>The kind of the running platform (Default when none): what players carry so others know whose id it is.</summary>
        public static OnlinePlatformKind CurrentKind => Current != null ? Current.Kind : OnlinePlatformKind.Default;

        /// <summary>A platform started or stopped (the Multiplayer screen redraws).</summary>
        public static event Action StatusChanged;

        /// <summary>An invite was accepted (or Join Game clicked) and is waiting in <see cref="PendingJoin"/>.</summary>
        public static event Action PendingJoinChanged;

        /// <summary>The lobby a friend invited you to, waiting for the menu to join it (null when none).</summary>
        public static string PendingJoin => s_PendingJoin;

        // ------------------------------------------------------------------ registration and startup

        /// <summary>Called by a platform assembly during SubsystemRegistration.</summary>
        public static void Register(OnlinePlatformKind kind, Func<IOnlinePlatform> factory)
        {
            if (kind == OnlinePlatformKind.Default || factory == null) return;
            s_Factories[kind] = factory;
        }

        /// <summary>Whether this build contains the platform's code (Default always does).</summary>
        public static bool IsImplemented(OnlinePlatformKind kind) => kind == OnlinePlatformKind.Default || s_Factories.ContainsKey(kind);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Play Mode without a domain reload keeps statics: start from scratch (the factories stay registered).
            s_Platform = null;
            s_PendingJoin = null;
            s_QuitHooked = false;
            s_Hooked = false;
            s_ReadyHandled = false;
            Problem = null;
            Selected = OnlinePlatformKind.Default;
            StatusChanged = null;
            PendingJoinChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Startup()
        {
            var settings = OnlinePlatformSettings.Current;
            Selected = ResolveSelection(settings, out var why);
            if (why != null) Debug.Log($"[DeckParty] Online platform: {Selected} ({why}).");
            if (Selected == OnlinePlatformKind.Default) return;

            if (!s_Factories.TryGetValue(Selected, out var factory))
            {
                Problem = Core.Err.Of("platform.not_available", DisplayName(Selected));
                Debug.LogWarning($"[DeckParty] The online platform is set to {Selected}, but this build has no {Selected} support. " +
                                 (Selected == OnlinePlatformKind.Steam
                                     ? "Install Steamworks.NET (Project Settings > Deck Party > Online Platform) and build for Windows, Mac or Linux."
                                     : Selected == OnlinePlatformKind.Epic
                                         ? "Install the EOS Plugin for Unity (Project Settings > Deck Party > Online Platform) and build for Windows, Mac or Linux."
                                         : "Only Steam and Epic are implemented so far.") + " Multiplayer uses the Default setup.");
                return;
            }

            try
            {
                s_Platform = factory();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Problem = Core.Err.Of("platform.failed", DisplayName(Selected));
                return;
            }
            TryInitialize(settings);
        }

        static bool TryInitialize(OnlinePlatformSettings settings)
        {
            bool ok;
            string error;
            try
            {
                ok = s_Platform.Initialize(settings, out error);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ok = false;
                error = Core.Err.Of("platform.failed", DisplayName(Selected));
            }

            if (!ok)
            {
                Problem = string.IsNullOrEmpty(error) ? Core.Err.Of("platform.failed", DisplayName(Selected)) : error;
                Debug.LogWarning($"[DeckParty] {DisplayName(Selected)} didn't start: {Problem}" +
                                 (settings.fallbackToDefault ? " Multiplayer uses the Default setup." : " Online play is off."));
                RaiseStatusChanged();
                return false;
            }

            Problem = null;
            HookPlatform();
            if (s_Platform.IsReady) OnPlatformReady();
            else
            {
                // Epic signs in asynchronously (maybe through a browser window): ReadyChanged finishes the job.
                Debug.Log($"[DeckParty] {s_Platform.DisplayName} started; signing in...");
                RaiseStatusChanged();
            }
            return true;
        }

        static void HookPlatform()
        {
            if (s_Hooked || s_Platform == null) return;
            s_Hooked = true;
            s_Platform.JoinRequested += OnJoinRequested;
            s_Platform.ReadyChanged += OnReadyChanged;
            if (!s_QuitHooked)
            {
                s_QuitHooked = true;
                Application.quitting += Shutdown;
            }
        }

        /// <summary>The platform finished signing in, failed to, or signed the player out.</summary>
        static void OnReadyChanged()
        {
            if (s_Platform == null) return;
            if (s_Platform.IsReady)
            {
                OnPlatformReady();
                return;
            }
            s_ReadyHandled = false;
            if (!s_Platform.IsSigningIn)
            {
                Problem = string.IsNullOrEmpty(s_Platform.LastError) ? Core.Err.Of("platform.failed", s_Platform.DisplayName) : s_Platform.LastError;
                Debug.LogWarning($"[DeckParty] {s_Platform.DisplayName} isn't signed in: {Problem}");
            }
            RaiseStatusChanged();
        }

        static void OnPlatformReady()
        {
            if (s_ReadyHandled || s_Platform == null) return;
            s_ReadyHandled = true;
            Problem = null;
            string launchJoin = s_Platform.TakeLaunchJoinRequest();
            if (!string.IsNullOrEmpty(launchJoin)) OnJoinRequested(launchJoin);
            SetPresence(PresenceState.Menu);
            Debug.Log($"[DeckParty] {s_Platform.DisplayName} is running multiplayer (signed in as {s_Platform.LocalUser.Name}).");
            RaiseStatusChanged();
        }

        /// <summary>
        /// The Multiplayer screen calls this when it opens: if the platform failed for a reason that can pass (Steam wasn't
        /// running yet, the Epic sign-in was cancelled), it tries again so the player doesn't have to restart the game.
        /// Returns true when it's running (or signing in) now.
        /// </summary>
        public static bool RetryIfNeeded()
        {
            if (Current != null) return true;
            if (s_Platform == null || s_Platform.InitFailureIsFinal) return false;
            if (s_Platform.IsSigningIn) return true;
            return TryInitialize(OnlinePlatformSettings.Current);
        }

        static void Shutdown()
        {
            var platform = s_Platform;
            if (platform == null) return;
            platform.JoinRequested -= OnJoinRequested;
            platform.ReadyChanged -= OnReadyChanged;
            s_Hooked = false;
            s_ReadyHandled = false;
            try
            {
                platform.Shutdown();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            s_Platform = null;
        }

        static OnlinePlatformKind ResolveSelection(OnlinePlatformSettings settings, out string why)
        {
            why = null;
            if (TryCommandLine(out var forced))
            {
                why = "set on the command line";
                return forced;
            }
#if UNITY_EDITOR
            if (settings.platform != OnlinePlatformKind.Default)
            {
                if (IsVirtualPlayer())
                {
                    why = "Multiplayer Play Mode virtual players always use the Default setup";
                    return OnlinePlatformKind.Default;
                }
                if (settings.editorUsesDefault)
                {
                    why = "'Editor uses Default' is on in the Online Platform settings";
                    return OnlinePlatformKind.Default;
                }
            }
#endif
            return settings.platform;
        }

        static bool TryCommandLine(out OnlinePlatformKind kind)
        {
            kind = OnlinePlatformKind.Default;
            string[] args;
            try
            {
                args = Environment.GetCommandLineArgs();
            }
            catch (Exception)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string value = null;
                if (string.Equals(arg, CommandLineSwitch, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) value = args[i + 1];
                else if (arg.StartsWith(CommandLineSwitch + "=", StringComparison.OrdinalIgnoreCase)) value = arg.Substring(CommandLineSwitch.Length + 1);
                if (value != null && Enum.TryParse(value.Trim(), true, out kind)) return true;
            }
            return false;
        }

#if UNITY_EDITOR
        /// <summary>A Multiplayer Play Mode virtual player (a clone editor), found without referencing the package.</summary>
        static bool IsVirtualPlayer()
        {
            try
            {
                var type = Type.GetType("Unity.Multiplayer.Playmode.CurrentPlayer, Unity.Multiplayer.Playmode");
                var property = type?.GetProperty("IsMainEditor");
                if (property != null) return !(bool)property.GetValue(null);
            }
            catch (Exception)
            {
                // Fall back to the clone folder check.
            }
            return Application.dataPath.Replace('\\', '/').Contains("/Library/VP/");
        }
#endif

        // ------------------------------------------------------------------ invites and presence

        static void OnJoinRequested(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId)) return;
            s_PendingJoin = lobbyId;
            try
            {
                PendingJoinChanged?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Takes the waiting invite (the menu is about to join it).</summary>
        public static string TakePendingJoin()
        {
            var lobby = s_PendingJoin;
            s_PendingJoin = null;
            return lobby;
        }

        /// <summary>Drops the waiting invite (the player said no).</summary>
        public static void ClearPendingJoin() => s_PendingJoin = null;

        /// <summary>Updates the status friends see (no-op on the Default setup).</summary>
        public static void SetPresence(PresenceState state, string lobbyId = null, int members = 0, int maxMembers = 0, bool joinable = false)
        {
            var platform = Current;
            if (platform == null) return;
            try
            {
                platform.SetPresence(state, lobbyId, members, maxMembers, joinable);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>The platform's brand name (not translated).</summary>
        public static string DisplayName(OnlinePlatformKind kind)
        {
            switch (kind)
            {
                case OnlinePlatformKind.Steam: return "Steam";
                case OnlinePlatformKind.Epic: return "Epic Games";
                case OnlinePlatformKind.AppleGameCenter: return "Game Center";
                case OnlinePlatformKind.GooglePlayGames: return "Google Play Games";
                case OnlinePlatformKind.Xbox: return "Xbox";
                case OnlinePlatformKind.PlayStation: return "PlayStation";
                case OnlinePlatformKind.NintendoSwitch: return "Nintendo Switch";
                default: return "Unity Relay";
            }
        }

        static void RaiseStatusChanged()
        {
            try
            {
                StatusChanged?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
