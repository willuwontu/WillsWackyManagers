using BepInEx;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.Utils;
using UnboundLib.GameModes;
using UnityEngine;
using WillsWackyManagers.Utils;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;

namespace WillsWackyManagers
{
    // These are the mods required for our mod to work
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]
    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]
    public class WillsWackyManagers : BaseUnityPlugin
    {
        private const string ModId = "com.willuwontu.rounds.managers";
        private const string ModName = "Will's Wacky Managers";
        public const string Version = "1.0.0"; // What version are we on (major.minor.patch)?

        void Awake()
        {
            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
        void Start()
        {
            gameObject.GetOrAddComponent<RerollManager>();
            gameObject.GetOrAddComponent<CurseManager>();

            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, PlayerPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, PlayerPickEnd);
        }

        IEnumerator GameStart(IGameModeHandler gm)
        {

            foreach (var player in PlayerManager.instance.players)
            {
                if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseCategory))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseCategory);
                }
                if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseInteractionCategory))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                }
            }

            yield break;
        }

        IEnumerator PlayerPickStart(IGameModeHandler gm)
        {
            foreach (var player in PlayerManager.instance.players)
            {
                if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseInteractionCategory))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                }
                if (CurseManager.instance.HasCurse(player))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseInteractionCategory);
                    UnityEngine.Debug.Log($"[WWM] Player {player.playerID} is available for curse interaction effects");
                }
            }
            yield break;
        }

        IEnumerator PlayerPickEnd(IGameModeHandler gm)
        {
            if (RerollManager.instance.tableFlipped)
            {
                StartCoroutine(RerollManager.instance.FlipTable());
            }
            yield return new WaitUntil(() => RerollManager.instance.tableFlipped == false);
            if (RerollManager.instance.reroll)
            {
                StartCoroutine(RerollManager.instance.InitiateRerolls());
            }
            yield return new WaitUntil(() => RerollManager.instance.reroll == false);
            yield break;
        }
    }
}