using Game;
using Game.Buildings;
using Game.Prefabs;
using Game.UI.InGame;
using HarmonyLib;
using Unity.Entities;

namespace RealEco;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
    [HarmonyPostfix]
    public static void Initialize_Postfix(UpdateSystem updateSystem)
    {
        updateSystem.UpdateAt<RealEco.Systems.HouseholdBehaviorSystem>(SystemUpdatePhase.GameSimulation);
        //updateSystem.UpdateAt<RealEco.Systems.CitizenBehaviorSystem>(SystemUpdatePhase.GameSimulation);
    }

    // Original HouseholdBehaviorSystem
    // This patch only removes its role as a job scheduler. There are multiple utility functions that remain in use by
    // several other simulation systems.

    [HarmonyPatch(typeof(Game.Simulation.HouseholdBehaviorSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool HouseholdBehaviorSystem_OnUpdate()
    {
        return false; // don't execute the original system
    }

    /*
    [HarmonyPatch(typeof(Game.Simulation.CitizenBehaviorSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool CitizenBehaviorSystem_OnUpdate()
    {
        return false; // don't execute the original system
    }
    */

    /* Example how to add extra info to the Developer UI Info
    [HarmonyPatch(typeof(Game.UI.InGame.DeveloperInfoUISystem), "UpdateExtractorCompanyInfo")]
    [HarmonyPostfix]
    public static void UpdateExtractorCompanyInfo_Postfix(Entity entity, Entity prefab, InfoList info, EntityQuery _____query_746694603_5)
    {
        // private EntityQuery __query_746694603_5;
        //Plugin.Log("UpdateExtractorCompanyInfo");
        ExtractorParameterData singleton = _____query_746694603_5.GetSingleton<ExtractorParameterData>();
        info.Add(new InfoList.Item($"ExtPar: {singleton.m_FertilityConsumption} {singleton.m_ForestConsumption} {singleton.m_OreConsumption} {singleton.m_OilConsumption}"));
    }
    */
    
    [HarmonyPatch(typeof(Game.UI.InGame.DeveloperInfoUISystem), "UpdateZoneInfo")]
    [HarmonyPostfix]
    public static void UpdateZoneInfo_Postfix(DeveloperInfoUISystem __instance, Entity entity, Entity prefab, GenericInfo info)
    {
        //Plugin.Log("UpdateExtractorCompanyInfo");
        if (!__instance.EntityManager.HasComponent<Building>(entity))
        {
            entity = __instance.EntityManager.GetComponentData<PropertyRenter>(entity).m_Property;
            prefab = __instance.EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
        }
        BuildingPropertyData comp = __instance.EntityManager.GetComponentData<BuildingPropertyData>(prefab);
        info.value += $" space {comp.m_SpaceMultiplier} res {comp.m_ResidentialProperties}";
    }
}
