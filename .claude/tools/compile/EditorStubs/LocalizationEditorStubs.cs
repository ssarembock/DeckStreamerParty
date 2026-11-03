// Compile-check stubs for com.unity.localization 1.5.8 / com.unity.addressables 2.9.1 editor APIs (signatures from the packages).
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization
{
    public class LocalizationTableCollection : ScriptableObject
    {
        public virtual SharedTableData SharedData { get; }
        public virtual string TableCollectionName { get; set; }
        public virtual LocalizationTable AddNewTable(LocaleIdentifier localeIdentifier) => null;
        public virtual LocalizationTable GetTable(LocaleIdentifier localeIdentifier) => null;
    }

    public class StringTableCollection : LocalizationTableCollection { }

    public static class LocalizationEditorSettings
    {
        public static LocalizationSettings ActiveLocalizationSettings { get; set; }
        public static void AddLocale(Locale locale, bool createUndo = false) { }
        public static ReadOnlyCollection<Locale> GetLocales() => null;
        public static ReadOnlyCollection<StringTableCollection> GetStringTableCollections() => null;
        public static StringTableCollection GetStringTableCollection(string tableNameOrGuid) => null;
        public static StringTableCollection CreateStringTableCollection(string tableName, string assetDirectory, IList<Locale> selectedLocales) => null;
    }
}

namespace UnityEditor.AddressableAssets.Settings
{
    public class AddressableAssetSettings : ScriptableObject
    {
        public enum PlayerBuildOption { PreferencesValue, BuildWithPlayer, DoNotBuildWithPlayer }
        public PlayerBuildOption BuildAddressablesWithPlayerBuild { get; set; }
    }
}

namespace UnityEditor.AddressableAssets
{
    public class AddressableAssetSettingsDefaultObject : ScriptableObject
    {
        public static Settings.AddressableAssetSettings Settings { get; set; }
    }
}
