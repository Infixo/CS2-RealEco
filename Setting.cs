using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using RealEco.Config;

namespace RealEco;

[FileLocation(nameof(RealEco))]
[SettingsUIGroupOrder(kOptionsGroup, kButtonGroup)]
[SettingsUIShowGroupName(kOptionsGroup, kButtonGroup)]
public class Setting : ModSetting
{
    public const string kSection = "Main";

    public const string kOptionsGroup = "Options";
    public const string kButtonGroup = "Actions";

    public Setting(IMod mod) : base(mod)
    {
        SetDefaults();
    }

    /// <summary>
    /// Gets or sets a value indicating whether: Used to force saving of Modsettings if settings would result in empty Json.
    /// </summary>
    [SettingsUIHidden]
    public bool _Hidden { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool Logging { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool UseLocalConfig { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool DumpConfig { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool FeaturePrefabs { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool FeatureConsumptionFix { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool FeatureNewCompanies { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool FeatureCommercialDemand { get; set; }

    /* not used atm
    [SettingsUIButton]
    [SettingsUIConfirmation]
    [SettingsUISection(kSection, kButtonGroup)]
    public bool ApplyConfiguration { set { Mod.log.Info("ApplyConfiguration clicked"); ConfigTool.ReadAndApply(); } }
    */

    public override void SetDefaults()
    {
        _Hidden = true;
#if DEBUG
        Logging = true;
        DumpConfig = true;
#else
        Logging = false;
        DumpConfig = false;
#endif
        UseLocalConfig = false;
        FeaturePrefabs = true;
        FeatureConsumptionFix = true;
        FeatureNewCompanies = true;
        FeatureCommercialDemand = true;
    }

    public override void Apply()
    {
        base.Apply();
        if (FeatureNewCompanies) FeatureCommercialDemand = true;
    }
}

public class LocaleEN : IDictionarySource
{
    private readonly Setting m_Setting;
    public LocaleEN(Setting setting)
    {
        m_Setting = setting;
    }

    public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
    {
        return new Dictionary<string, string>
        {
            { m_Setting.GetSettingsLocaleID(), $"Economy Rebalance {Mod.modAsset.version}" },
            { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

            { m_Setting.GetOptionGroupLocaleID(Setting.kOptionsGroup), "Options" },
            { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Actions" },
			
            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Logging)), "Detailed logging" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.Logging)), "Outputs more diagnostics information to the log file." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UseLocalConfig)), "Use local configuration" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.UseLocalConfig)), "Local configuration will be used instead of the default one that is shipped with the mod." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpConfig)), "Dump configuration" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpConfig)), "Configuration will be dumped to a separate file for diagnostics." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeaturePrefabs)), "Enable Prefabs" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeaturePrefabs)), "Enables new prefab params." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeatureConsumptionFix)), "Enable Consumption Fix" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeatureConsumptionFix)), "Enables Consumption Fix." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeatureNewCompanies)), "Enable New Companies" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeatureNewCompanies)), "Enables commercial companies for immaterial resources. Requires Commercial Demand feature." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeatureCommercialDemand)), "Enable Commercial Demand" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeatureCommercialDemand)), "Enables modded commercial demand and dedicated UI." },

            /* not used atm
            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ApplyConfiguration)), "Apply Configuration" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.ApplyConfiguration)), "This will apply a new configuration from Confix.xml file." },
            { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ApplyConfiguration)), "This will apply a new configuration. Please confirm." },
            */
        };
    }

    public void Unload()
    {
    }
}
