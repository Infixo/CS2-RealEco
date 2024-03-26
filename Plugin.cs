using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Colossal.Logging;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using HookUILib.Core;
using Game.UI.InGame;
using Unity.Entities;
using Game;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace RealEco;

[HarmonyPatch]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger; // BepInEx logging
    private static ILog s_Log; // CO logging

    public static void LogIf(string text, bool bMethod = false)
    {
        if (Logging.Value) Log(text, bMethod);
    }

    public static void Log(string text, bool bMethod = false)
    {
        if (bMethod) text = GetCallingMethod(2) + ": " + text;
        Logger.LogInfo(text);
        s_Log.Info(text);
    }

    public static void LogStack(string text)
    {
        //string msg = GetCallingMethod(2) + ": " + text + " STACKTRACE";
        Logger.LogInfo(text + " STACKTRACE");
        s_Log.logStackTrace = true;
        s_Log.Info(text + "STACKTRACE");
        s_Log.logStackTrace = false;
    }

    /// <summary>
    /// Gets the method from the specified <paramref name="frame"/>.
    /// </summary>
    public static string GetCallingMethod(int frame)
    {
        StackTrace st = new StackTrace();
        MethodBase mb = st.GetFrame(frame).GetMethod(); // 0 - GetCallingMethod, 1 - Log, 2 - actual function calling a Log method
        return mb.DeclaringType + "." + mb.Name;
    }

    // Mod settings
    public static ConfigEntry<bool> Logging;
    public static ConfigEntry<bool> ConfigDump;
    public static ConfigEntry<bool> FeaturePrefabs;
    public static ConfigEntry<bool> FeatureConsumptionFix;
    public static ConfigEntry<bool> FeatureNewCompanies;
    public static ConfigEntry<bool> FeatureCommercialDemand;

    internal static Mod Mod;

    private void Awake()
    {
        // Create mod
        Mod = new Mod();

        Logger = base.Logger;

        // CO logging standard as described here https://cs2.paradoxwikis.com/Logging
        s_Log = Mod.log; //  LogManager.GetLogger(MyPluginInfo.PLUGIN_NAME);

        // Mod settings
        Logging = base.Config.Bind<bool>("Debug", "Logging", false, "Enables detailed logging.");
        ConfigDump = base.Config.Bind<bool>("Debug", "ConfigDump", false, "Saves configuration to a secondary xml file.");
        FeaturePrefabs = base.Config.Bind<bool>("Features", "FeaturePrefabs", true, "Enables new prefab params.");
        FeatureConsumptionFix = base.Config.Bind<bool>("Features", "FeatureConsumptionFix", true, "Enables Consumption Fix.");
        FeatureNewCompanies = base.Config.Bind<bool>("Features", "FeatureNewCompanies", false, "Enables commercial companies for immaterial resources.");
        FeatureCommercialDemand = base.Config.Bind<bool>("Features", "FeatureCommercialDemand", false, "Enables modded commercial demand and dedicated UI.");

        Log($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony");
        var patchedMethods = harmony.GetPatchedMethods().ToArray();

        Log($"Plugin {MyPluginInfo.PLUGIN_GUID} made patches! Patched methods: " + patchedMethods.Length);

        foreach (var patchedMethod in patchedMethods) {
            Log($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
        }

        // READ CONFIG DATA
        RealEco.Config.ConfigToolXml.LoadConfig();
    }

    // Simulate Mod.OnLoad
    [HarmonyPatch(typeof(Game.Modding.ModManager), "InitializeMods")]
    [HarmonyPostfix]
    public static void InitializeMods(UpdateSystem updateSystem)
    {
        if (Mod != null)
            Mod.OnLoad(updateSystem);
        else
            Log($"Mod not created");
    }

}

public class RealEco_Commercial : UIExtension
{
    public new readonly string extensionID = "realeco.commercial";
    public new readonly string extensionContent;
    public new readonly ExtensionType extensionType = ExtensionType.Panel;

    public RealEco_Commercial()
    {
        this.extensionContent = this.LoadEmbeddedResource("RealEco.dist.commercial.js");
    }
}
