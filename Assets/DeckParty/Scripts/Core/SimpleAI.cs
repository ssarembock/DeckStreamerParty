using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>
    /// Versus the AI (2026-11-16): the simplest opponent that plays a real season. It streams with random moves (adds
    /// viewers, sometimes uses an action on a random legal target) and stops at a random moment that gets likelier as chat
    /// fills up and Risk climbs, so it neither quits straight away nor always pushes its luck. In the shop it buys a Trophy
    /// viewer whenever it can afford one, otherwise random viewers, a Sub Cap upgrade now and then and its streamer's power.
    /// It only ever sends commands the rules engine accepts for any player, so it can't cheat.
    /// </summary>
    public sealed class SimpleAI
    {
        /// <summary>The AI never ends a stream with fewer viewers than this in chat (unless it can't add any more).</summary>
        public const int MinViewersBeforeStopping = 2;
        /// <summary>Most shop purchases in one visit.</summary>
        public const int MaxShopMoves = 8;

        readonly Random m_Random;

        public SimpleAI(int seed) => m_Random = new Random(seed);

        // ================================================================== streaming

        /// <summary>
        /// The AI's next move while its stream is on (or a Temporary Ban to hand out), or null when the stream is over.
        /// </summary>
        public GameCommand? NextStreamMove(PlayerState s)
        {
            if (s == null || s.IsGameOver) return null;
            if (s.Phase == GamePhase.RaidBlame)
            {
                int suspect = s.RaidSuspects.Count > 0 ? s.RaidSuspects[m_Random.Next(s.RaidSuspects.Count)] : -1;
                return GameCommand.Blame(suspect);
            }
            if (s.Phase != GamePhase.Party) return null;

            // Decisions the stream waits on.
            if (s.PeekedId >= 0)
            {
                var peeked = s.Find(s.PeekedId);
                bool risky = PartyEngine.WouldArriveRisky(peeked);
                bool room = !s.SideFull(peeked != null ? peeked.Owner : 0);
                return room && (!risky || m_Random.NextDouble() < 0.3) ? GameCommand.AdmitPeeked() : GameCommand.RejectPeeked();
            }
            if (s.AttractionPending) return GameCommand.ResolveAttraction(-1, -1);

            // Sometimes use an action first.
            if (m_Random.NextDouble() < 0.3)
            {
                var action = RandomAction(s);
                if (action.HasValue) return action;
            }

            bool canAdd = PartyEngine.GetDoorBlocker(s) == null;
            if (!canAdd)
            {
                // Nobody else can join: maybe one last action, then end the stream.
                if (m_Random.NextDouble() < 0.5)
                {
                    var action = RandomAction(s);
                    if (action.HasValue) return action;
                }
                return GameCommand.EndParty();
            }

            if (s.House.Count >= MinViewersBeforeStopping && m_Random.NextDouble() < StopChance(s)) return GameCommand.EndParty();
            return GameCommand.OpenDoor();
        }

        /// <summary>
        /// The chance to stop now: low with an empty-ish chat, higher as it fills, and much higher one Risk short of being
        /// deplatformed. Random enough that the AI sometimes stops early and sometimes gets greedy.
        /// </summary>
        static double StopChance(PlayerState s)
        {
            double fill = s.Capacity > 0 ? Math.Min(1.0, s.Occupancy / (double)s.Capacity) : 1.0;
            int risk = PartyEngine.NetTrouble(s);
            double chance = 0.08 + 0.35 * fill * fill;
            if (risk >= PartyEngine.RiskLimit(s) - 1) chance += 0.55;
            else if (risk > 0) chance += 0.1 * risk;
            if (PartyEngine.DiamondsPresent(s) >= PartyEngine.TrophiesToWin(s)) chance = 1.0;
            return Math.Min(1.0, chance);
        }

        /// <summary>A random usable action with random legal targets, or null when none can be used.</summary>
        GameCommand? RandomAction(PlayerState s)
        {
            var actors = new List<int>();
            foreach (var id in s.House)
                if (PartyEngine.GetAbilityBlocker(s, id) == null && !WastesEarlyRetirement(s, id)) actors.Add(id);

            for (int attempt = 0; attempt < 3 && actors.Count > 0; attempt++)
            {
                int index = m_Random.Next(actors.Count);
                int actorId = actors[index];
                actors.RemoveAt(index);
                var command = BuildAction(s, actorId);
                if (command.HasValue && PartyEngine.ValidateCommand(s, command.Value) == null) return command;
            }
            return null;
        }

        /// <summary>
        /// A Boomer's Early Retirement the AI leaves alone: on a Trophy viewer (it could lose the card for good), or while the
        /// chat would bank little (under half the chat's seats in Cash).
        /// </summary>
        static bool WastesEarlyRetirement(PlayerState s, int actorId)
        {
            var actor = s.Find(actorId);
            if (actor == null || actor.Info.Ability != AbilityKind.EarlyRetirement) return false;
            if (actor.Info.IsDiamond) return true;
            PartyEngine.ProjectEarlyRetirement(s, out int cash, out _);
            return cash < Math.Max(4, s.Capacity / 2);
        }

        GameCommand? BuildAction(PlayerState s, int actorId)
        {
            var actor = s.Find(actorId);
            if (actor == null) return null;
            var info = actor.Info;
            var options = info.Options;
            int option = options != null && options.Length > 0 ? m_Random.Next(options.Length) : 0;

            switch (info.Target)
            {
                case TargetMode.None:
                    return GameCommand.UseAbility(actorId, option: option);

                case TargetMode.AnyAttendee:
                case TargetMode.OtherAttendee:
                case TargetMode.UpToThree:
                {
                    int target = RandomTarget(s, actorId, s.House, null);
                    return target >= 0 ? GameCommand.UseAbility(actorId, target, option: option) : (GameCommand?)null;
                }

                case TargetMode.TwoAttendees:
                case TargetMode.AdjacentPair:
                {
                    int first = RandomTarget(s, actorId, s.House, null);
                    if (first < 0) return null;
                    int second = RandomTarget(s, actorId, s.House, new List<int> { first });
                    return second >= 0 ? GameCommand.UseAbility(actorId, first, second, option: option) : (GameCommand?)null;
                }

                case TargetMode.WaitingGuest:
                {
                    int target = RandomTarget(s, actorId, s.Pool, null);
                    return target >= 0 ? GameCommand.UseAbility(actorId, target, option: option) : (GameCommand?)null;
                }

                default:
                    // Look-ahead pickers (put-aside viewers) are left alone.
                    return null;
            }
        }

        int RandomTarget(PlayerState s, int actorId, List<int> from, List<int> chosen)
        {
            var valid = new List<int>();
            foreach (var id in from)
                if (PartyEngine.IsValidTarget(s, actorId, id, chosen)) valid.Add(id);
            return valid.Count > 0 ? valid[m_Random.Next(valid.Count)] : -1;
        }

        // ================================================================== shopping

        /// <summary>
        /// The AI's next shop move, or null when it's done shopping (the caller then starts its next stream). Rival powers
        /// are left to the caller, which knows the rival (<paramref name="rivalAvailable"/>).
        /// </summary>
        public GameCommand? NextShopMove(PlayerState s, int movesSoFar, bool rivalAvailable)
        {
            if (s == null || s.IsGameOver || s.Phase != GamePhase.Planning || movesSoFar >= MaxShopMoves) return null;

            // A Trophy viewer whenever it can afford one: that's how a season is won.
            GuestType? bestTrophy = null;
            var affordable = new List<GuestType>();
            foreach (var type in s.Shop)
            {
                if (PartyEngine.GetBuyBlocker(s, type) != null) continue;
                var info = GuestCatalog.Get(type);
                if (info.IsDiamond)
                {
                    if (!bestTrophy.HasValue || info.Cost > GuestCatalog.Get(bestTrophy.Value).Cost) bestTrophy = type;
                }
                else affordable.Add(type);
            }
            if (bestTrophy.HasValue) return GameCommand.Buy(bestTrophy.Value);

            // Its streamer's power now and then.
            if (m_Random.NextDouble() < 0.35 && PartyEngine.GetPowerBlocker(s, rivalAvailable) == null) return GameCommand.UsePower();

            // A Sub Cap upgrade now and then (more seats, more Cash).
            if (m_Random.NextDouble() < 0.4 && PartyEngine.GetExpandBlocker(s) == null) return GameCommand.ExpandHouse();

            // Random viewers while the money lasts, saving up some of the time.
            if (affordable.Count > 0 && m_Random.NextDouble() < 0.7) return GameCommand.Buy(affordable[m_Random.Next(affordable.Count)]);
            return null;
        }
    }
}
