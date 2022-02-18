using HarmonyLib;
using System;

namespace WillsWackyManagers.Patches
{
    [HarmonyPatch(typeof(CardRarityColor))]
    class CardRarityColor_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Awake")]
        static bool MyMethodName(CardRarityColor __instance)
        {
            CardVisuals componentInParent = __instance.GetComponentInParent<CardVisuals>();
            if (componentInParent)
            {
                componentInParent.toggleSelectionAction = (Action<bool>)Delegate.Combine(componentInParent.toggleSelectionAction, new Action<bool>(__instance.Toggle));
            }
            return false;
        }

        //[HarmonyPostfix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}
    }
}
