namespace DeckParty.EditorTools
{
    internal static class DeckPartyPaths
    {
        public const string Root = "Assets/DeckParty";
        public const string Data = Root + "/Data";
        public const string Venues = Data + "/Venues";
        public const string Resources = Root + "/Resources";
        public const string NetworkResources = Resources + "/Network";
        public const string Prefabs = Root + "/Prefabs/UI";
        public const string Scenes = Root + "/Scenes";
        public const string Art = Root + "/Art";
        public const string ArtCharacters = Art + "/Characters";
        public const string ArtStreamers = Art + "/Streamers";
        public const string ArtBackgrounds = Art + "/Backgrounds";
        public const string ArtUI = Art + "/UI";
        public const string Audio = Root + "/Audio";

        public const string Database = Resources + "/DeckPartyDatabase.asset";
        public const string Skin = Data + "/UISkin.asset";
        public const string AudioLibrary = Data + "/AudioLibrary.asset";
        public const string NetworkSessionPrefab = NetworkResources + "/DeckPartyNetSession.prefab";

        public const string GuestCardPrefab = Prefabs + "/GuestCard.prefab";
        public const string GuestRowPrefab = Prefabs + "/GuestRow.prefab";
        public const string ShopItemPrefab = Prefabs + "/ShopItem.prefab";
        public const string ScenarioItemPrefab = Prefabs + "/ScenarioItem.prefab";
        public const string PlayerRowPrefab = Prefabs + "/PlayerRow.prefab";
        public const string TextLinePrefab = Prefabs + "/TextLine.prefab";

        public const string MainMenuScene = Scenes + "/MainMenu.unity";
        public const string GameScene = Scenes + "/Game.unity";
    }
}
