using HarmonyLib;
using System;
using WillsWackyManagers.Utils;

namespace WillsWackyManagers.Patches
{
    [HarmonyPatch(typeof(Player))]
    class Player_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("FullReset")]
        static void ResetCurseDraw(Player __instance)
        {
            CurseManager.instance.PlayerCanDrawCurses(__instance, false);
        }

        //[HarmonyPostfix]
        //[HarmonyPatch("FullReset")]
        //static void ResetCardsDrawAdjustment(Player __instance)
        //{
        //    if (AlteringTheDeal.cardsTaken.ContainsKey(__instance))
        //    {
        //        AlteringTheDeal.cardsTaken.Remove(__instance);
        //    }
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}

        //[HarmonyPostfix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}
    }
}
