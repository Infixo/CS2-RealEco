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
    [HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
    [HarmonyPostfix]
    public static void Initialize_Postfix(UpdateSystem updateSystem)
    {
        if (Plugin.FeatureNewCompanies.Value)
            updateSystem.UpdateAt<RealEco.Systems.ResourceBuyerSystem>(SystemUpdatePhase.GameSimulation);
        if (Plugin.FeatureConsumptionFix.Value)
            updateSystem.UpdateAt<RealEco.Systems.HouseholdBehaviorSystem>(SystemUpdatePhase.GameSimulation);
        //updateSystem.UpdateAt<RealEco.Systems.CitizenBehaviorSystem>(SystemUpdatePhase.GameSimulation); // debug only
    }

    // Original HouseholdBehaviorSystem
    // This patch only removes its role as a job scheduler. There are multiple utility functions that remain in use by
    // several other simulation systems.

    [HarmonyPatch(typeof(Game.Simulation.HouseholdBehaviorSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool HouseholdBehaviorSystem_OnUpdate()
    {
        // Skip the patch and execute the original if the feaure is disabled
        if (!Plugin.FeatureConsumptionFix.Value)
            return true;

        return false; // don't execute the original system
    }

    /* 
    // debug only
    [HarmonyPatch(typeof(Game.Simulation.CitizenBehaviorSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool CitizenBehaviorSystem_OnUpdate()
    {
        return false; // don't execute the original system
    }
    */

    [HarmonyPatch(typeof(Game.Simulation.ResourceBuyerSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool ResourceBuyerSystem_OnUpdate()
    {
        // Skip the patch and execute the original if the feaure is disabled
        if (!Plugin.FeatureNewCompanies.Value)
            return true;

        return false; // don't execute the original system
    }
}
