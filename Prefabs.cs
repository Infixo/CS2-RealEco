using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Entities;
using HarmonyLib;

namespace RealEco;

[HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
public static class PrefabSystem_AddPrefab_Patches
{
    [HarmonyPrefix]
    public static bool Resources_Prefix(object __instance, PrefabBase prefab)
    {
        return true; // DISABLED
        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "ResourcePrefab")
        {
            ResourcePrefab res = (ResourcePrefab)prefab;
            Plugin.Log($"{prefab.name}: {res.m_Resource} price {res.m_InitialPrice} {res.m_Weight} " +
                $"flags {res.m_IsProduceable} {res.m_IsTradable} {res.m_IsMaterial} {res.m_IsLeisure} " +
                $"base {res.m_BaseConsumption} car {res.m_CarConsumption} wealth {res.m_WealthModifier} " +
                $"weights {res.m_ChildWeight} {res.m_TeenWeight} {res.m_AdultWeight} {res.m_ElderlyWeight} ");
        }
        return true;
    }
    
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

    [HarmonyPrefix]
    public static bool EconomyPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "EconomyPrefab")
        {
            EconomyPrefab p = (EconomyPrefab)prefab;
            p.m_ExtractorCompanyExportMultiplier = 0.70f; // default: 0.85f, this change effectively increases Extractor production by 21%
            Plugin.Log($"{prefab.name}: " +
                $"discount {p.m_CommercialDiscount} extMult {p.m_ExtractorCompanyExportMultiplier} " +
                $"wages {p.m_Wage0} {p.m_Wage1} {p.m_Wage2} {p.m_Wage3} {p.m_Wage4}");
            Plugin.Log($"{prefab.name}: " +
                $"effI {p.m_IndustrialEfficiency} effC {p.m_CommercialEfficiency} effE {p.m_ExtractorEfficiency} " +
                $"resCon {p.m_ResourceConsumption} tour {p.m_TouristConsumptionMultiplier} traff {p.m_TrafficReduction}");
        }
        return true;
    }

    private static readonly Dictionary<string, float> MaxWorkersPerCellDict = new Dictionary<string, float>
    {
        {"Industrial_ForestryExtractor",  0.05f}, // 0.02
        {"Industrial_GrainExtractor",     0.06f}, // 0.032
        {"Industrial_OreExtractor",       0.10f}, // 0.04
        {"Industrial_OilExtractor",       0.12f}, // 0.04
        {"Industrial_CoalMine",           0.15f}, // 0.1
        {"Industrial_StoneQuarry",        0.12f}, // 0.08
        {"Industrial_VegetableExtractor", 0.08f}, // 0.032
        {"Industrial_LivestockExtractor", 0.10f}, // 0.04
        {"Industrial_CottonExtractor",    0.10f}, // 0.04
    };
    
    // NOT USED
    private static readonly Dictionary<string, int> OutputAmountDict = new Dictionary<string, int>
    {
        // default values are 30 for all
        // Infixo: this doesn't increase production because it is countered by increased WPU so the profitability stays the same
        {"Industrial_ForestryExtractor",  30},
        {"Industrial_GrainExtractor",     60},
        {"Industrial_OreExtractor",       40},
        {"Industrial_OilExtractor",       50},
        {"Industrial_CoalMine",           30},
        {"Industrial_StoneQuarry",        50},
        {"Industrial_VegetableExtractor", 90},
        {"Industrial_LivestockExtractor", 90},
        {"Industrial_CottonExtractor",    70},
    };
    
    private static readonly Dictionary<string, WorkplaceComplexity> ComplexityDict = new Dictionary<string, WorkplaceComplexity>
    {
        // Infixo stats before 9,28,11,4 => after 11,19,10,12
        // Manual => Simple
        {"Industrial_GrainExtractor",     WorkplaceComplexity.Simple },
        {"Industrial_OreExtractor",       WorkplaceComplexity.Simple },
        {"Industrial_VegetableExtractor", WorkplaceComplexity.Simple },
        {"Industrial_LivestockExtractor", WorkplaceComplexity.Simple },
        {"Industrial_CottonExtractor",    WorkplaceComplexity.Simple },
        // Simple => Manual
        {"Commercial_FoodStore",   WorkplaceComplexity.Manual },
        {"Commercial_Restaurant",  WorkplaceComplexity.Manual },
        {"Commercial_GasStation",  WorkplaceComplexity.Manual },
        {"Commercial_Bar",         WorkplaceComplexity.Manual },
        {"Commercial_ConvenienceFoodStore",     WorkplaceComplexity.Manual },
        {"Commercial_FashionStore",             WorkplaceComplexity.Manual },
        {"Industrial_TextileFromCottonFactory", WorkplaceComplexity.Manual },
        // Simple => Complex
        {"Commercial_VehicleStore",       WorkplaceComplexity.Complex },
        {"Industrial_OilExtractor",       WorkplaceComplexity.Complex },
        {"Commercial_ChemicalStore",      WorkplaceComplexity.Complex },
        {"Industrial_MachineryFactory",   WorkplaceComplexity.Complex },
        {"Industrial_BeverageFromGrainFactory",      WorkplaceComplexity.Complex },
        {"Industrial_BeverageFromVegetablesFactory", WorkplaceComplexity.Complex },
        {"Industrial_FurnitureFactory",   WorkplaceComplexity.Complex },
        // Complex => Hitech
        {"Industrial_ElectronicsFactory", WorkplaceComplexity.Hitech },
        {"Industrial_PlasticsFactory",    WorkplaceComplexity.Hitech },
        {"Industrial_OilRefinery",        WorkplaceComplexity.Hitech },
        {"Commercial_DrugStore",          WorkplaceComplexity.Hitech },
        {"Industrial_ChemicalFactory",    WorkplaceComplexity.Hitech },
        {"Industrial_VehicleFactory",     WorkplaceComplexity.Hitech },
        {"Office_Bank",                   WorkplaceComplexity.Hitech },
        {"Office_MediaCompany",           WorkplaceComplexity.Hitech },
    };
    
    [HarmonyPrefix]
    public static bool Companies_Prefix(PrefabBase prefab)
    {
        if (prefab.GetType().Name == "CompanyPrefab")
        {
            // Component ProcessingCompany => m_MaxWorkersPerCell
            if (prefab.Has<ExtractorCompany>())
            {
                ProcessingCompany comp = prefab.GetComponent<ProcessingCompany>();
                if (MaxWorkersPerCellDict.ContainsKey(prefab.name))
                {
                    comp.process.m_MaxWorkersPerCell = MaxWorkersPerCellDict[prefab.name];
                    Plugin.Log($"Modded {prefab.name}: wpc {comp.process.m_MaxWorkersPerCell}");
                }
            }

            // Component Workplace => WorkplaceComplexity, m_Complexity
            if (prefab.Has<Workplace>())
            {
                Workplace comp = prefab.GetComponent<Workplace>();
                if (ComplexityDict.ContainsKey(prefab.name))
                {
                    comp.m_Complexity = ComplexityDict[prefab.name];
                    Plugin.Log($"Modded {prefab.name}: comp {comp.m_Complexity}");
                }
            }
        }
        return true;
    }

    /*
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