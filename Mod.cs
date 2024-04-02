using System;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using RealEco.Config;
using Game.Prefabs;
using Game.Economy;
using Game.Common;

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
        setting._Hidden = false;

        AssetDatabase.global.LoadSettings(nameof(RealEco), setting, new Setting(this));

        // Harmony
        var harmony = new Harmony(harmonyID);
        harmony.PatchAll(typeof(Mod).Assembly);
        var patchedMethods = harmony.GetPatchedMethods().ToArray();
        log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods.Length);
        foreach (var patchedMethod in patchedMethods)
        {
            log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
        }

        // NEW COMPANIES
        if (setting.FeatureNewCompanies) PrefabStore.CreateNewCompanies();

        // READ AND APPLY CONFIG
        if (setting.FeaturePrefabs) ConfigTool.ReadAndApply();

        // 240401 We now have to siumulate initialization of core economy systems, this section might grow in the future
        //ReinitializeResources();
        ReinitializeCompanies();

        // Disable original systems
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.HouseholdBehaviorSystem>().Enabled = !Mod.setting.FeatureConsumptionFix;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.ResourceBuyerSystem>().Enabled = !Mod.setting.FeatureNewCompanies;

        // Create modded systems
        RealEco.Patches.Initialize_Postfix(updateSystem); // reuse existing code

        ConfigTool.isLatePrefabsActive = true; // enable processing of late-added prefabs
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

    // TODO: this must be converted to Job to avoid partial inits that run only on a subset of resources
    /// <summary>
    /// This will run ResourceSystem.OnUpdate() again on all resources to update Entities and refresh m_BaseConsumptionSum.
    /// </summary>
    public static void ReinitializeResources()
    {
        Mod.Log("Marking Resources as Created for re-initialization.");
        EntityManager entityManager= World.DefaultGameObjectInjectionWorld.EntityManager;
        ResourceSystem resourceSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ResourceSystem>();
        ResourceIterator iterator = ResourceIterator.GetIterator();
        while (iterator.Next())
        {
            Entity resourcePrefab = resourceSystem.GetPrefab(iterator.resource);
            entityManager.SetComponentData<Created>(resourcePrefab, default(Created));
            Mod.LogIf($"... {iterator.resource} is Created.");
        }
    }

    // This may run in parts because the calculations are only for single companies, there is no roll-up.
    // However, it also needs a system and it must run AFTER the Resources are refreshed.
    /// <summary>
    /// This will run CompanyPrefabInitializeSystem.OnUpdate() because we are changing profitabilities and economy parameters.
    /// </summary>
    public static void ReinitializeCompanies()
    {
        Mod.Log("Marking Companies as Created for re-initialization.");
        PrefabSystem m_PrefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        foreach (PrefabXml prefabXml in ConfigToolXml.Config.Prefabs)
            if (prefabXml.Type == "CompanyPrefab")
            {
                PrefabID prefabID = new PrefabID(prefabXml.Type, prefabXml.Name);
                if (m_PrefabSystem.TryGetPrefab(prefabID, out PrefabBase prefab) && m_PrefabSystem.TryGetEntity(prefab, out Entity entity))
                {
                    entityManager.SetComponentData<Created>(entity, default(Created));
                    Mod.LogIf($"... {prefab.name} is Created.");
                }
                else
                    Mod.log.Warn($"Failed to retrieve {prefabXml} from the PrefabSystem.");
            }
    }
}
