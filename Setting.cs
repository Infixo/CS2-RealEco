using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;

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
    public bool FeaturePrefabs { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool FeatureConsumptionFix { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool FeatureNewCompanies { get; set; }

    [SettingsUISection(kSection, kOptionsGroup)]
    public bool FeatureCommercialDemand { get; set; }

    [SettingsUISection(kSection, kButtonGroup)]
    public bool Button { set { Mod.log.Info("Button clicked"); } }

    [SettingsUIButton]
    [SettingsUIConfirmation]
    [SettingsUISection(kSection, kButtonGroup)]
    public bool ButtonWithConfirmation { set { Mod.log.Info("ButtonWithConfirmation clicked"); } }

    public override void SetDefaults()
    {
        _Hidden = true;
        Logging = false;
        FeaturePrefabs = true;
        FeatureConsumptionFix = true;
        FeatureNewCompanies = true;
        FeatureCommercialDemand = true;
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
            { m_Setting.GetSettingsLocaleID(), "Economy Rebalance v0.7" },
            { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

            { m_Setting.GetOptionGroupLocaleID(Setting.kOptionsGroup), "Options" },
            { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Actions" },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Button" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Simple single button. It should be bool property with only setter or use [{nameof(SettingsUIButtonAttribute)}] to make button from bool property with setter and getter" },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ButtonWithConfirmation)), "Button with confirmation" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.ButtonWithConfirmation)), $"Button can show confirmation message. Use [{nameof(SettingsUIConfirmationAttribute)}]" },
            { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ButtonWithConfirmation)), "is it confirmation text which you want to show here?" },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Logging)), "Detailed logging" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.Logging)), "Output more diagnostic information." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeaturePrefabs)), "Enable Prefabs" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeaturePrefabs)), "Enables new prefab params." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeatureConsumptionFix)), "Enable Consumption Fix" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeatureConsumptionFix)), "Enables Consumption Fix." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeatureNewCompanies)), "Enable New Companies" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeatureNewCompanies)), "Enables commercial companies for immaterial resources." },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeatureCommercialDemand)), "Enable Commercial Demand" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeatureCommercialDemand)), "Enables modded commercial demand and dedicated UI." },

        };
    }

    public void Unload()
    {

    }
}
