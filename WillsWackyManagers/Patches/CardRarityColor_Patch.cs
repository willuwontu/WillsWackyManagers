using HarmonyLib;
using System;

namespace WillsWackyManagers.Patches
{
    [HarmonyPatch(typeof(CardRarityColor))]
    class CardRarityColor_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Awake")]
        static bool StopError(CardRarityColor __instance)
        {
            CardVisuals componentInParent = __instance.GetComponentInParent<CardVisuals>();
            if (componentInParent)
            {
                componentInParent.toggleSelectionAction += __instance.Toggle;
                var companion = __instance.gameObject.AddComponent<CardRarityColorCompanion>();
                companion.rarity = __instance;
                companion.visuals = componentInParent;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Toggle")]
        static bool StopOtherError(CardRarityColor __instance)
        {
            CardInfo componentInParent = __instance.GetComponentInParent<CardInfo>();
            if (!componentInParent)
            {
                return false;
            }
            return true;
        }

        //[HarmonyPostfix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}

        class CardRarityColorCompanion : UnityEngine.MonoBehaviour
        {
            public CardVisuals visuals;
            public CardRarityColor rarity;

            public void OnDestroy()
            {
                if (visuals)
                {
                    visuals.toggleSelectionAction -= rarity.Toggle;
                }
            }
        }
    }
}
