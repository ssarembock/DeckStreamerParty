// Signature-accurate stubs of the Unity Services APIs Deck Party calls (copied from the 2.4.1 / 3.8.0 / 1.18.0 sources).
using System;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;

namespace Unity.Services.Core
{
    public enum ServicesInitializationState { Uninitialized, Initializing, Initialized }
    public class InitializationOptions { }
    public static class UnityServices
    {
        public static ServicesInitializationState State => default;
        public static Task InitializeAsync() => Task.CompletedTask;
        public static Task InitializeAsync(InitializationOptions options) => Task.CompletedTask;
    }
}

namespace Unity.Services.Authentication
{
    using Unity.Services.Core;
    public class SignInOptions { }
    public interface IAuthenticationService
    {
        bool IsSignedIn { get; }
        Task SignInAnonymouslyAsync(SignInOptions options = null);
        void SwitchProfile(string profile);
    }
    public static class AuthenticationService { public static IAuthenticationService Instance => null; }
    public static class AuthenticationExtensions
    {
        public static InitializationOptions SetProfile(this InitializationOptions options, string profile) => options;
    }
}

namespace Unity.Services.Relay.Models
{
    public class Allocation { public System.Guid AllocationId { get; } }
    public class JoinAllocation { public System.Guid AllocationId { get; } }
    public static class AllocationUtils
    {
        public static RelayServerData ToRelayServerData(this Allocation allocation, string connectionType) => default;
        public static RelayServerData ToRelayServerData(this JoinAllocation allocation, string connectionType) => default;
    }
}

namespace Unity.Services.Relay
{
    using Unity.Services.Relay.Models;
    public interface IRelayService
    {
        Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null);
        Task<string> GetJoinCodeAsync(Guid allocationId);
        Task<JoinAllocation> JoinAllocationAsync(string joinCode);
    }
    public static class RelayService { public static IRelayService Instance => null; }
}
