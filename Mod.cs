﻿using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;

namespace RealEco;

public class Mod : IMod
{
    public static readonly string harmonyID = "Infixo." + nameof(RealEco);

    // mod's instance
    public static Mod instance { get; private set; }

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
            log.Info($"Current mod asset at {asset.path}");

        setting = new Setting(this);
        setting.RegisterInOptionsUI();
        GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(setting));
        GameManager.instance.localizationManager.AddSource("en-US", new StatsLocaleEN());
        setting._Hidden = false;

        AssetDatabase.global.LoadSettings(nameof(RealEco), setting, new Setting(this));

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

        // READ CONFIG DATA
        RealEco.Config.ConfigToolXml.LoadConfig();
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
}
