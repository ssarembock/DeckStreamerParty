// Epic code only exists when the EOS Plugin for Unity is installed (DECKPARTY_EOS, a version define in DeckParty.Epic.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_EOS && UNITY_STANDALONE
using System;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using ConnectCredentials = Epic.OnlineServices.Connect.Credentials;
using ConnectLoginOptions = Epic.OnlineServices.Connect.LoginOptions;
using ConnectLoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using AuthLoginOptions = Epic.OnlineServices.Auth.LoginOptions;
using AuthLoginCallbackInfo = Epic.OnlineServices.Auth.LoginCallbackInfo;
using AuthCredentials = Epic.OnlineServices.Auth.Credentials;

namespace DeckParty.Platforms.Epic
{
    /// <summary>
    /// Signing in to Epic, in two steps.
    /// <list type="number">
    /// <item>Epic Account Services (EOS Auth), for friends, presence and the overlay. The ways tried, in order: the Epic
    /// Games Launcher's exchange code (store builds), the remembered login (persistent auth), the Developer
    /// Authentication Tool (Editor and development builds), and, only when the player opens the Multiplayer screen, the
    /// account portal (Epic's login window).</item>
    /// <item>EOS Connect, which gives the product user id lobbies and P2P use. With an Epic account it's made from the
    /// account's id token; without one (allowed in the settings) from this device's id, which plays fine but has no
    /// friends, invites or presence.</item>
    /// </list>
    /// </summary>
    public sealed partial class EpicPlatform
    {
        const string ExchangeCodeArg = "-AUTH_PASSWORD=";
        const string AuthTypeArg = "-AUTH_TYPE=";
        const string DevAuthArg = "-epicdevauth=";
        const string DevAuthHostArg = "-epicdevauthhost=";

        ulong m_AuthStatusNotify;
        ulong m_ConnectExpiryNotify;
        bool m_Refreshing;

        static AuthInterface Auth => Sdk(m => m.GetEOSAuthInterface());
        static global::Epic.OnlineServices.Connect.ConnectInterface Connect => Sdk(m => m.GetEOSConnectInterface());

        void BeginSignIn(bool interactive)
        {
            if (m_SigningIn) return;
            _ = SignInAsync(interactive);
        }

        async Task SignInAsync(bool interactive)
        {
            m_SigningIn = true;
            m_LastError = null;
            RaiseReadyChanged();
            try
            {
                EpicAccountId account = null;
                string launcherCode = m_Settings.useExchangeCode ? ExchangeCodeFromCommandLine() : null;
                if (launcherCode != null) account = await AuthLoginAsync(LoginCredentialType.ExchangeCode, null, launcherCode, false);
                if (account == null && m_Settings.usePersistentLogin) account = await AuthLoginAsync(LoginCredentialType.PersistentAuth, null, null, true);
                if (account == null && TryGetDevAuth(out var host, out var credential))
                    account = await AuthLoginAsync(LoginCredentialType.Developer, host, credential, false);
                if (account == null && interactive && m_Settings.useAccountPortal)
                    account = await AuthLoginAsync(LoginCredentialType.AccountPortal, null, null, false);
                if (!m_SdkRunning) return;

                ProductUserId user = null;
                if (account != null) user = await ConnectWithEpicAccountAsync(account);

                // No Epic account: this device's id still gets lobbies, join codes and matches. At start-up that waits for
                // the Multiplayer screen when the login window is on, so players get the chance to sign in properly first.
                if (user == null && m_Settings.allowDeviceIdFallback && (interactive || !m_Settings.useAccountPortal))
                {
                    if (account != null) Debug.LogWarning("[DeckParty] Epic: the Epic account couldn't open an EOS Connect session; using this device's id.");
                    account = null;
                    user = await ConnectWithDeviceIdAsync();
                }
                if (!m_SdkRunning) return;

                if (user == null)
                {
                    m_LastError = account != null ? "epic.connect_failed" : interactive ? "epic.sign_in_failed" : "epic.sign_in_needed";
                    return;
                }

                m_LocalAccount = account;
                m_LocalProductUser = user;
                string name = account != null ? await DisplayNameAsync(account) : null;
                if (string.IsNullOrWhiteSpace(name)) name = AppSettings.PlayerName;
                m_LocalUser = new PlatformUser(user.ToString(), name);

                AddLoginNotifications();
                StartLobbyServices();
                StartFriendServices();
                m_Ready = true;
                Debug.Log($"[DeckParty] Epic signed in: {name} ({(account != null ? "Epic account" : "device id")}, product user {user}).");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                m_LastError = "epic.sign_in_failed";
            }
            finally
            {
                m_SigningIn = false;
                bool openWindowNow = !m_Ready && !interactive && m_InteractiveWanted && m_SdkRunning;
                m_InteractiveWanted = false;
                // The Multiplayer screen is waiting: go straight on to the login window instead of reporting the failure.
                if (openWindowNow) BeginSignIn(true);
                else RaiseReadyChanged();
            }
        }

        // ================================================================== Epic Account Services (Auth)

        Task<EpicAccountId> AuthLoginAsync(LoginCredentialType type, string id, string token, bool noUserInterface)
        {
            var done = new TaskCompletionSource<EpicAccountId>();
            var auth = Auth;
            if (auth == null)
            {
                done.SetResult(null);
                return done.Task;
            }

            var options = new AuthLoginOptions
            {
                Credentials = new AuthCredentials { Type = type, Id = id, Token = token },
                // Must match the permissions of the client in the Developer Portal (Epic Account Services).
                ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence,
                LoginFlags = noUserInterface ? LoginFlags.NoUserInterface : LoginFlags.None,
            };
            auth.Login(ref options, null, (ref AuthLoginCallbackInfo info) =>
            {
                if (info.ResultCode == Result.Success && info.LocalUserId != null && info.LocalUserId.IsValid())
                {
                    done.TrySetResult(info.LocalUserId);
                    return;
                }
                // A missing or stale remembered login is normal: only say so for the other ways.
                if (type != LoginCredentialType.PersistentAuth) Debug.Log($"[DeckParty] Epic sign-in ({type}) didn't work: {info.ResultCode}.");
                if (type == LoginCredentialType.PersistentAuth && info.ResultCode != Result.NotFound) ForgetPersistentLogin();
                done.TrySetResult(null);
            });
            return done.Task;
        }

        static void ForgetPersistentLogin()
        {
            var auth = Auth;
            if (auth == null) return;
            var options = new DeletePersistentAuthOptions();
            auth.DeletePersistentAuth(ref options, null, (ref DeletePersistentAuthCallbackInfo _) => { });
        }

        Task<string> DisplayNameAsync(EpicAccountId account)
        {
            var done = new TaskCompletionSource<string>();
            var userInfo = Sdk(m => m.GetEOSUserInfoInterface());
            if (userInfo == null)
            {
                done.SetResult(null);
                return done.Task;
            }
            var query = new global::Epic.OnlineServices.UserInfo.QueryUserInfoOptions { LocalUserId = account, TargetUserId = account };
            userInfo.QueryUserInfo(ref query, null, (ref global::Epic.OnlineServices.UserInfo.QueryUserInfoCallbackInfo info) =>
            {
                done.TrySetResult(info.ResultCode == Result.Success ? BestDisplayName(account, account) : null);
            });
            return done.Task;
        }

        /// <summary>An Epic account's display name as this player may see it (empty when not known yet).</summary>
        static string BestDisplayName(EpicAccountId local, EpicAccountId target)
        {
            var userInfo = Sdk(m => m.GetEOSUserInfoInterface());
            if (userInfo == null || local == null || target == null) return string.Empty;
            var best = new global::Epic.OnlineServices.UserInfo.CopyBestDisplayNameOptions { LocalUserId = local, TargetUserId = target };
            if (userInfo.CopyBestDisplayName(ref best, out var name) == Result.Success && name.HasValue && name.Value.DisplayName != null)
                return name.Value.DisplayName;
            var copy = new global::Epic.OnlineServices.UserInfo.CopyUserInfoOptions { LocalUserId = local, TargetUserId = target };
            if (userInfo.CopyUserInfo(ref copy, out var data) == Result.Success && data.HasValue && data.Value.DisplayName != null)
                return data.Value.DisplayName;
            return string.Empty;
        }

        // ================================================================== EOS Connect (product user id)

        async Task<ProductUserId> ConnectWithEpicAccountAsync(EpicAccountId account)
        {
            var auth = Auth;
            if (auth == null) return null;
            var copy = new CopyIdTokenOptions { AccountId = account };
            if (auth.CopyIdToken(ref copy, out var token) != Result.Success || !token.HasValue || token.Value.JsonWebToken == null)
            {
                Debug.LogWarning("[DeckParty] Epic: no id token for the signed-in account.");
                return null;
            }
            var credentials = new ConnectCredentials { Type = ExternalCredentialType.EpicIdToken, Token = token.Value.JsonWebToken };
            return await ConnectLoginAsync(credentials, null);
        }

        async Task<ProductUserId> ConnectWithDeviceIdAsync()
        {
            var created = await CreateDeviceIdAsync();
            // The device id may exist already from an earlier run: that's what we want.
            if (created != Result.Success && created != Result.DuplicateNotAllowed)
            {
                Debug.LogWarning($"[DeckParty] Epic: couldn't create a device id ({created}).");
                return null;
            }
            var credentials = new ConnectCredentials { Type = ExternalCredentialType.DeviceidAccessToken, Token = null };
            var info = new global::Epic.OnlineServices.Connect.UserLoginInfo { DisplayName = AppSettings.PlayerName };
            return await ConnectLoginAsync(credentials, info);
        }

        Task<Result> CreateDeviceIdAsync()
        {
            var done = new TaskCompletionSource<Result>();
            var connect = Connect;
            if (connect == null)
            {
                done.SetResult(Result.NotConfigured);
                return done.Task;
            }
            var options = new global::Epic.OnlineServices.Connect.CreateDeviceIdOptions { DeviceModel = string.IsNullOrEmpty(SystemInfo.deviceModel) ? "PC" : SystemInfo.deviceModel };
            connect.CreateDeviceId(ref options, null, (ref global::Epic.OnlineServices.Connect.CreateDeviceIdCallbackInfo info) => done.TrySetResult(info.ResultCode));
            return done.Task;
        }

        /// <summary>Connect login; a first-time player gets their product user id created.</summary>
        async Task<ProductUserId> ConnectLoginAsync(ConnectCredentials credentials, global::Epic.OnlineServices.Connect.UserLoginInfo? userInfo)
        {
            var connect = Connect;
            if (connect == null) return null;

            var done = new TaskCompletionSource<(Result Result, ProductUserId User, ContinuanceToken Continuance)>();
            var options = new ConnectLoginOptions { Credentials = credentials, UserLoginInfo = userInfo };
            connect.Login(ref options, null, (ref ConnectLoginCallbackInfo info) => done.TrySetResult((info.ResultCode, info.LocalUserId, info.ContinuanceToken)));
            var login = await done.Task;

            if (login.Result == Result.Success && login.User != null && login.User.IsValid()) return login.User;
            if (login.Result == Result.InvalidUser && login.Continuance != null) return await CreateUserAsync(login.Continuance);
            Debug.LogWarning($"[DeckParty] Epic Connect login ({credentials.Type}) failed: {login.Result}.");
            return null;
        }

        Task<ProductUserId> CreateUserAsync(ContinuanceToken continuance)
        {
            var done = new TaskCompletionSource<ProductUserId>();
            var options = new global::Epic.OnlineServices.Connect.CreateUserOptions { ContinuanceToken = continuance };
            Connect.CreateUser(ref options, null, (ref global::Epic.OnlineServices.Connect.CreateUserCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success) Debug.LogWarning($"[DeckParty] Epic Connect couldn't create the player ({info.ResultCode}).");
                done.TrySetResult(info.ResultCode == Result.Success ? info.LocalUserId : null);
            });
            return done.Task;
        }

        // ================================================================== staying signed in

        void AddLoginNotifications()
        {
            RemoveLoginNotifications();
            var connect = Connect;
            if (connect != null)
            {
                var expiry = new global::Epic.OnlineServices.Connect.AddNotifyAuthExpirationOptions();
                m_ConnectExpiryNotify = connect.AddNotifyAuthExpiration(ref expiry, null,
                    (ref global::Epic.OnlineServices.Connect.AuthExpirationCallbackInfo info) => _ = RefreshConnectAsync());
            }
            var auth = Auth;
            if (auth != null && HasEpicAccount)
            {
                var status = new AddNotifyLoginStatusChangedOptions();
                m_AuthStatusNotify = auth.AddNotifyLoginStatusChanged(ref status, null, (ref LoginStatusChangedCallbackInfo info) =>
                {
                    if (info.CurrentStatus != LoginStatus.NotLoggedIn || m_LocalAccount == null || info.LocalUserId == null) return;
                    if (info.LocalUserId.ToString() != m_LocalAccount.ToString()) return;
                    // Signed out of Epic (from the overlay, or the session ended): multiplayer waits for a new sign-in.
                    Debug.LogWarning("[DeckParty] Epic signed the player out.");
                    SignedOut("epic.signed_out");
                });
            }
        }

        void RemoveLoginNotifications()
        {
            if (!SdkRunning)
            {
                m_ConnectExpiryNotify = 0;
                m_AuthStatusNotify = 0;
                return;
            }
            if (m_ConnectExpiryNotify != 0) Connect?.RemoveNotifyAuthExpiration(m_ConnectExpiryNotify);
            if (m_AuthStatusNotify != 0) Auth?.RemoveNotifyLoginStatusChanged(m_AuthStatusNotify);
            m_ConnectExpiryNotify = 0;
            m_AuthStatusNotify = 0;
        }

        /// <summary>The Connect session runs out every so often: log in again with the same identity before it does.</summary>
        async Task RefreshConnectAsync()
        {
            if (m_Refreshing || !m_Ready) return;
            m_Refreshing = true;
            try
            {
                var user = HasEpicAccount ? await ConnectWithEpicAccountAsync(m_LocalAccount) : await ConnectWithDeviceIdAsync();
                if (user == null) Debug.LogWarning("[DeckParty] Epic: couldn't refresh the EOS Connect session; it may expire.");
            }
            finally
            {
                m_Refreshing = false;
            }
        }

        void SignedOut(string why)
        {
            if (!m_Ready) return;
            try
            {
                LeaveLobby();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            RemoveNotifications();
            m_Ready = false;
            m_LastError = why;
            m_LocalAccount = null;
            m_LocalProductUser = null;
            RaiseReadyChanged();
        }

        void RemoveNotifications()
        {
            RemoveLoginNotifications();
            RemoveLobbyNotifications();
            RemoveFriendNotifications();
        }

        // ================================================================== command line

        /// <summary>The exchange code the Epic Games Launcher passes (-AUTH_TYPE=exchangecode -AUTH_PASSWORD=code), or null.</summary>
        static string ExchangeCodeFromCommandLine()
        {
            string type = CommandLineValue(AuthTypeArg);
            string code = CommandLineValue(ExchangeCodeArg);
            if (string.IsNullOrEmpty(code)) return null;
            return string.IsNullOrEmpty(type) || string.Equals(type, "exchangecode", StringComparison.OrdinalIgnoreCase) ? code : null;
        }

        /// <summary>The Developer Authentication Tool login (Editor and development builds only), from the settings or the command line.</summary>
        bool TryGetDevAuth(out string host, out string credential)
        {
            host = CommandLineValue(DevAuthHostArg) ?? m_Settings.devAuthHost;
            credential = CommandLineValue(DevAuthArg) ?? m_Settings.devAuthCredential;
            if (!Application.isEditor && !Debug.isDebugBuild) return false;
            return !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(credential);
        }

        static string CommandLineValue(string prefix)
        {
            string[] args;
            try
            {
                args = Environment.GetCommandLineArgs();
            }
            catch (Exception)
            {
                return null;
            }
            foreach (var arg in args)
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return arg.Substring(prefix.Length).Trim('"');
            return null;
        }
    }
}
#endif
