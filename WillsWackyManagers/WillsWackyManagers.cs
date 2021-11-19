using BepInEx;
using BepInEx.Configuration;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.Utils;
using UnboundLib.GameModes;
using UnboundLib.Utils.UI;
using UnboundLib.Networking;
using UnityEngine;
using WillsWackyManagers.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using HarmonyLib;
using Photon.Pun;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;

namespace WillsWackyManagers
{
    // These are the mods required for our mod to work
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("io.olavim.rounds.rwf", BepInDependency.DependencyFlags.HardDependency)]
    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]
    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]
    public class WillsWackyManagers : BaseUnityPlugin
    {
        private const string ModId = "com.willuwontu.rounds.managers";
        private const string ModName = "Will's Wacky Managers";
        public const string Version = "1.1.2"; // What version are we on (major.minor.patch)?
        internal const string ModInitials = "WWM";

        public static WillsWackyManagers instance;

        public static ConfigEntry<bool> enableCurseRemoval;

        void Awake()
        {
            instance = this;
            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
        void Start()
        {
            instance = this;

            gameObject.GetOrAddComponent<RerollManager>();
            gameObject.GetOrAddComponent<CurseManager>();

            enableCurseRemoval = Config.Bind("Wills Wacky Managers", "Enabled", false, "Enables curse removal via other effects.");

            Unbound.RegisterMenu(ModName, () => { }, NewGUI, null, false);
            Unbound.RegisterHandshake(ModId, OnHandShakeCompleted);

            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, PlayerPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, PlayerPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, PickEnd);


            var bounces = Mathf.Pow(1, 4);
        }

        IEnumerator PickEnd(IGameModeHandler gm)
        {
            yield return new WaitForSecondsRealtime(1f);
            RerollManager.instance.tableFlipped = false;
            RerollManager.instance.rerollPlayers = new List<Player>();
            RerollManager.instance.reroll = false;

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

        private IEnumerator GameStart(IGameModeHandler gm)
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

        private static void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText($"{ModName} Options", menu, out TextMeshProUGUI _);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _);
            MenuHandler.CreateToggle(enableCurseRemoval.Value, "Enables curse removal menu.", menu, value => { enableCurseRemoval.Value = value; OnHandShakeCompleted(); });
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _);
        }

        private static void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(WillsWackyManagers), nameof(SyncSettings),
                    new[] { enableCurseRemoval.Value });
            }
        }

        [UnboundRPC]
        private static void SyncSettings(bool tableFlipEnabled)
        {
            enableCurseRemoval.Value = tableFlipEnabled;
        }

        public void InjectUIElements()
        {
            var uiGo = GameObject.Find("/Game/UI");
            var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;

            var popupGo = gameGo.transform.Find("PopUpMenu").gameObject;

            if (popupGo)
            {
                popupGo.GetOrAddComponent<Utils.UI.PopUpMenu>();
            }
        }


        /*********************
        ** Public Functions **
        *********************/
        /// <summary>
        /// Gives an extra pick to the selected players.
        /// </summary>
        /// <param name="pickers">A dictionary containing the players and the amount of cards they get to pick.</param>
        /// <returns></returns>
        public static IEnumerator ExtraPicks(Dictionary<Player, int> pickers)
        {
            yield return new WaitForSecondsRealtime(1f);

            while (pickers.Values.Max() > 0)
            {
                foreach (var player in pickers.Keys)
                {
                    if (pickers[player] <= 0)
                    {
                        yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);
                        CardChoiceVisuals.instance.Show(Enumerable.Range(0, PlayerManager.instance.players.Count).Where(i => PlayerManager.instance.players[i].playerID == player.playerID).First(), true);
                        yield return CardChoice.instance.DoPick(1, pickers[player], PickerType.Player);
                        yield return new WaitForSecondsRealtime(0.1f);
                        yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd);
                        yield return new WaitForSecondsRealtime(0.1f);
                        pickers[player] -= 1;
                    }
                }
            }

            CardChoiceVisuals.instance.Hide();

            yield break;
        }
    }
}