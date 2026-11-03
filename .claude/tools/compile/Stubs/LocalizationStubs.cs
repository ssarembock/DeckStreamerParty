// Compile-check stubs for the parts of com.unity.localization 1.5.8 that Deck Party uses (signatures copied from the package).
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    public struct AsyncOperationHandle<T>
    {
        public bool IsDone => true;
        public T WaitForCompletion() => default;
        public T Result => default;
    }
}

namespace UnityEngine.Localization
{
    public struct LocaleIdentifier
    {
        public LocaleIdentifier(string code) { Code = code; }
        public LocaleIdentifier(SystemLanguage language) { Code = language.ToString(); }
        public string Code { get; }
    }

    public class Locale : ScriptableObject
    {
        public LocaleIdentifier Identifier { get; set; }
        public string LocaleName { get; set; }
        public static Locale CreateLocale(string code) => null;
        public static Locale CreateLocale(LocaleIdentifier identifier) => null;
    }
}

namespace UnityEngine.Localization.Tables
{
    public class SharedTableData : ScriptableObject
    {
        public class SharedTableEntry { public long Id; public string Key; }
        public List<SharedTableEntry> Entries { get; } = new List<SharedTableEntry>();
        public SharedTableEntry AddKey(string key) => null;
        public SharedTableEntry GetEntry(string key) => null;
        public void Clear() { }
        public string TableCollectionName { get; set; }
    }

    public class TableEntry
    {
        public string Key { get; set; }
        public long KeyId { get; }
        public string LocalizedValue { get; }
    }

    public class StringTableEntry : TableEntry
    {
        public string Value { get; set; }
        public bool IsSmart { get; set; }
    }

    public class LocalizationTable : ScriptableObject
    {
        public SharedTableData SharedData { get; set; }
        public LocaleIdentifier LocaleIdentifier { get; set; }
    }

    public class StringTable : LocalizationTable
    {
        public ICollection<StringTableEntry> Values { get; } = new List<StringTableEntry>();
        public StringTableEntry AddEntry(string key, string localized) => null;
        public StringTableEntry GetEntry(string key) => null;
        public bool RemoveEntry(string key) => false;
    }
}

namespace UnityEngine.Localization.Settings
{
    using UnityEngine.Localization.Tables;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public interface ILocalesProvider
    {
        List<Locale> Locales { get; }
        Locale GetLocale(LocaleIdentifier id);
        Locale GetLocale(string code);
    }

    public interface IStartupLocaleSelector { }
    public class PlayerPrefLocaleSelector : IStartupLocaleSelector { public string PlayerPreferenceKey { get; set; } }
    public class SystemLocaleSelector : IStartupLocaleSelector { }
    public class SpecificLocaleSelector : IStartupLocaleSelector { public LocaleIdentifier LocaleId { get; set; } }

    public class LocalizedStringDatabase
    {
        public virtual StringTable GetTable(string tableReference, Locale locale = null) => null;
    }

    public class LocalizationSettings : ScriptableObject
    {
        public static bool HasSettings => false;
        public static LocalizationSettings Instance => null;
        public static AsyncOperationHandle<LocalizationSettings> InitializationOperation => default;
        public static ILocalesProvider AvailableLocales => null;
        public static LocalizedStringDatabase StringDatabase => null;
        public static Locale SelectedLocale { get; set; }
        public static List<IStartupLocaleSelector> StartupLocaleSelectors => null;
        public static event Action<Locale> SelectedLocaleChanged;
    }
}
