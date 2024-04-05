using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Colossal.Collections;
using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.City;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;

namespace RealEco;

[HarmonyPatch]
class Patches
{
    //[HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
    //[HarmonyPostfix]
    public static void Initialize_Postfix(UpdateSystem updateSystem)
    {
        if (Mod.setting.FeatureNewCompanies)
            updateSystem.UpdateAt<RealEco.Systems.ResourceBuyerSystem>(SystemUpdatePhase.GameSimulation);
        if (Mod.setting.FeatureConsumptionFix)
            updateSystem.UpdateAt<RealEco.Systems.HouseholdBehaviorSystem>(SystemUpdatePhase.GameSimulation);
        //updateSystem.UpdateAt<RealEco.Systems.CitizenBehaviorSystem>(SystemUpdatePhase.GameSimulation); // debug only
        if (Mod.setting.FeatureCommercialDemand)
            updateSystem.UpdateAt<RealEco.Systems.CommercialUISystem>(SystemUpdatePhase.UIUpdate);
    }

    // Original HouseholdBehaviorSystem
    // This patch only removes its role as a job scheduler. There are multiple utility functions that remain in use by
    // several other simulation systems.

/* 240331 not used
[HarmonyPatch(typeof(Game.Simulation.HouseholdBehaviorSystem), "OnUpdate")]
[HarmonyPrefix]
static bool HouseholdBehaviorSystem_OnUpdate()
{
    // Skip the patch and execute the original if the feaure is disabled
    if (!Mod.setting.FeatureConsumptionFix)
        return true;

    return false; // don't execute the original system
}
*/

/* 
// debug only
[HarmonyPatch(typeof(Game.Simulation.CitizenBehaviorSystem), "OnUpdate")]
[HarmonyPrefix]
static bool CitizenBehaviorSystem_OnUpdate()
{
    return false; // don't execute the original system
}
*/

/* 240331 not used
[HarmonyPatch(typeof(Game.Simulation.ResourceBuyerSystem), "OnUpdate")]
[HarmonyPrefix]
static bool ResourceBuyerSystem_OnUpdate()
{
    // Skip the patch and execute the original if the feaure is disabled
    if (!Mod.setting.FeatureNewCompanies)
        return true;

    return false; // don't execute the original system
}
*/
#if DEBUG
    [HarmonyPatch(typeof(Game.Prefabs.ComponentBase), "Initialize")]
    [HarmonyPostfix]
    static void ComponentBase_Initialize(ComponentBase __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.ComponentBase.Initialize: ({entity.Index}) {__instance.prefab.name}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.ComponentBase), "LateInitialize")]
    [HarmonyPostfix]
    static void ComponentBase_LateInitialize(ComponentBase __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.ComponentBase.LateInitialize: ({entity.Index}) {__instance.prefab.name}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.ArchetypePrefab), "RefreshArchetype")]
    [HarmonyPostfix]
    static void ArchetypePrefab_RefreshArchetype(ArchetypePrefab __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.ArchetypePrefab.RefreshArchetype: ({entity.Index}) {__instance.name}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.EconomyPrefab), "LateInitialize")]
    [HarmonyPostfix]
    static void EconomyPrefab_LateInitialize(EconomyPrefab __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.EconomyPrefab.LateInitialize: ({entity.Index}) {__instance.m_CommercialDiscount} {__instance.m_ExtractorCompanyExportMultiplier} {__instance.m_IndustrialProfitFactor}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.DemandPrefab), "LateInitialize")]
    [HarmonyPostfix]
    static void DemandPrefab_LateInitialize(DemandPrefab __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.DemandPrefab.LateInitialize: ({entity.Index}) {__instance.m_CommercialBaseDemand} {__instance.m_FreeCommercialProportion} {__instance.m_HomelessEffect}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.ZoneServiceConsumption), "Initialize")]
    [HarmonyPostfix]
    static void ZoneServiceConsumption_Initialize(ZoneServiceConsumption __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.ZoneServiceConsumption.Initialize: ({entity.Index}) {__instance.m_Upkeep}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.ZoneProperties), "Initialize")]
    [HarmonyPostfix]
    static void ZoneProperties_Initialize(ZoneProperties __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.ZoneProperties.Initialize: ({entity.Index}) {__instance.m_SpaceMultiplier} {EconomyUtils.GetNames(EconomyUtils.GetResources(__instance.m_AllowedSold))}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.ProcessingCompany), "Initialize")]
    [HarmonyPostfix]
    static void ProcessingCompany_Initialize(Game.Prefabs.ProcessingCompany __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.ProcessingCompany.Initialize: ({entity.Index}) {__instance.transports} {__instance.process.m_MaxWorkersPerCell} {__instance.process.m_Output.m_Resource}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.ServiceCompany), "Initialize")]
    [HarmonyPostfix]
    static void ServiceCompany_Initialize(Game.Prefabs.ServiceCompany __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.ServiceCompany.Initialize: ({entity.Index}) {__instance.m_MaxService} {__instance.m_MaxWorkersPerCell}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.Workplace), "Initialize")]
    [HarmonyPostfix]
    static void Workplace_Initialize(Game.Prefabs.Workplace __instance, EntityManager entityManager, Entity entity)
    {
        Mod.Log($"{__instance.name}.Workplace.Initialize: ({entity.Index}) {__instance.m_Workplaces} {__instance.m_Complexity}");
    }

    [HarmonyPatch(typeof(Game.Prefabs.CompanyPrefabInitializeSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool CompanyPrefabInitializeSystem_OnUpdate(Game.Prefabs.CompanyPrefabInitializeSystem __instance, EntityQuery ___m_PrefabQuery)
    {
        Mod.Log($"CompanyPrefabInitializeSystem.OnUpdate: {___m_PrefabQuery.CalculateEntityCount()}");
        return true;
    }

    [HarmonyPatch(typeof(Game.Prefabs.CompanyInitializeSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool CompanyInitializeSystem_OnUpdate(Game.Prefabs.CompanyInitializeSystem __instance, EntityQuery ___m_PrefabQuery, EntityQuery ___m_CompanyQuery)
    {
        Mod.Log($"CompanyInitializeSystem.OnUpdate: {___m_PrefabQuery.CalculateEntityCount()} {___m_CompanyQuery.CalculateEntityCount()}");
        return true;
    }

#endif
}
