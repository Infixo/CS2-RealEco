using Game;
using HarmonyLib;

namespace RealEco;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
    [HarmonyPostfix]
    public static void Initialize_Postfix(UpdateSystem updateSystem)
    {
        updateSystem.UpdateAt<RealEco.Systems.HouseholdBehaviorSystem>(SystemUpdatePhase.GameSimulation);
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
}
