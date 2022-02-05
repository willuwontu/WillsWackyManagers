using HarmonyLib;

namespace WillsWackyManagers.Patches
{
    [HarmonyPatch(typeof(MainMenuHandler), "Awake")]
    class MainMenuHandler_Patch_Awake
    {
        static void Postfix()
        {
            WillsWackyManagers.instance.InjectUIElements();
        }
    }
}
