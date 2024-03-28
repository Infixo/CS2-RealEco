using System;
using System.Collections.Generic;
using Colossal;
using Colossal.Localization;
using Game.SceneFlow;
using Game.UI;
using Game.Economy;

namespace RealEco;

public class StatsLocaleEN : IDictionarySource
{
    public StatsLocaleEN()
    {
    }

    public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
    {
        LocalizationManager manager = GameManager.instance.localizationManager;
        Mod.log.Info($"Registering labels for LocaleId = active {manager.activeLocaleId} / fallback {manager.fallbackLocaleId}");

        // Dumps entire active localization to the log
        //foreach (var entry in manager.activeDictionary.entries)
            //Plugin.Log($"{entry.Key}={entry.Value}");

        Dictionary<string, string> dict = new Dictionary<string, string>();

        void AddResource(Resource res)
        {
            if (manager.activeDictionary.TryGetValue($"Resources.TITLE[{res}]", out string locName))
            {
                dict.Add(string.Format(LocaleIds.kStatisticTitleFormat, $"ServiceCount{res}"), locName);
                dict.Add(string.Format(LocaleIds.kStatisticTitleFormat, $"ServiceMaxWorkers{res}"), locName);
                dict.Add(string.Format(LocaleIds.kStatisticTitleFormat, $"ServiceWealth{res}"), locName);
                dict.Add(string.Format(LocaleIds.kStatisticTitleFormat, $"ServiceWorkers{res}"), locName);
            }
            else
                Mod.log.Info($"Warning. Failed to get localized name for {res}.");
        }

        AddResource(Resource.Software);
        AddResource(Resource.Telecom);
        AddResource(Resource.Financial);
        AddResource(Resource.Media);

        //foreach (var pair in dict)
            //Plugin.Log($"{pair.Key}={pair.Value}");

        return dict;
    }

    public void Unload()
    {
    }
}
