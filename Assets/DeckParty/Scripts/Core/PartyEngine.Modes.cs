using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>
    /// The game modes added on 2026-11-16 (additionalgamemodes.md): Endless (Trophies are a currency that buys extra streams
    /// and extra shop viewers) and the hooks Versus the AI needs. Team play (2v2, co-op) lives in the main file, wherever
    /// a member's queue, Sub Cap or Audience matters.
    /// </summary>
    public static partial class PartyEngine
    {
        // ================================================================== Endless: extra streams

        /// <summary>Trophies the next extra stream costs: 1, 2, 3, 5, 8, 13... (<see cref="GameRules.EndlessStreamCost"/>).</summary>
        public static int ExtraStreamCost(PlayerState s) => GameRules.EndlessStreamCost(s.StreamsBought);

        /// <summary>Why another stream can't be bought right now (null when it can).</summary>
        public static string GetExtraStreamBlocker(PlayerState s)
        {
            if (!s.IsEndless) return Err.Of("err.endless_only");
            if (s.Phase != GamePhase.Planning) return Err.Of("err.shop_only");
            int cost = ExtraStreamCost(s);
            if (s.Trophies < cost) return Err.Of("err.needs_trophies", cost);
            return null;
        }

        /// <summary>Endless: pay Trophies for one more stream in the run.</summary>
        static CommandResult BuyExtraStream(PlayerState s, List<GameEvent> ev)
        {
            var blocker = GetExtraStreamBlocker(s);
            if (blocker != null) return CommandResult.Fail(blocker);
            int cost = ExtraStreamCost(s);
            s.Trophies -= cost;
            s.StreamsBought++;
            s.TotalNights++;
            Emit(ev, GameEventType.StreamBought, null, cost, s.TotalNights);
            return CommandResult.Success;
        }

        // ================================================================== Endless: extra shop viewers

        /// <summary>Trophies the next viewer added to the shop costs (2, then 1 more each time).</summary>
        public static int ShopViewerCost(PlayerState s) => GameRules.EndlessTrophyCost(s.ShopViewersBought);

        /// <summary>
        /// The viewers on offer when adding one to the shop: up to <see cref="GameRules.EndlessViewerChoices"/> buyable roster
        /// viewers the shop doesn't sell yet. The same every time it's asked until something is bought (the pick depends on
        /// the season's seed and how many have been added), so the offer can't be rerolled and every peer sees the same.
        /// </summary>
        public static List<GuestType> ShopViewerChoices(PlayerState s)
        {
            var pool = new List<GuestType>();
            foreach (var info in GuestCatalog.Roster)
                if (info.Purchasable && !s.Shop.Contains(info.Type)) pool.Add(info.Type);

            var result = new List<GuestType>();
            ulong rng = DeterministicRandom.Seed(s.Seed ^ (0x9E37u * (uint)(s.ShopViewersBought + 1)) ^ (uint)s.Night);
            for (int i = 0; i < GameRules.EndlessViewerChoices && pool.Count > 0; i++)
            {
                int index = DeterministicRandom.Range(ref rng, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return result;
        }

        /// <summary>Why a viewer can't be added to the shop right now (null when it can). Doesn't check which viewer.</summary>
        public static string GetShopViewerBlocker(PlayerState s)
        {
            if (!s.IsEndless) return Err.Of("err.endless_only");
            if (s.Phase != GamePhase.Planning) return Err.Of("err.shop_only");
            if (ShopViewerChoices(s).Count == 0) return Err.Of("err.shop_has_all");
            int cost = ShopViewerCost(s);
            if (s.Trophies < cost) return Err.Of("err.needs_trophies", cost);
            return null;
        }

        /// <summary>Endless: pay Trophies to add one of the offered viewers to the shop (for the rest of the run).</summary>
        static CommandResult BuyShopViewer(PlayerState s, GuestType type, List<GameEvent> ev)
        {
            var blocker = GetShopViewerBlocker(s);
            if (blocker != null) return CommandResult.Fail(blocker);
            if (!ShopViewerChoices(s).Contains(type)) return CommandResult.Fail(Err.Of("err.not_offered"));

            int cost = ShopViewerCost(s);
            s.Trophies -= cost;
            s.ShopViewersBought++;
            s.Shop.Add(type);
            ScenarioCatalog.SortShop(s.Shop);
            Emit(ev, GameEventType.ShopViewerAdded, null, (int)type, cost);
            return CommandResult.Success;
        }

        // ================================================================== Versus the AI

        /// <summary>
        /// Versus the AI: the rival won first, so this season is lost. The session calls it (the engine only sees one player);
        /// it does nothing once the season is over.
        /// </summary>
        public static void LoseToRival(PlayerState s, int rivalNight, List<GameEvent> ev)
        {
            if (s == null || s.IsGameOver) return;
            ClearParty(s);
            s.Phase = GamePhase.Defeat;
            Emit(ev, GameEventType.RivalVictory, null, rivalNight);
            Emit(ev, GameEventType.Defeat, null);
        }

        /// <summary>Streams the run has lasted so far (Endless): the streams played, counting the one under way.</summary>
        public static int StreamsLasted(PlayerState s) => s == null ? 0 : Math.Max(0, s.Night);
    }
}
