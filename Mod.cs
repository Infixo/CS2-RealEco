using System;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using RealEco.Config;

namespace RealEco;

public class Mod : IMod
{
    public static readonly string harmonyID = "Infixo." + nameof(RealEco);

    // mod's instance and asset
    public static Mod instance { get; private set; }
    public static ExecutableAsset modAsset { get; private set; }

    // logging
    public static ILog log = LogManager.GetLogger($"{nameof(RealEco)}").SetShowsErrorsInUI(false);

    public static void Log(string text) => log.Info(text);

    public static void LogIf(string text)
    {
        if (setting.Logging) log.Info(text);
    }

    // setting
    public static Setting setting { get; private set; }

    public void OnLoad(UpdateSystem updateSystem)
    {
        instance = this;

        log.Info(nameof(OnLoad));

        if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
        {
            log.Info($"{asset.name} mod asset at {asset.path}");
            modAsset = asset;
            //DumpObjectData(asset);
        }

        setting = new Setting(this);
        setting.RegisterInOptionsUI();
        GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(setting));
        GameManager.instance.localizationManager.AddSource("en-US", new StatsLocaleEN());
        setting._Hidden = false;

        AssetDatabase.global.LoadSettings(nameof(RealEco), setting, new Setting(this));

        // READ AND APPLY CONFIG
        ConfigTool.ReadAndApply();

        // Disable original systems
        /*
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.AgingSystem>().Enabled = false;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.ApplyToSchoolSystem>().Enabled = false;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.BirthSystem>().Enabled = false;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.GraduationSystem>().Enabled = false;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.SchoolAISystem>().Enabled = false;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Citizens.CitizenInitializeSystem>().Enabled = false;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.DeathCheckSystem>().Enabled = Mod.setting.DeathChanceIncrease <= 0;
        */

        // Create modded systems
        //RealPopPatches.Initialize_Postfix(updateSystem); // reuse existing code

        // Harmony
        var harmony = new Harmony(harmonyID);
        harmony.PatchAll(typeof(Mod).Assembly);
        var patchedMethods = harmony.GetPatchedMethods().ToArray();
        log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods.Length);
        foreach (var patchedMethod in patchedMethods)
        {
            log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
        }

        // Patch prefabs
        //PrefabPatcher patcher = new PrefabPatcher();
        //patcher.PatchDemandParameters();
        //patcher.PatchHouseholds();
        //patcher.PatchInitialWealth();


    }

    public void OnDispose()
    {
        log.Info(nameof(OnDispose));
        if (setting != null)
        {
            setting.UnregisterInOptionsUI();
            setting = null;
        }
        // Harmony
        var harmony = new Harmony(harmonyID);
        harmony.UnpatchAll(harmonyID);
    }
    public static void DumpObjectData(object objectToDump)
    {
        //string className = objectToDump.GetType().Name;
        //Mod.log.Info($"{prefab.name}.{objectToDump.name}.CLASS: {className}");
        Mod.log.Info($"Object: {objectToDump}");

        // Fields
        Type type = objectToDump.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            //if (field.Name != "isDirty" && field.Name != "active" && field.Name != "components")
            Mod.log.Info($" {field.Name}: {field.GetValue(objectToDump)}");
        }

        // Properties
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
        {
            Mod.log.Info($" {property.Name}: {property.GetValue(objectToDump)}");
        }
    }
}
