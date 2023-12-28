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
        //updateSystem.UpdateAt<Inspector.InspectorSystem>(SystemUpdatePhase.UIUpdate);
    }
}
