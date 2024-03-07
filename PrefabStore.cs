using System;
using UnityEngine;
using Game.Economy;
using Game.Prefabs;
using Game.SceneFlow;
using HarmonyLib;
using System.Collections.Generic;

namespace RealEco;

[HarmonyPatch]
public static class PrefabStore_Patches
{
    private static bool isAdded = false;

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

    private static CompanyPrefab CreateNewCompanyPrefab(string prefabName, ResourceInEditor resource, WorkplaceComplexity complexity, float maxWorkersPerCell)
    {
        // CompanyPrefab
        CompanyPrefab prefab = ScriptableObject.CreateInstance<CompanyPrefab>();
        prefab.name = prefabName;
        prefab.prefab = prefab; // prefab is also a ComponentBase
        prefab.zone = Game.Zones.AreaType.Commercial;
        prefab.profitability = 100f;
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
        // done
        return prefab;
    }

    // Step 1a. AssetLibrary - there are 3 passes
    // Loading: foreach AssetCollection in m_Collections calls -> collection.AddPrefabsTo(prefabSystem);
    [HarmonyPatch(typeof(Game.SceneFlow.AssetLibrary), "Load")]
    [HarmonyPrefix]
    public static bool AssetLibrary_Load(AssetLibrary __instance, int ___m_AssetCount)
    {
        Plugin.Log($"AssetLibrary.Load: {__instance.name}, {__instance.m_Collections.Count} collections, {___m_AssetCount} assets");
        return true;
    }

    // Step 1b: AssetLibary -> AssetCollection, mThis is called 1035 times
    // Loading: foreach (PrefabBase prefab in m_Prefabs) calls ->  prefabSystem.AddPrefab(prefab, base.name);
    [HarmonyPatch(typeof(Game.Prefabs.AssetCollection), "AddPrefabsTo")]
    [HarmonyPrefix]
    public static bool AssetCollection_AddPrefabsTo_Prefix(AssetCollection __instance)
    {
        Plugin.Log($"AssetCollection.AddPrefabsTo: {__instance.name} {__instance.isActive}, {__instance.m_Collections.Count} collections, {__instance.m_Prefabs.Count} prefabs");
        if (!isAdded && __instance.name == "CompaniesCollection")
        {
            Plugin.Log($"Adding new CompanyPrefabs");
            __instance.m_Prefabs.Add(CreateNewCompanyPrefab("Commercial_SoftwareStore", ResourceInEditor.Software, WorkplaceComplexity.Complex, 0.4f));
            __instance.m_Prefabs.Add(CreateNewCompanyPrefab("Commercial_TelecomStore", ResourceInEditor.Telecom, WorkplaceComplexity.Complex, 0.4f));
            __instance.m_Prefabs.Add(CreateNewCompanyPrefab("Commercial_FinancialStore", ResourceInEditor.Financial, WorkplaceComplexity.Complex, 0.4f));
            __instance.m_Prefabs.Add(CreateNewCompanyPrefab("Commercial_MediaStore", ResourceInEditor.Media, WorkplaceComplexity.Complex, 0.4f));
            isAdded = true;
        }
        return true;
    }

    // Step 1c:  prefabSystem.AddPrefab(prefab, base.name) -> usual patched method
    [HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    [HarmonyPrefix]
    public static bool ZonePrefab_Prefix(PrefabBase prefab)
    {
        static void AddResource(ref ResourceInEditor[] array, ResourceInEditor resource)
        {
            if (Array.IndexOf(array, resource) < 0)
            {
                // Convert the array to a list
                List<ResourceInEditor> tempList = new List<ResourceInEditor>(array);

                // Add the new resource to the list
                tempList.Add(resource);

                // Convert the list back to an array
                array = tempList.ToArray();
                Plugin.Log($"... {resource} added to the array");
            }
            else
            {
                Plugin.Log($"... {resource} already in the array");
            }
        }

        if (prefab.GetType().Name == "ZonePrefab" && prefab.TryGet<ZoneProperties>(out ZoneProperties comp) && comp.m_AllowedSold.Length > 0)
        {
            if (Array.IndexOf(comp.m_AllowedSold, ResourceInEditor.Software) < 0)
            {
                // not working...
                //comp.m_AllowedSold.AddItem<ResourceInEditor>(ResourceInEditor.Software);
                //if (Array.IndexOf(comp.m_AllowedSold, ResourceInEditor.Telecom) < 0)
                //comp.m_AllowedSold.AddItem<ResourceInEditor>(ResourceInEditor.Telecom);
                //if (Array.IndexOf(comp.m_AllowedSold, ResourceInEditor.Financial) < 0)
                //comp.m_AllowedSold.AddItem<ResourceInEditor>(ResourceInEditor.Financial);
                //if (Array.IndexOf(comp.m_AllowedSold, ResourceInEditor.Media) < 0)
                //comp.m_AllowedSold.AddItem<ResourceInEditor>(ResourceInEditor.Media);
                
                List<ResourceInEditor> tempList = new List<ResourceInEditor>(comp.m_AllowedSold); // Convert the array to a list
                // Add the new resource to the list
                tempList.Add(ResourceInEditor.Software);
                tempList.Add(ResourceInEditor.Telecom);
                tempList.Add(ResourceInEditor.Financial);
                tempList.Add(ResourceInEditor.Media);
                comp.m_AllowedSold = tempList.ToArray(); // Convert the list back to an array

                Game.Economy.Resource res = Game.Economy.EconomyUtils.GetResources(comp.m_AllowedSold);
                Plugin.Log($"{prefab.GetType().Name}.{prefab.name}.{comp.name}.m_AllowedSold: {Game.Economy.EconomyUtils.GetNames(res)}");
            }
            //AddResource(ref comp.m_AllowedSold, ResourceInEditor.Software);
            //AddResource(ref comp.m_AllowedSold, ResourceInEditor.Telecom);
            //AddResource(ref comp.m_AllowedSold, ResourceInEditor.Financial);
            //AddResource(ref comp.m_AllowedSold, ResourceInEditor.Media);
        }
        return true;
    }

    // Step 2: Creation of Systems

    // Step 3: This is called 1 time
    [HarmonyPatch(typeof(Game.SceneFlow.GameManager), "LoadPrefabs")]
    [HarmonyPrefix]
    public static bool LoadPrefabs_Postfix()
    {
        Plugin.Log("*** Game.SceneFlow.GameManager.LoadPrefabs");
        return true;
    }

    // Step 4: This is called 1 time, at the very end, even after modded systems are created
    // Some prefabs are added here e.g. "Building Land Value"
    [HarmonyPatch(typeof(Game.Prefabs.PrefabInitializeSystem), "OnUpdate")]
    [HarmonyPrefix]
    public static bool OnUpdate_Postfix()
    {
        Plugin.Log("*** PrefabInitializeSystem.OnUpdate ***");
        return true;
    }

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