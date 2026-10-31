using System;
using System.Collections.Generic;
using DeckParty.Core;

namespace DeckParty
{
    /// <summary>
    /// The game UI only ever talks to this interface. Single player runs the engine locally;
    /// multiplayer forwards commands to the host and receives authoritative snapshots.
    /// </summary>
    public interface IGameSession : IDisposable
    {
        bool IsMultiplayer { get; }

        /// <summary>Latest known state (null until the first snapshot arrives in multiplayer). Treat as read-only.</summary>
        PlayerState State { get; }

        event Action<PlayerState, IReadOnlyList<GameEvent>> StateChanged;
        event Action<string> CommandRejected;

        void Submit(GameCommand command);
    }
}
