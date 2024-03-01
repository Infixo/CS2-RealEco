using System;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Economy;
using HarmonyLib;

namespace RealEco;

[HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
public static class PrefabSystem_AddPrefab_Patches
{
    public static readonly Dictionary<ResourceInEditor, float> ResourcePriceDict = new Dictionary<ResourceInEditor, float>()
    {
        {ResourceInEditor.ConvenienceFood, 35 },
        {ResourceInEditor.Food, 50 },
        //{ResourceInEditor.Meals,
        {ResourceInEditor.Paper, 70 },
        {ResourceInEditor.Furniture, 90 },
        //{ResourceInEditor.Vehicles,    -
        {ResourceInEditor.Petrochemicals,  30 },
        {ResourceInEditor.Plastics, 70 }, // 110
        {ResourceInEditor.Electronics, 90 }, // 120
        //{ResourceInEditor.Software,    - // 40
        {ResourceInEditor.Machinery, 150 }, // 100
        //{ResourceInEditor.Chemicals,   -
        //{ResourceInEditor.Pharmaceuticals, -
        {ResourceInEditor.Beverages, 45 },
        {ResourceInEditor.Textiles, 60 },
        {ResourceInEditor.Telecom, 60 }, // 80
        {ResourceInEditor.Financial, 80 }, // 100
        //{ResourceInEditor.Media, 50 }, // 60
        //{ResourceInEditor.Entertainment
        //{ResourceInEditor.Recreation
    };

    public static readonly Dictionary<ResourceInEditor, float> ResourceBaseConsumptionDict = new Dictionary<ResourceInEditor, float>()
    {
        {ResourceInEditor.ConvenienceFood, 15 },
        {ResourceInEditor.Food, 25 },
        //{ResourceInEditor.Meals,
        //{ResourceInEditor.Paper, 70 },
        //{ResourceInEditor.Furniture, 25 },
        //{ResourceInEditor.Vehicles,    -
        //{ResourceInEditor.Petrochemicals,  30 },
        //{ResourceInEditor.Plastics, 90 },
        {ResourceInEditor.Electronics, 20 },
        //{ResourceInEditor.Software,    -
        {ResourceInEditor.Chemicals, 20 },
        {ResourceInEditor.Pharmaceuticals, 15 },
        {ResourceInEditor.Beverages, 15 },
        {ResourceInEditor.Textiles, 30 },
        {ResourceInEditor.Telecom, 25 },
        {ResourceInEditor.Financial, 20 },
        {ResourceInEditor.Media, 15 },
        //{ResourceInEditor.Entertainment
        //{ResourceInEditor.Recreation
    };

    public static readonly Dictionary<ResourceInEditor, int> ResourceCarConsumptionDict = new Dictionary<ResourceInEditor, int>()
    {
        {ResourceInEditor.Vehicles, 5 }, // 20
        //{ResourceInEditor.Petrochemicals, 30 }, // 30
        {ResourceInEditor.Plastics, 5 }, // 0
        {ResourceInEditor.Electronics, 5 }, // 0
        {ResourceInEditor.Chemicals, 10 }, // 0
        {ResourceInEditor.Financial, 10 }, // 5
    };

    /* No longer in use, Resource params are in xml
    [HarmonyPrefix]
    public static bool Resources_Prefix(object __instance, PrefabBase prefab)
    {
        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "ResourcePrefab")
        {
            ResourcePrefab res = (ResourcePrefab)prefab;
            bool isModded = false;
            if (ResourcePriceDict.ContainsKey(res.m_Resource))
            {
                res.m_InitialPrice = ResourcePriceDict[res.m_Resource];
                isModded = true;
            }
            if (ResourceBaseConsumptionDict.ContainsKey(res.m_Resource))
            {
                res.m_BaseConsumption = ResourceBaseConsumptionDict[res.m_Resource];
                isModded = true;
            }
            if (ResourceCarConsumptionDict.ContainsKey(res.m_Resource))
            {
                res.m_CarConsumption = ResourceCarConsumptionDict[res.m_Resource];
                isModded = true;
            }
            if (isModded)
            Plugin.Log($"Modded {res.m_Resource}: weight {res.m_Weight} price {res.m_InitialPrice} base {res.m_BaseConsumption} car {res.m_CarConsumption} wealth {res.m_WealthModifier} " +
                $"flags {res.m_IsProduceable} {res.m_IsTradable} {res.m_IsMaterial} {res.m_IsLeisure} " +
                $"weights {res.m_ChildWeight} {res.m_TeenWeight} {res.m_AdultWeight} {res.m_ElderlyWeight} ");
        }
        return true;
    }
    */

    /* Not in use, Demand prefab not changed
    [HarmonyPrefix]
    public static bool DemandPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "DemandPrefab")
        {
            DemandPrefab p = (DemandPrefab)prefab;
            Plugin.Log($"{prefab.name}: resRatio {p.m_FreeResidentialProportion} " +
                $"happiness min {p.m_MinimumHappiness} neu {p.m_NeutralHappiness} eff {p.m_HappinessEffect} " +
                $"homeless neu {p.m_NeutralHomelessness} eff {p.m_HomelessEffect} " +
                $"unemployment neu {p.m_NeutralUnemployment} eff {p.m_UnemploymentEffect}");
            Plugin.Log($"{prefab.name}: comRatio {p.m_FreeCommercialProportion} indRatio {p.m_FreeIndustrialProportion} " +
                $"baseDemand com {p.m_CommercialBaseDemand} ind {p.m_IndustrialBaseDemand} ext {p.m_ExtractorBaseDemand}");
        }
        return true;
    }
    */

    [HarmonyPrefix]
    public static bool EconomyPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        /* These params are in xml now
        if (prefab.GetType().Name == "EconomyPrefab")
        {
            EconomyPrefab p = (EconomyPrefab)prefab;
            p.m_ExtractorCompanyExportMultiplier = 0.70f; // default: 0.85f, this change effectively increases Extractor production; 0.65f for 31%, 0.75f for 13%, 0.70 for 21%
            p.m_IndustrialProfitFactor = 0.0008f; // default 0.0001f, commercial is 0.0010f, extractor is 64/10000 i.e. 0.0064f
            Plugin.Log($"Modded {prefab.name}: ExtrExpMult {p.m_ExtractorCompanyExportMultiplier} IndProfFact {p.m_IndustrialProfitFactor}");
            // adjust wages
            p.m_Wage0 = 1550; // 1200
            p.m_Wage1 = 1900; // 2000
            p.m_Wage2 = 2980; // 2500
            p.m_Wage3 = 4020; // 3500
            p.m_Wage4 = 5010; // 5000
            p.m_Pension = 1000; // 800 - for Elders
            p.m_FamilyAllowance = 500; // 300 - for Child and Teen
            //p.m_UnemploymentBenefit = 600; // 800 - for unemployed Adults
            Plugin.Log($"Modded {prefab.name}: Wages {p.m_Wage0} {p.m_Wage1} {p.m_Wage2} {p.m_Wage3} {p.m_Wage4} Pension {p.m_Pension} Unemp {p.m_UnemploymentBenefit} Family {p.m_FamilyAllowance}");
        }
        */

        if (prefab.GetType().Name == "ServiceFeeParameterPrefab")
        {
            ServiceFeeParameterPrefab p = (ServiceFeeParameterPrefab)prefab;
            p.m_GarbageFee.m_Default = 0.4f;
            Plugin.Log($"Modded {prefab.name}: GarbageFee {p.m_GarbageFee.m_Default}");
        }

        return true;
    }

    /* Infixo: this does not have any effect which is super weird
    [HarmonyPrefix]
    public static bool ExtractorParameterPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "ExtractorParameterPrefab")
        {
            // This tweaks effectively lower the usage rate of natural resources to 50% of the original
            ExtractorParameterPrefab p = (ExtractorParameterPrefab)prefab;
            p.m_ForestConsumption = 0.5f; // 1f, Wood is used approx. 3x faster
            p.m_FertilityConsumption = 0.05f; // 0.1f, Fetile land is used approx. 4x faster
            p.m_OreConsumption = 1000000f; // 500000f, Ore is used approx. 4x faster
            p.m_OilConsumption = 200000f; // 100000f, Oil is used 4x faster
            Plugin.Log($"Modded {prefab.name}: forest {p.m_ForestConsumption} fertility {p.m_FertilityConsumption} ore {p.m_OreConsumption} oil {p.m_OilConsumption}");
        }
        return true;
    }
    */

    public static readonly Dictionary<string, float> MaxWorkersPerCellDict = new Dictionary<string, float>
    {
        // extractors
        {"Industrial_ForestryExtractor",  0.04f}, // 0.02
        {"Industrial_GrainExtractor",     0.05f}, // 0.032
        {"Industrial_OreExtractor",       0.08f}, // 0.04
        {"Industrial_OilExtractor",       0.11f}, // 0.04
        {"Industrial_CoalMine",           0.14f}, // 0.1
        {"Industrial_StoneQuarry",        0.12f}, // 0.08
        {"Industrial_VegetableExtractor", 0.06f}, // 0.032
        {"Industrial_LivestockExtractor", 0.09f}, // 0.04
        {"Industrial_CottonExtractor",    0.07f}, // 0.04
        // industry - too few people, not enough garbage, levels up very fast
        {"Industrial_ConcreteFactory", 0.4f }, // 0.25, Simple
        {"Industrial_PaperMill",       0.4f }, // 0.3, Complex
        {"Industrial_VehicleFactory",  0.4f }, // 0.3, Complex
        {"Industrial_SawMill",         0.4f }, // 0.32, Simple
        // industry - too many workers
        {"Industrial_BioRefinery",             0.6f }, // 0.65, Hitech
        {"Industrial_ElectronicsFactory",      0.6f }, // 0.65, Hitech
        {"Industrial_ChemicalFactory",         0.6f }, // 0.65, Hitech
        {"Industrial_TextileFromCottonFactory",0.6f }, // 0.65, Simple
    };
    
    public static readonly Dictionary<string, int> OutputAmountDict = new Dictionary<string, int>
    {
        // default values are 30 for all
        // Infixo: this doesn't increase production because it is countered by increased WPU so the profitability stays the same
        /*
        {"Industrial_ForestryExtractor",  30},
        {"Industrial_GrainExtractor",     60},
        {"Industrial_OreExtractor",       40},
        {"Industrial_OilExtractor",       50},
        {"Industrial_CoalMine",           30},
        {"Industrial_StoneQuarry",        50},
        {"Industrial_VegetableExtractor", 90},
        {"Industrial_LivestockExtractor", 90},
        {"Industrial_CottonExtractor",    70},
        */
        // industry
        { "Industrial_FoodFactory",      3 }, // 1 Vegetables + 1 Livestock -> 2 Food
        { "Industrial_MachineryFactory", 1 }, // 1 Steel + 1 Metal -> Machinery
        { "Industrial_VehicleFactory",   1 }, // 1 Plastics + 1 Metal -> Vehicles
        { "Industrial_BeverageFromGrainFactory",      2 }, // 1 Grain -> 1 Beverages
        { "Industrial_BeverageFromVegetablesFactory", 2 }, // 1 Vegetables -> 1 Beverages
        { "Industrial_TextileFromCottonFactory",         2 }, // 1 Cotton -> 1 Textile
        { "Industrial_TextileFromLivestockFactory",      2 }, // 1 Livestock -> 1 Textile
        { "Industrial_TextileFromPetrochemicalsFactory", 2 }, // 1 Grain -> 3 Texttiles
        { "Industrial_ConvenienceFoodFromLivestockFactory", 2 }, // 1 Livestock -> 1 Conv.food
        { "Industrial_ConvenienceFoodFromGrainFactory",     2 }, // 1 Grain -> 1 Conv.food
        // office
        { "Office_SoftwareCompany", 10 }, // 1 Electronics -> 20 Software
        { "Office_Bank",             6 }, // 1 Software -> 20 Financial
        { "Office_MediaCompany",     8 }, // 1 Software -> 20 Media
        { "Office_TelecomCompany",  10 }, // 1 Electronics + 2 Software -> 20 Telecom
    };
    
    public static readonly Dictionary<string, WorkplaceComplexity> ComplexityDict = new Dictionary<string, WorkplaceComplexity>
    {
        // Infixo stats before 9,28,11,4 => after 8,21,14,9
        // Manual => Simple
        {"Industrial_GrainExtractor",     WorkplaceComplexity.Simple },
        {"Industrial_OreExtractor",       WorkplaceComplexity.Simple },
        {"Industrial_VegetableExtractor", WorkplaceComplexity.Simple },
        {"Industrial_LivestockExtractor", WorkplaceComplexity.Simple },
        {"Industrial_CottonExtractor",    WorkplaceComplexity.Simple },
        {"Industrial_SawMill",            WorkplaceComplexity.Simple },
        // Simple => Manual
        {"Commercial_FoodStore",   WorkplaceComplexity.Manual },
        {"Commercial_Restaurant",  WorkplaceComplexity.Manual },
        //{"Commercial_GasStation",  WorkplaceComplexity.Manual },
        {"Commercial_Bar",         WorkplaceComplexity.Manual },
        {"Commercial_ConvenienceFoodStore",     WorkplaceComplexity.Manual },
        {"Commercial_FashionStore",             WorkplaceComplexity.Manual },
        //{"Industrial_TextileFromCottonFactory", WorkplaceComplexity.Manual },
        // Simple => Complex
        //{"Commercial_VehicleStore",       WorkplaceComplexity.Complex },
        {"Industrial_MetalSmelter",       WorkplaceComplexity.Complex },
        //{"Industrial_OilExtractor",       WorkplaceComplexity.Complex },
        //{"Commercial_ChemicalStore",      WorkplaceComplexity.Complex },
        {"Industrial_MachineryFactory",   WorkplaceComplexity.Complex },
        {"Industrial_BeverageFromGrainFactory",      WorkplaceComplexity.Complex },
        {"Industrial_BeverageFromVegetablesFactory", WorkplaceComplexity.Complex },
        {"Industrial_FurnitureFactory",   WorkplaceComplexity.Complex },
        // Complex => Hitech
        {"Industrial_ElectronicsFactory", WorkplaceComplexity.Hitech },
        {"Industrial_PlasticsFactory",    WorkplaceComplexity.Hitech },
        //{"Industrial_OilRefinery",        WorkplaceComplexity.Hitech },
        //{"Commercial_DrugStore",          WorkplaceComplexity.Hitech },
        {"Industrial_ChemicalFactory",    WorkplaceComplexity.Hitech },
        //{"Industrial_VehicleFactory",     WorkplaceComplexity.Hitech },
        {"Office_Bank",                   WorkplaceComplexity.Hitech },
        {"Office_MediaCompany",           WorkplaceComplexity.Hitech },
    };

    public static readonly Dictionary<string, float> ProfitabilityDict = new Dictionary<string, float>
    {
        // commercial (1/5th)
        {"Commercial_FoodStore", 80}, // 400
        {"Commercial_BookStore", 80}, // 420
        {"Commercial_VehicleStore", 50}, // 240
        {"Commercial_Restaurant", 80}, // 420
        {"Commercial_ElectronicsStore", 110}, // 600
        {"Commercial_GasStation", 50}, // 240
        {"Commercial_Hotel", 100}, // 500
        {"Commercial_Bar", 70}, // 380
        {"Commercial_ChemicalStore", 70}, // 380
        {"Commercial_ConvenienceFoodStore", 60}, // 330
        {"Commercial_DrugStore", 120}, // 600
        {"Commercial_FashionStore", 95}, // 500
        {"Commercial_FurnitureStore", 60}, // 300
        {"Commercial_LiquorStore", 75}, // 400
        {"Commercial_PlasticsStore", 70}, // 360
        {"Commercial_RecreactionStore", 70}, // 360
        // extractors
        //{"Industrial_ForestryExtractor", 20}, // 30
        //{"Industrial_GrainExtractor", 25}, // 25
        //{"Industrial_OreExtractor", 35}, // 35
        {"Industrial_OilExtractor", 60}, // 90
        {"Industrial_CoalMine", 40}, // 60
        //{"Industrial_StoneQuarry", 30}, // 30
        //{"Industrial_VegetableExtractor", 30}, // 30
        //{"Industrial_LivestockExtractor", 25}, // 25
        //{"Industrial_CottonExtractor", 30}, // 30
        // industrial
        {"Industrial_FoodFactory", 45}, // 50, price 40->50
        {"Industrial_PaperMill", 40}, // 85, price 60->70
        {"Industrial_BioRefinery", 65}, // 100, price 25->30
        {"Industrial_ElectronicsFactory", 45}, // 100
        {"Industrial_MetalSmelter", 40}, // 50
        {"Industrial_OilRefinery", 50}, // 120, price 25->30
        {"Industrial_PlasticsFactory", 45}, // 60, price 110->90
        {"Industrial_SteelPlant", 45}, //85
        {"Industrial_MachineryFactory", 30}, // 50
        {"Industrial_MineralPlant", 45}, // 40
        {"Industrial_ConcreteFactory", 40}, // 50
        {"Industrial_ChemicalFactory", 60}, // 70
        {"Industrial_PharmaceuticalsFactory", 50}, // 100
        {"Industrial_VehicleFactory", 45}, // 75
        {"Industrial_BeverageFromGrainFactory", 35}, // 40, price 34->45
        {"Industrial_BeverageFromVegetablesFactory", 30}, // 40, price 34->45
        {"Industrial_ConvenienceFoodFromLivestockFactory", 35}, // 40, price 20->35
        {"Industrial_TextileFromCottonFactory", 40}, // 50, price 34->60
        {"Industrial_TextileFromLivestockFactory", 40}, // 40, price 34->60
        {"Industrial_TextileFromPetrochemicalsFactory", 30}, // 50, price 34->60
        {"Industrial_SawMill", 35}, // 32
        {"Industrial_FurnitureFactory", 50}, // 30, price 60->90
        {"Industrial_ConvenienceFoodFromGrainFactory", 30}, // 50, price 20->35
        // office
        {"Office_SoftwareCompany", 90}, // 400
        {"Office_Bank",            90}, // 400
        {"Office_MediaCompany",    90}, // 400, price 60->50
        {"Office_TelecomCompany",  90}, // 400, price 80->60
        // warehouse - all changed from 10 to 15
        {"Industrial_WarehouseConvenienceFood", 15},
        {"Industrial_WarehouseGrain",     15},
        {"Industrial_WarehousePaper",     15},
        {"Industrial_WarehouseVehicles",  15},
        {"Industrial_WarehouseWood",      15},
        {"Industrial_WarehouseElectronics", 15},
        {"Industrial_WarehouseMetals",    15},
        {"Industrial_WarehouseOil",       15},
        {"Industrial_WarehousePlastics",  15},
        {"Industrial_WarehouseOre",       15},
        {"Industrial_WarehousePetrochemicals", 15},
        {"Industrial_WarehouseStone",     15},
        {"Industrial_WarehouseCoal",      15},
        {"Industrial_WarehouseLivestock", 15},
        {"Industrial_WarehouseCotton",    15},
        {"Industrial_WarehouseSteel",     15},
        {"Industrial_WarehouseMinerals",  15},
        {"Industrial_WarehouseConcrete",  15},
        {"Industrial_WarehouseMachinery", 15},
        {"Industrial_WarehouseChemicals", 15},
        {"Industrial_WarehousePharmaceuticals", 15},
        {"Industrial_WarehouseBeverages", 15},
        {"Industrial_WarehouseTextiles",  15},
        {"Industrial_WarehouseFood",      15},
        {"Industrial_WarehouseVegetables", 15},
        {"Industrial_WarehouseTimber",    15},
        {"Industrial_WarehouseFurniture", 15},
    };

    [HarmonyPrefix]
    public static bool Companies_Prefix(PrefabBase prefab)
    {
        if (prefab.GetType().Name == "CompanyPrefab")
        {
            /* These params are in xml now
           
            // Component ProcessingCompany => m_MaxWorkersPerCell
            if (MaxWorkersPerCellDict.ContainsKey(prefab.name) && prefab.Has<ProcessingCompany>())
            {
                ProcessingCompany comp = prefab.GetComponent<ProcessingCompany>();
                comp.process.m_MaxWorkersPerCell = MaxWorkersPerCellDict[prefab.name];
                Plugin.Log($"Modded {prefab.name}: wpc {comp.process.m_MaxWorkersPerCell}");
            }

            // Component Workplace => WorkplaceComplexity, m_Complexity
            if (ComplexityDict.ContainsKey(prefab.name) && prefab.Has<Workplace>())
            {
                Workplace comp = prefab.GetComponent<Workplace>();
                comp.m_Complexity = ComplexityDict[prefab.name];
                Plugin.Log($"Modded {prefab.name}: comp {comp.m_Complexity}");
            }
            
            CompanyPrefab cp = prefab as CompanyPrefab;
            //Plugin.Log($"{{\"{cp.name}\", {cp.profitability}}},"); // Profitability dump
            if (ProfitabilityDict.ContainsKey(prefab.name))
            {
                cp.profitability = ProfitabilityDict[prefab.name];
                Plugin.Log($"Modded {prefab.name}: prof {cp.profitability}");
            }

            // Component ProcessingCompany => m_Output
            if (OutputAmountDict.ContainsKey(prefab.name))
            {
                ProcessingCompany pc = prefab.GetComponent<ProcessingCompany>();
                IndustrialProcess pci = pc.process;
                pci.m_Output.m_Amount = OutputAmountDict[prefab.name];
                Plugin.Log($"Modded {prefab.name}.ProcessingCompany: out {pci.m_Output.m_Resource} {pci.m_Output.m_Amount} in1 {pci.m_Input1.m_Resource} {pci.m_Input1.m_Amount} in2 {pci.m_Input2.m_Resource} {pci.m_Input2.m_Amount} wpc {pci.m_MaxWorkersPerCell} tr {pc.transports}");
            }
            */

            // Special case: Industrial_BioRefinery, make it use less Grain
            if (prefab.name == "Industrial_BioRefinery")
            {
                ProcessingCompany pc = prefab.GetComponent<ProcessingCompany>();
                pc.process.m_Input1.m_Amount = 1;
                Plugin.Log($"Modded {prefab.name}.ProcessingCompany: out {pc.process.m_Output.m_Resource} {pc.process.m_Output.m_Amount} in1 {pc.process.m_Input1.m_Resource} {pc.process.m_Input1.m_Amount}");
            }
            
        }
        return true;
    }


    private static readonly Dictionary<string, float> ZoneUpkeepDict = new Dictionary<string, float>
    {
        {"EU Residential High",   500}, // 900
        {"NA Residential High",   500}, // 900
        {"EU Residential Mixed",  330}, // 450
        {"NA Residential Mixed",  330}, // 450
        {"EU Residential Medium", 200}, // 250
        {"NA Residential Medium", 200}, // 250
        {"Residential LowRent",   250}, // 900
        {"Office High",           350}, // 200
    };

    private static readonly Dictionary<string, float> ZoneSpaceMultiplierDict = new Dictionary<string, float>
    {
        //{"EU Commercial High", 5}, // 5
        //{"NA Commercial High", 5}, // 5
        {"EU Commercial Low",  1.2f}, // 1
        {"NA Commercial Low",  1.2f}, // 1
        {"Industrial Manufacturing", 1.4f}, // 1
        {"Office High",              7.0f}, // 5
        {"Office Low",               1.2f}, // 1
    };

    /* Not in use, all params in xml now
    [HarmonyPrefix]
    public static bool ZonePrefab_Prefix(PrefabBase prefab)
    {
        return true; // DISABLED
        if (prefab.GetType().Name == "ZonePrefab")
        {
            // Component ZoneServiceConsumption
            if (ZoneUpkeepDict.ContainsKey(prefab.name) && prefab.Has<ZoneServiceConsumption>())
            {
                ZoneServiceConsumption comp = prefab.GetComponent<ZoneServiceConsumption>();
                comp.m_Upkeep = ZoneUpkeepDict[prefab.name];
                Plugin.Log($"Modded {prefab.name}: upkeep {comp.m_Upkeep}");
            }
            // Component ZoneProperties
            if (ZoneSpaceMultiplierDict.ContainsKey(prefab.name) && prefab.Has<ZoneProperties>())
            {
                ZoneProperties comp = prefab.GetComponent<ZoneProperties>();
                comp.m_SpaceMultiplier = ZoneSpaceMultiplierDict[prefab.name];
                Plugin.Log($"Modded {prefab.name}: spcmult {comp.m_SpaceMultiplier:F1}");
            }
        }
        return true;
    }
    */
    
    /* Reserved for the future
    [HarmonyPrefix]
    public static bool Buildings_Prefix(object __instance, PrefabBase prefab)
    {
        return true; // DISABLED
        //if (prefab.Has<BuildingProperties>())
        //{
            //Plugin.Log($"{prefab.name}: {prefab.GetType().Name}");
        //}

        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "BuildingPrefab" && prefab.Has<BuildingProperties>())
        {
            string ShowResources(Game.Economy.ResourceInEditor[] resArr)
            {
                Game.Economy.Resource res = Game.Economy.EconomyUtils.GetResources(resArr);
                return Game.Economy.EconomyUtils.GetNames(res);
            }
            //BuildingPrefab p = (BuildingPrefab)prefab;
            BuildingProperties bp = prefab.GetComponent<BuildingProperties>();
            Plugin.Log($"{prefab.name}: spc {bp.m_SpaceMultiplier} res {bp.m_ResidentialProperties} " +
                $"com {ShowResources(bp.m_AllowedSold)} " +
                $"ind {ShowResources(bp.m_AllowedManufactured)} " +
                $"war {ShowResources(bp.m_AllowedStored)}");
            //foreach (var component in prefab.components)
                //Plugin.Log($"{prefab.name}: {component.GetType().Name}");
        }

        return true;
    }
    */
}

/*
[HarmonyPatch]
public static class ProcessingCompany_Patches
{
    [HarmonyPatch(typeof(Game.Prefabs.ProcessingCompany), "Initialize")]
    [HarmonyPrefix]
    public static bool ProcessingCompany_Initialize_Prefix(object __instance, EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<ExtractorCompanyData>(entity))
        {
            //Plugin.Log($"ProcessingCompany_Initialize_Prefix: {entity} has ExtractorCompanyData");
            if (entityManager.HasComponent<IndustrialProcessData>(entity))
            {
                IndustrialProcessData ipd = entityManager.GetComponentData<IndustrialProcessData>(entity);
                Plugin.Log($"ProcessingCompany_Initialize_Prefix: {entity} has IndustrialProcessData, wpc {ipd.m_MaxWorkersPerCell}, out {ipd.m_Output.m_Amount}");
            }
            else
                Plugin.Log($"ProcessingCompany_Initialize_Prefix: {entity} NO IndustrialProcessData");
        }
        //if (entityManager.HasComponent<ExtractorCompany>(entity))
        //{
        //Plugin.Log($"ProcessingCompany_Initialize_Prefix: {entity} has ExtractorCompany");
        //}
        return true;
    }

    [HarmonyPatch(typeof(Game.Prefabs.ProcessingCompany), "Initialize")]
    [HarmonyPostfix]
    public static void ProcessingCompany_Initialize_Postfix(object __instance, EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<ExtractorCompanyData>(entity))
        {
            //Plugin.Log($"ProcessingCompany_Initialize_Postfix: {entity} has ExtractorCompanyData");
            if (entityManager.HasComponent<IndustrialProcessData>(entity))
            {
                IndustrialProcessData ipd = entityManager.GetComponentData<IndustrialProcessData>(entity);
                Plugin.Log($"ProcessingCompany_Initialize_Postfix: {entity} has IndustrialProcessData, wpc {ipd.m_MaxWorkersPerCell}, out {ipd.m_Output.m_Amount}");
            }
            else
                Plugin.Log($"ProcessingCompany_Initialize_Postfix: {entity} NO IndustrialProcessData");
        }
    }

}
*/

/*
[HarmonyPatch]
public static class PrefabInitializeSystem_Patches
{
    [HarmonyPatch(typeof(Game.Prefabs.PrefabInitializeSystem), "InitializePrefab")]
    [HarmonyPrefix]
    public static bool InitializePrefab_Prefix(object __instance, Entity entity, PrefabBase prefab, List<ComponentBase> components)
    {
        if (prefab.GetType().Name == "CompanyPrefab")
        {
            Plugin.Log($"InitializePrefab_Prefix: {prefab.name} {entity}");
        }
        return true;
    }

    [HarmonyPatch(typeof(Game.Prefabs.PrefabInitializeSystem), "LateInitializePrefab")]
    [HarmonyPrefix]
    public static bool LateInitializePrefab_Prefix(object __instance, Entity entity, PrefabBase prefab)
    {
        if (prefab.GetType().Name == "CompanyPrefab")
        {
            Plugin.Log($"LateInitializePrefab_Prefix: {prefab.name} {entity}");
        }
        return true;
    }

}
*/