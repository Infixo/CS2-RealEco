using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;

namespace RealEco;

[HarmonyPatch]
public static class PrefabStore_Patches
{
    static bool CompaniesCreated = false;
    static CompanyPrefab[] CompanyPrefabs = new CompanyPrefab[4];

/*
 * [2024-02-27 13:47:58,574] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.CompanyPrefab.zone: Commercial
[2024-02-27 13:47:58,575] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.CompanyPrefab.profitability: 600
[2024-02-27 13:47:58,575] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ServiceCompany.m_MaxService: 5000
[2024-02-27 13:47:58,576] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ServiceCompany.m_MaxWorkersPerCell: 0.5
[2024-02-27 13:47:58,576] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ServiceCompany.m_ServiceConsuming: 1
[2024-02-27 13:47:58,577] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ProcessingCompany.process: Game.Prefabs.IndustrialProcess
[2024-02-27 13:47:58,577] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ProcessingCompany.m_MaxWorkersPerCell: 0
[2024-02-27 13:47:58,578] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ProcessingCompany.m_Output: Electronics 1
[2024-02-27 13:47:58,578] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ProcessingCompany.m_Input1: Electronics 1
[2024-02-27 13:47:58,579] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ProcessingCompany.m_Input2: NoResource 0
[2024-02-27 13:47:58,580] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.ProcessingCompany.transports: 1
[2024-02-27 13:47:58,580] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.StorageLimit.storageLimit: 8000
[2024-02-27 13:47:58,581] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.Workplace.m_Workplaces: 2
[2024-02-27 13:47:58,581] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.Workplace.m_Complexity: Complex
[2024-02-27 13:47:58,582] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.Workplace.m_EveningShiftProbability: 0.25
[2024-02-27 13:47:58,582] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.Workplace.m_NightShiftProbability: 0

 */

    private static CompanyPrefab CreateNewCompanyPrefab(string prefabName, ResourceInEditor resource, WorkplaceComplexity complexity, float maxWorkersPerCell, float profitability)
    {
        // CompanyPrefab
        CompanyPrefab prefab = ScriptableObject.CreateInstance<CompanyPrefab>();
        prefab.name = prefabName;
        prefab.prefab = prefab; // prefab is also a ComponentBase
        prefab.zone = Game.Zones.AreaType.Commercial;
        prefab.profitability = profitability;
        // ServiceCompany
        ServiceCompany serviceCompany = prefab.AddComponent<ServiceCompany>();
        //serviceCompany.name = serviceCompany.GetType().Name;
        //serviceCompany.prefab = prefab;
        serviceCompany.m_MaxService = 5000;
        serviceCompany.m_MaxWorkersPerCell = maxWorkersPerCell;
        // ProcessingCompany
        ProcessingCompany processingCompany = prefab.AddComponent<ProcessingCompany>();
        processingCompany.transports = 1;
        processingCompany.process.m_Output.m_Resource = resource;
        processingCompany.process.m_Output.m_Amount = 1;
        processingCompany.process.m_Input1.m_Resource = resource;
        processingCompany.process.m_Input1.m_Amount = 1;
        // StorageLimit
        StorageLimit storageLimit = prefab.AddComponent<StorageLimit>();
        storageLimit.storageLimit = 8000;
        // Workplace
        Workplace workplace = prefab.AddComponent<Workplace>();
        workplace.m_Workplaces = 2;
        workplace.m_Complexity = complexity;
        workplace.m_EveningShiftProbability = 0.2f;
        workplace.m_NightShiftProbability = 0f;
        Plugin.Log($"New company prefab: {prefabName} {resource} wrk {complexity} wpc {maxWorkersPerCell} prof {profitability}");
        // done
        return prefab;
    }

    // Step 1a. AssetLibrary - there are 3 passes
    // Loading: foreach AssetCollection in m_Collections calls -> collection.AddPrefabsTo(prefabSystem);
    /*
    [HarmonyPatch(typeof(Game.SceneFlow.AssetLibrary), "Load")]
    [HarmonyPrefix]
    public static bool AssetLibrary_Load(AssetLibrary __instance, int ___m_AssetCount)
    {
        Plugin.Log($"AssetLibrary.Load: {__instance.name}, {__instance.m_Collections.Count} collections, {___m_AssetCount} assets");
        return true;
    }
    */

    // Step 1b: AssetLibary -> AssetCollection, mThis is called 1035 times
    // Loading: foreach (PrefabBase prefab in m_Prefabs) calls ->  prefabSystem.AddPrefab(prefab, base.name);
    [HarmonyPatch(typeof(Game.Prefabs.AssetCollection), "AddPrefabsTo")]
    [HarmonyPrefix]
    public static bool AssetCollection_AddPrefabsTo_Prefix(AssetCollection __instance)
    {
        //Plugin.Log($"AssetCollection.AddPrefabsTo: {__instance.name} {__instance.isActive}, {__instance.m_Collections.Count} collections, {__instance.m_Prefabs.Count} prefabs");
        if (Plugin.FeatureNewCompanies.Value && !CompaniesCreated && __instance.name == "CompaniesCollection")
        {
            //Plugin.Log($"Adding new CompanyPrefabs");
            CompanyPrefabs[0] = CreateNewCompanyPrefab("Commercial_SoftwareStore", ResourceInEditor.Software, WorkplaceComplexity.Complex, 0.22f, 70f); // price 85
            CompanyPrefabs[1] = CreateNewCompanyPrefab("Commercial_TelecomStore", ResourceInEditor.Telecom, WorkplaceComplexity.Simple, 0.3f, 60f); // price 60
            CompanyPrefabs[2] = CreateNewCompanyPrefab("Commercial_FinancialStore", ResourceInEditor.Financial, WorkplaceComplexity.Complex, 0.22f, 60f); // price 70
            CompanyPrefabs[3] = CreateNewCompanyPrefab("Commercial_MediaStore", ResourceInEditor.Media, WorkplaceComplexity.Simple, 0.3f, 60f); // price 60
            for (int i = 0; i < CompanyPrefabs.Length; i++)
                __instance.m_Prefabs.Add(CompanyPrefabs[i]);
            CompaniesCreated = true;
        }
        return true;
    }

    // Step 1c:  prefabSystem.AddPrefab(prefab, base.name) -> usual patched method
    [HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    [HarmonyPrefix]
    public static bool ZonePrefab_Prefix(PrefabBase prefab)
    {
        if (Plugin.FeatureNewCompanies.Value && prefab.GetType().Name == "ZonePrefab" && prefab.TryGet<ZoneProperties>(out ZoneProperties comp) && comp.m_AllowedSold.Length > 0)
        {
            if (Array.IndexOf(comp.m_AllowedSold, ResourceInEditor.Software) < 0)
            {
                List<ResourceInEditor> tempList = new List<ResourceInEditor>(comp.m_AllowedSold); // Convert the array to a list
                // Add the new resource to the list
                tempList.Add(ResourceInEditor.Software);
                tempList.Add(ResourceInEditor.Telecom);
                tempList.Add(ResourceInEditor.Financial);
                tempList.Add(ResourceInEditor.Media);
                comp.m_AllowedSold = tempList.ToArray(); // Convert the list back to an array

                Game.Economy.Resource res = Game.Economy.EconomyUtils.GetResources(comp.m_AllowedSold);
                Plugin.Log($"{prefab.name}.{comp.name}.m_AllowedSold: {Game.Economy.EconomyUtils.GetNames(res)}");
            }
        }
        return true;
    }

    // Need to remember resources to patch later statistics and taxes
    static Dictionary<Game.Economy.ResourceInEditor, ResourcePrefab> ResourcePrefabs = new();

    // Step 1c:  prefabSystem.AddPrefab(prefab, base.name) -> usual patched method
    [HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    [HarmonyPrefix]
    public static bool ResourcePrefab_Prefix(PrefabBase prefab)
    {
        if (Plugin.FeatureNewCompanies.Value && prefab.GetType().Name == "ResourcePrefab" && prefab.TryGet<TaxableResource>(out TaxableResource comp))
        {
            if (comp.m_TaxAreas.Length == 1 && comp.m_TaxAreas[0] == TaxAreaType.Office)
            {
                comp.m_TaxAreas = [TaxAreaType.Office, TaxAreaType.Commercial];
                // show in the log
                string text = "";
                for (int i = 0; i < comp.m_TaxAreas.Length; i++)
                    text += comp.m_TaxAreas[i] + "|";
                if (comp.m_TaxAreas.Length == 0)
                    text = "None";
                Plugin.Log($"{prefab.name}.{comp.name}.m_TaxAreas: {text}");
                // store the prefab for later use
                ResourcePrefab resPrefab = prefab as ResourcePrefab;
                if (!ResourcePrefabs.ContainsKey(resPrefab.m_Resource))
                {
                    ResourcePrefabs.Add(resPrefab.m_Resource, resPrefab);
                    //Plugin.Log($"...storing {prefab.name} for later use (total {ResourcePrefabs.Count})");
                }
            }
        }
        return true;
    }


    static Dictionary<Game.City.StatisticType, bool> StatisticsTypesToPatch = new()
    {
        {  Game.City.StatisticType.CommercialTaxableIncome, true },
        {  Game.City.StatisticType.ServiceCount, true },
        {  Game.City.StatisticType.ServiceWealth, true },
        {  Game.City.StatisticType.ServiceWorkers, true },
        {  Game.City.StatisticType.ServiceMaxWorkers, true },
    };

    // Step 1c:  prefabSystem.AddPrefab(prefab, base.name) -> usual patched method
    [HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    [HarmonyPrefix]
    public static bool ResourceStatistic_Prefix(PrefabBase prefab)
    {
        if (Plugin.FeatureNewCompanies.Value && prefab.GetType() == typeof(ResourceStatistic) && ResourcePrefabs.Count == 4)
        {
            ResourceStatistic stat = prefab as ResourceStatistic;
            if (StatisticsTypesToPatch.ContainsKey(stat.m_StatisticsType) && StatisticsTypesToPatch[stat.m_StatisticsType])
            {
                Plugin.Log($"{prefab.name}.{stat.GetType().Name}.m_StatisticsType: {stat.m_StatisticsType}");
                // add new resources
                List<ResourcePrefab> tempList = new List<ResourcePrefab>(stat.m_Resources); // Convert the array to a list
                // Add the new resources to the list
                tempList.Add(ResourcePrefabs[ResourceInEditor.Software]);
                tempList.Add(ResourcePrefabs[ResourceInEditor.Telecom]);
                tempList.Add(ResourcePrefabs[ResourceInEditor.Financial]);
                tempList.Add(ResourcePrefabs[ResourceInEditor.Media]);
                stat.m_Resources = tempList.ToArray(); // Convert the list back to an array
                StatisticsTypesToPatch[stat.m_StatisticsType] = false;
                // show in the log
                string text = "";
                for (int i = 0; i < stat.m_Resources.Length; i++)
                    text += stat.m_Resources[i].m_Resource.ToString() + "|";
                if (stat.m_Resources.Length == 0)
                    text = "None";
                Plugin.Log($"{prefab.name}.{stat.GetType().Name}.m_Resources: {text}");
            }
        }
        return true;
    }


    static Dictionary<string, int> BrandsToPatch = new()
    {
        // -1 means it is patched
        // 0 - Software
        { "Cebeliverse", 0 },
        { "ChirpyTeck", 0 },
        { "DennyAlsLaw", 0 },
        { "DLCHut", 0 },
        { "FaultStudios", 0 },
        { "FixedTraffic", 0 },
        { "LuiboDigital", 0 },
        { "Speltware", 0 },
        { "Szoftver", 0 },
        { "TechOMat", 0 },
        // 1 - Telecom
        { "Kapine", 1 },
        { "LehtoElectronics", 1 },
        { "Pteropus", 1 },
        // 2 - Financial
        { "BanhammerBank", 2 },
        { "CRIMoore", 2 },
        { "Pihi", 2 },
        { "SnafuInsurance", 2 },
        { "StadelmannAndBardolf", 2 },
        { "THELawAndAccounting", 2 },
        // 3 - Media
        { "AshtrainRecords", 3 },
        { "BendyLetters", 3 },
        { "BootAndBug", 3 },
        { "IndieLizard", 3 },
        { "MouthwaterPress", 3 },
        { "Placesstages", 3 },
        { "PNGMedia", 3 },
        { "SingTapeRecords", 3 },
    };

    // Step 1c:  prefabSystem.AddPrefab(prefab, base.name) -> usual patched method
    [HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    [HarmonyPrefix]
    public static bool BrandPrefab_Prefix(PrefabBase prefab)
    {
        if (Plugin.FeatureNewCompanies.Value && prefab.GetType() == typeof(BrandPrefab) && CompaniesCreated)
        {
            BrandPrefab brand = prefab as BrandPrefab;
            if (BrandsToPatch.ContainsKey(brand.name) && BrandsToPatch[brand.name] != -1)
            {
                //Plugin.Log($"{prefab.name}.{brand.GetType().Name}: patching");
            
                // add new company type to the brand
                List<CompanyPrefab> tempList = new List<CompanyPrefab>(brand.m_Companies); // Convert the array to a list
                tempList.Add(CompanyPrefabs[BrandsToPatch[brand.name]]);
                brand.m_Companies = tempList.ToArray(); // Convert the list back to an array
                BrandsToPatch[brand.name] = -1;
            
                // show in the log
                string text = "";
                for (int i = 0; i < brand.m_Companies.Length; i++)
                    text += brand.m_Companies[i].name + "|";
                if (brand.m_Companies.Length == 0)
                    text = "None";
                Plugin.Log($"{prefab.name}.{brand.GetType().Name}: {text}");
            }
        }
        return true;
    }

    // Step 2: Creation of Systems

    // Step 3: This is called 1 time
    /*
    [HarmonyPatch(typeof(Game.SceneFlow.GameManager), "LoadPrefabs")]
    [HarmonyPrefix]
    public static bool LoadPrefabs_Postfix()
    {
        Plugin.Log("*** Game.SceneFlow.GameManager.LoadPrefabs");
        return true;
    }
    */
    // Step 4: This is called 1 time, at the very end, even after modded systems are created
    // Some prefabs are added here e.g. "Building Land Value"
    /*
    [HarmonyPatch(typeof(Game.Prefabs.PrefabInitializeSystem), "OnUpdate")]
    [HarmonyPrefix]
    public static bool OnUpdate_Postfix()
    {
        Plugin.Log("*** PrefabInitializeSystem.OnUpdate ***");
        return true;
    }
    */
}


/*
[2024-03-07 19:33:35,478] [INFO]  AssetLibrary.Load: GameAssetLibrary, 154 collections, 0 assets

    [2024-03-07 19:33:35,480] [INFO]  AssetCollection.AddPrefabsTo: SettingsCollection True, 0 collections, 55 prefabs

        [2024-03-07 19:33:35,486] [INFO]  EconomyParameters.EconomyPrefab.m_ExtractorCompanyExportMultiplier: 0.7
        [2024-03-07 19:33:35,486] [INFO]  EconomyParameters.EconomyPrefab.m_IndustrialProfitFactor: 0.0008
        [2024-03-07 19:33:35,487] [INFO]  EconomyParameters.EconomyPrefab.m_Wage0: 1330
        [2024-03-07 19:33:35,493] [INFO]  EconomyParameters.EconomyPrefab.m_UnemploymentBenefit: 600
        [2024-03-07 19:33:35,510] [INFO]  Modded ServiceFeeParameters: GarbageFee 0.4

    [2024-03-07 19:33:35,516] [INFO]  AssetCollection.AddPrefabsTo: ZonesCollection True, 0 collections, 19 prefabs

        [2024-03-07 19:33:35,517] [INFO]  EU Commercial High.ZoneProperties.m_SpaceMultiplier: 4
        [2024-03-07 19:33:35,519] [INFO]  EU Commercial Low.ZoneProperties.m_SpaceMultiplier: 1.2
        [2024-03-07 19:33:35,522] [INFO]  EU Residential Mixed.ZoneServiceConsumption.m_Upkeep: 330
        [2024-03-07 19:33:35,523] [INFO]  Industrial Manufacturing.ZoneProperties.m_SpaceMultiplier: 1.3

    [2024-03-07 19:33:35,542] [INFO]  AssetCollection.AddPrefabsTo: EventsCollection True, 0 collections, 15 prefabs
    [2024-03-07 19:33:35,546] [INFO]  AssetCollection.AddPrefabsTo: RoutesCollection True, 0 collections, 9 prefabs
    [2024-03-07 19:33:35,548] [INFO]  AssetCollection.AddPrefabsTo: CitizensCollection True, 0 collections, 16 prefabs
    [2024-03-07 19:33:35,567] [INFO]  AssetCollection.AddPrefabsTo: InfoViewsCollection True, 0 collections, 122 prefabs
    [2024-03-07 19:33:35,573] [INFO]  AssetCollection.AddPrefabsTo: MilestonesCollection True, 0 collections, 20 prefabs

    [2024-03-07 19:33:35,575] [INFO]  AssetCollection.AddPrefabsTo: CompaniesCollection True, 0 collections, 79 prefabs

        [2024-03-07 19:33:35,575] [INFO]  Commercial_FoodStore.CompanyPrefab.profitability: 80
        [2024-03-07 19:33:35,576] [INFO]  Commercial_FoodStore.Workplace.m_Complexity: Manual
        [2024-03-07 19:33:35,579] [INFO]  Industrial_FoodFactory.CompanyPrefab.profitability: 45
        [2024-03-07 19:33:35,580] [INFO]  Industrial_FoodFactory.IndustrialProcess: wpc 0.5 output 3
 */