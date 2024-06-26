using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Game.SceneFlow;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
//using HarmonyLib;

namespace RealEco;

//[HarmonyPatch]
public static class PrefabStore
{
    private static PrefabSystem m_PrefabSystem;
    private static EntityManager m_EntityManager;

    public static bool TryGetPrefabAndEntity(string prefabType, string prefabName, out PrefabBase prefab, out Entity entity)
    {
        PrefabID prefabID = new PrefabID(prefabType, prefabName);
        if (m_PrefabSystem.TryGetPrefab(prefabID, out PrefabBase prefabTmp) && m_PrefabSystem.TryGetEntity(prefabTmp, out Entity entityTmp))
        {
            prefab = prefabTmp;
            entity = entityTmp;
            return true;
        }
        prefab = null;
        entity = default(Entity);
        return false;
    }

    //static bool CompaniesCreated = false;
    static CompanyPrefab[] CompanyPrefabs = new CompanyPrefab[4];

/*
[2024-02-27 13:47:58,574] [INFO]  CompanyPrefab.Commercial_ElectronicsStore.CompanyPrefab.zone: Commercial
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
        Mod.Log($"New company prefab: {prefabName} {resource} wrk {complexity} wpc {maxWorkersPerCell} prof {profitability}");
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
        Mod.Log($"AssetLibrary.Load: {__instance.name}, {__instance.m_Collections.Count} collections, {___m_AssetCount} assets");
        return true;
    }
    */

    // Step 1b: AssetLibary -> AssetCollection, mThis is called 1035 times
    // Loading: foreach (PrefabBase prefab in m_Prefabs) calls ->  prefabSystem.AddPrefab(prefab, base.name);
    //[HarmonyPatch(typeof(Game.Prefabs.AssetCollection), "AddPrefabsTo")]
    //[HarmonyPrefix]
    public static bool AssetCollection_AddPrefabsTo_Prefix(AssetCollection __instance)
    {
        //Mod.Log($"AssetCollection.AddPrefabsTo: {__instance.name} {__instance.isActive}, {__instance.m_Collections.Count} collections, {__instance.m_Prefabs.Count} prefabs");
        //if (Mod.setting.FeatureNewCompanies && !CompaniesCreated && __instance.name == "CompaniesCollection")
        //{
        //Mod.Log($"Adding new CompanyPrefabs");
        CompanyPrefabs[0] = CreateNewCompanyPrefab("Commercial_SoftwareStore", ResourceInEditor.Software, WorkplaceComplexity.Complex, 0.45f, 350f); // price 85
        CompanyPrefabs[1] = CreateNewCompanyPrefab("Commercial_TelecomStore", ResourceInEditor.Telecom, WorkplaceComplexity.Complex, 0.45f, 450f); // price 60
        CompanyPrefabs[2] = CreateNewCompanyPrefab("Commercial_FinancialStore", ResourceInEditor.Financial, WorkplaceComplexity.Complex, 0.50f, 400f); // price 70
        CompanyPrefabs[3] = CreateNewCompanyPrefab("Commercial_MediaStore", ResourceInEditor.Media, WorkplaceComplexity.Complex, 0.45f, 500f); // price 60

        // 240405 Company Brands
        AddCompanyBrands(CompanyPrefabs[0], BrandsSoftware);
        AddCompanyBrands(CompanyPrefabs[1], BrandsTelecom);
        AddCompanyBrands(CompanyPrefabs[2], BrandsFinancial);
        AddCompanyBrands(CompanyPrefabs[3], BrandsMedia);

        for (int i = 0; i < CompanyPrefabs.Length; i++)
                m_PrefabSystem.AddPrefab(CompanyPrefabs[i], "CompaniesCollection");
        //CompaniesCreated = true;
        //}
        return true;
    }

    // Step 1c:  prefabSystem.AddPrefab(prefab, base.name) -> usual patched method
    //[HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    //[HarmonyPrefix]
    public static bool ZonePrefab_Prefix(PrefabBase prefab, Entity entity)
    {
        if (Mod.setting.FeatureNewCompanies && prefab.GetType().Name == "ZonePrefab" && prefab.TryGet<ZoneProperties>(out ZoneProperties comp) && comp.m_AllowedSold.Length > 0)
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
                Mod.Log($"{prefab.name}.{comp.name}.m_AllowedSold: {Game.Economy.EconomyUtils.GetNames(res)}");

                // patch Entity
                comp.Initialize(m_EntityManager, entity);

                // debug check
                if (m_PrefabSystem.TryGetComponentData<ZonePropertiesData>(prefab, out ZonePropertiesData data))
                    Mod.LogIf($"{prefab.name}.ZonePropertiesData.m_AllowedSold: {EconomyUtils.GetNames(data.m_AllowedSold)}");
            }
        }
        return true;
    }

    static string[] ZonesToPatch = new string[] {
        "EU Commercial High", "EU Commercial Low", "EU Residential Mixed",
        "NA Commercial High", "NA Commercial Low", "NA Residential Mixed",
    };

    static void PatchZones()
    {
        foreach (string zoneName in ZonesToPatch)
            if (TryGetPrefabAndEntity(nameof(ZonePrefab), zoneName, out PrefabBase prefab, out Entity entity))
        //{
            //PrefabID prefabID = new PrefabID(nameof(ZonePrefab), zoneName);
            //if (m_PrefabSystem.TryGetPrefab(prefabID, out PrefabBase prefab) && m_PrefabSystem.TryGetEntity(prefab, out Entity entity))
                _ = ZonePrefab_Prefix(prefab, entity);
        //}
    }

    // Need to remember resources to patch later statistics and taxes
    static Dictionary<Game.Economy.ResourceInEditor, ResourcePrefab> ResourcePrefabs = new();

    // Step 1c:  prefabSystem.AddPrefab(prefab, base.name) -> usual patched method
    //[HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    //[HarmonyPrefix]
    public static bool ResourcePrefab_Prefix(PrefabBase prefab, Entity entity)
    {
        if (Mod.setting.FeatureNewCompanies && prefab.GetType().Name == "ResourcePrefab" && prefab.TryGet<TaxableResource>(out TaxableResource comp))
        {
            bool isPatched = false;
            if (comp.m_TaxAreas.Length == 1 && comp.m_TaxAreas[0] == TaxAreaType.Office)
            {
                comp.m_TaxAreas = new TaxAreaType[2] { TaxAreaType.Office, TaxAreaType.Commercial };
                isPatched = true;
                // store the prefab for later use
                ResourcePrefab resPrefab = prefab as ResourcePrefab;
                if (!ResourcePrefabs.ContainsKey(resPrefab.m_Resource))
                {
                    ResourcePrefabs.Add(resPrefab.m_Resource, resPrefab);
                    //Mod.Log($"...storing {prefab.name} for later use (total {ResourcePrefabs.Count})");
                }
            }
            // 240312 Fix for non-taxable Gas Stations ResourcePetrochemicals.TaxableResource.m_TaxAreas: Industrial |
            if (prefab.name == "ResourcePetrochemicals")
            {
                comp.m_TaxAreas = new TaxAreaType[2] { TaxAreaType.Commercial, TaxAreaType.Industrial };
                isPatched = true;
            }
            if (isPatched)
            {
                // show in the log
                string text = "";
                for (int i = 0; i < comp.m_TaxAreas.Length; i++)
                    text += comp.m_TaxAreas[i] + "|";
                if (comp.m_TaxAreas.Length == 0)
                    text = "None";
                Mod.Log($"{prefab.name}.{comp.name}.m_TaxAreas: {text}");

                // patch Entity
                comp.Initialize(m_EntityManager, entity);

                // debug check
                if (m_PrefabSystem.TryGetComponentData<TaxableResourceData>(prefab, out TaxableResourceData data))
                    Mod.LogIf($"{prefab.name}.TaxableResourceData.m_TaxAreas: {data.m_TaxAreas}");
            }
        }
        return true;
    }

    static string[] ResourcesToPatch = new string[] { "ResourceSoftware", "ResourceTelecom", "ResourceFinancial", "ResourceMedia", "ResourcePetrochemicals" };

    static void PatchResources()
    {
        foreach (string prefabName in ResourcesToPatch)
            if (TryGetPrefabAndEntity(nameof(ResourcePrefab), prefabName, out PrefabBase prefab, out Entity entity))
                _ = ResourcePrefab_Prefix(prefab, entity);
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
    //[HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
    //[HarmonyPrefix]
    public static bool ResourceStatistic_Prefix(PrefabBase prefab, Entity entity)
    {
        if (Mod.setting.FeatureNewCompanies && prefab.GetType() == typeof(ResourceStatistic) && ResourcePrefabs.Count == 4)
        {
            ResourceStatistic stat = prefab as ResourceStatistic;
            if (StatisticsTypesToPatch.ContainsKey(stat.m_StatisticsType) && StatisticsTypesToPatch[stat.m_StatisticsType])
            {
                Mod.Log($"{prefab.name}.{stat.GetType().Name}.m_StatisticsType: {stat.m_StatisticsType}");
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
                Mod.Log($"{prefab.name}.{stat.GetType().Name}.m_Resources: {text}");

                // patch Entity
                m_EntityManager.GetBuffer<StatisticParameterData>(entity).Clear(); // prevents duplicated entries
                stat.LateInitialize(m_EntityManager, entity);

                // debug check
                if (Mod.setting.Logging && m_PrefabSystem.TryGetBuffer<StatisticParameterData>(prefab, true, out DynamicBuffer<StatisticParameterData> data))
                    for(int i = 0; i < data.Length; i++)
                        Mod.Log($"{prefab.name}.StatisticParameterData[{i}]: {data[i].m_Value}");
            }
        }
        return true;
    }

    static string[] StatisticsToPatch = new string[] { "ServiceTaxableIncome", "ServiceCount", "ServiceWealth", "ServiceWorkers", "ServiceMaxWorkers" };

    static void PatchStatistics()
    {
        foreach (string prefabName in StatisticsToPatch)
            if (TryGetPrefabAndEntity(nameof(ResourceStatistic), prefabName, out PrefabBase prefab, out Entity entity))
                _ = ResourceStatistic_Prefix(prefab, entity);
    }

    /* 240405 NOT USED

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
        if (Mod.setting.FeatureNewCompanies && prefab.GetType() == typeof(BrandPrefab) && CompaniesCreated)
        {
            BrandPrefab brand = prefab as BrandPrefab;
            if (BrandsToPatch.ContainsKey(brand.name) && BrandsToPatch[brand.name] != -1)
            {
                //Mod.Log($"{prefab.name}.{brand.GetType().Name}: patching");
            
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
                Mod.Log($"{prefab.name}.{brand.GetType().Name}: {text}");
            }
        }
        return true;
    }
    */

    // 240405 Initialize Company Brands - cannot use AddPrefab on BrandPrefab as it runs before the mod

    public static readonly string[] BrandsSoftware = new string[] {
        "Cebeliverse",
        "ChirpyTeck",
        "DennyAlsLaw",
        "DLCHut",
        "FaultStudios",
        "FixedTraffic",
        "LuiboDigital",
        "Speltware",
        "Szoftver",
        "TechOMat" };

    public static readonly string[] BrandsTelecom = new string[] {
        "Kapine", 
        "LehtoElectronics",
        "Pteropus" };

    public static readonly string[] BrandsFinancial = new string[] {
        "BanhammerBank",
        "CRIMoore",
        "Pihi",
        "SnafuInsurance",
        "StadelmannAndBardolf",
        "THELawAndAccounting" };

    public static readonly string[] BrandsMedia = new string[] {
        "AshtrainRecords",
        "BendyLetters",
        "BootAndBug",
        "IndieLizard",
        "MouthwaterPress",
        "Placesstages",
        "PNGMedia",
        "SingTapeRecords" };

    static void AddCompanyBrands(PrefabBase companyPrefab, string[] brands)
    {
        Mod.LogIf($"Brands: Adding brands to {companyPrefab.name}.");
        /*
        if (m_PrefabSystem.TryGetEntity(companyPrefab, out var companyEntity))
        {
            Mod.log.Warn($"Brands: Failed to retrieve entity for {companyPrefab.name}. Brands not added.");
            return;
        }
        */
        foreach (string brandName in brands)
        {
            if (TryGetPrefabAndEntity("BrandPrefab", brandName, out PrefabBase brandPrefab, out Entity brandEntity))
            {
                // STEP 1 - add company to the brand prefab
                Mod.LogIf($"{brandPrefab.GetType().Name}.{brandPrefab.name}: patching");

                // add new company type to the brand
                List<CompanyPrefab> tempList = new List<CompanyPrefab>((brandPrefab as BrandPrefab).m_Companies); // Convert the array to a list
                tempList.Add(companyPrefab as CompanyPrefab);
                (brandPrefab as BrandPrefab).m_Companies = tempList.ToArray(); // Convert the list back to an array

                // show in the log
                string text = "";
                for (int i = 0; i < (brandPrefab as BrandPrefab).m_Companies.Length; i++)
                    text += (brandPrefab as BrandPrefab).m_Companies[i].name + "|";
                if ((brandPrefab as BrandPrefab).m_Companies.Length == 0)
                    text = "None";
                Mod.Log($"{(brandPrefab as BrandPrefab).GetType().Name}.{brandPrefab.name}: {text}");

                // STEP 2 - update entity's buffer with a brand and affiliated brand
                // Based on CompanyInitializeSystem.OnUpdate
                //m_PrefabSystem.GetBuffer<CompanyBrandElement>(companyPrefab, isReadOnly: false).Add(new CompanyBrandElement(brandEntity));
                //m_PrefabSystem.GetBuffer<AffiliatedBrandElement>(companyPrefab, isReadOnly: false).Add(new AffiliatedBrandElement { m_Brand = brandEntity });
                //Mod.Log($"{companyPrefab.GetType().Name}.{companyPrefab.name}: uses {brandName}");
            }
            else
                Mod.log.Warn($"Brands: Failed to retrieve BrandPrefab {brandName} from the PrefabSystem. Brands not added.");
        }
    }

    // Step 2: Creation of Systems

    // Step 3: This is called 1 time
    /*
    [HarmonyPatch(typeof(Game.SceneFlow.GameManager), "LoadPrefabs")]
    [HarmonyPrefix]
    public static bool LoadPrefabs_Postfix()
    {
        Mod.Log("*** Game.SceneFlow.GameManager.LoadPrefabs");
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
        Mod.Log("*** PrefabInitializeSystem.OnUpdate ***");
        return true;
    }
    */

    public static void CreateNewCompanies()
    {
        m_PrefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _ = AssetCollection_AddPrefabsTo_Prefix(null); // create CompanyPrefabs

        PatchZones();

        PatchResources();

        PatchStatistics();

        GameManager.instance.localizationManager.AddSource("en-US", new StatsLocaleEN()); // create strings for statistics window
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