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
using WillsWackyManagers.Cards;
using WillsWackyManagers.Networking;
using UnityEngine.UI;
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
        public const string Version = "1.2.6"; // What version are we on (major.minor.patch)?
        internal const string ModInitials = "WWM";

        public static WillsWackyManagers instance;

        public static bool enableCurseRemoval = false;
        public static bool enableCurseSpawning = true;
        public static ConfigEntry<bool> enableCurseRemovalConfig;
        public static ConfigEntry<bool> enableCurseSpawningConfig;

        public static bool enableTableFlip = true;
        public static bool secondHalfTableFlip = true;
        public static ConfigEntry<bool> enableTableFlipConfig;
        public static ConfigEntry<bool> secondHalfTableFlipConfig;

        // A way for me to hook onto the menu and add more options in WWC, if needed.
        public GameObject optionsMenu;

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

            gameObject.AddComponent<SettingCoordinator>();

            gameObject.GetOrAddComponent<RerollManager>();
            gameObject.GetOrAddComponent<CurseManager>();

            { // Config File Stuff
                // Curse Manager Settings
                enableCurseSpawningConfig = Config.Bind(ModInitials, "CurseSpawning", true, "Cards that give curses can spawn.");
                enableCurseRemovalConfig = Config.Bind(ModInitials, "CurseRemoval", false, "Enables curse removal via end of round effects.");
                enableCurseRemoval = enableCurseRemovalConfig.Value;
                enableCurseSpawning = enableCurseSpawningConfig.Value;

                // Reroll Manager Settings
                enableTableFlipConfig = Config.Bind(ModInitials, "TableFlipAllowed", true, "Enable table flip and reroll.");
                secondHalfTableFlipConfig = Config.Bind(ModInitials, "TableFlipSecondHalf", true, "Makes Table Flip an Uncommon and only able to appear in the second half.");
                enableTableFlip = enableTableFlipConfig.Value;
                secondHalfTableFlip = secondHalfTableFlipConfig.Value;
            }

            Unbound.RegisterMenu("Will's Wacky Options", () => { }, NewGUI, null, false);
            Unbound.RegisterHandshake(ModId, OnHandShakeCompleted);

            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, PlayerPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, PlayerPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, PickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, PickStart);


            CustomCard.BuildCard<TableFlip>((cardInfo) => { RerollManager.instance.tableFlipCard = cardInfo; });
            CustomCard.BuildCard<Reroll>((cardInfo) => { RerollManager.instance.rerollCard = cardInfo; });
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

            var mixers = RerollManager.instance.MixUpPlayers.ToArray();

            foreach (var player in mixers)
            {
                yield return RerollManager.instance.IMixUpCards(player);
            }

            RerollManager.instance.MixUpPlayers.Clear();

            yield break;
        }

        private IEnumerator GameStart(IGameModeHandler gm)
        {
            foreach (var player in PlayerManager.instance.players)
            {
                // Curses are always disabled as an option for players to choose
                if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseCategory))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseCategory);
                }
                if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseInteractionCategory))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                }

                // If Reroll cards are disabled, we blacklist them as an option to be taken.
                if (!enableTableFlip)
                {
                    if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(RerollManager.instance.NoFlip))
                    {
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(RerollManager.instance.NoFlip);
                    }
                }
                else
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll((category) => category == RerollManager.instance.NoFlip);
                }

                // If curse spawning cards are disabled, we blacklist them as an option for players.
                if (!enableCurseSpawning)
                {
                    if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseSpawnerCategory))
                    {
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseSpawnerCategory);
                    }
                }
                else
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll((category) => category == CurseManager.instance.curseSpawnerCategory);
                }
            }

            yield break;
        }

        private IEnumerator PickStart(IGameModeHandler gm)
        {
            if (secondHalfTableFlip)
            {
                RerollManager.instance.tableFlipCard.rarity = CardInfo.Rarity.Uncommon;

                var roundsToWin = (int)gm.Settings["roundsToWinGame"];
                var pickable = false;

                foreach (var player in PlayerManager.instance.players)
                {
                    if (gm.GetTeamScore(player.teamID).rounds > ((roundsToWin / 2) + 1 * roundsToWin % 2))
                    {
                        pickable = true;
                    }
                }

                foreach (var player in PlayerManager.instance.players)
                {
                    if (!pickable)
                    {
                        if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(TableFlip.tableFlipCardCategory))
                        {
                            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(TableFlip.tableFlipCardCategory);
                        }
                    }
                    else
                    {
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll((category) => category == TableFlip.tableFlipCardCategory);
                    }
                }
            }

            yield break;
        }

        private static void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText($"Will's Wacky Options", menu, out TextMeshProUGUI _);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText("Curse Manager", menu, out TextMeshProUGUI _, 45);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            var curseSpawn = MenuHandler.CreateToggle(enableCurseSpawningConfig.Value, "Enables curse spawning cards.", menu, null);
            var curseRemove = MenuHandler.CreateToggle(enableCurseRemovalConfig.Value, "Enables curse removal between rounds.", menu, value => { enableCurseRemovalConfig.Value = value; enableCurseRemoval = value; });
            curseSpawn.GetComponent<Toggle>().onValueChanged.AddListener((value) => {
                curseRemove.SetActive(value);
                if (!value)
                {
                    curseRemove.GetComponent<Toggle>().isOn = false;
                }
                enableCurseSpawningConfig.Value = value;
                enableCurseSpawning = value;
            });
            //curseRemove.SetActive(false);
            //enableCurseRemoval.Value = false;
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText("Reroll Manager", menu, out TextMeshProUGUI _, 45);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            var enable = MenuHandler.CreateToggle(enableTableFlipConfig.Value, "Enable Table Flip and Reroll", menu, null);
            var secondHalf = MenuHandler.CreateToggle(secondHalfTableFlipConfig.Value, "Table Flip becomes uncommon, and can only show up when someone has half the rounds needed to win.", menu, value => { secondHalfTableFlipConfig.Value = value; secondHalfTableFlip = value; });
            var secondHalfToggle = secondHalf.GetComponent<Toggle>();

            enable.GetComponent<Toggle>().onValueChanged.AddListener(value =>
            {
                secondHalf.SetActive(value);
                if (!value)
                {
                    secondHalfToggle.isOn = false;
                }
                enableTableFlipConfig.Value = value;
                enableTableFlip = value;
            });

            instance.optionsMenu = menu;

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _);
        }

        internal void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                for (int i = 0; i < 1; i++)
                {
                    NetworkingManager.RPC(typeof(WillsWackyManagers), nameof(RPCA_SyncSettings), new object[] { enableCurseSpawningConfig.Value, enableCurseRemovalConfig.Value, enableTableFlipConfig.Value, secondHalfTableFlipConfig.Value });
                    RPCA_SyncSettings(enableCurseSpawningConfig.Value, enableCurseRemovalConfig.Value, enableTableFlipConfig.Value, secondHalfTableFlipConfig.Value);
                }
            }
        }

        [UnboundRPC]
        private static void RPCA_SyncSettings(bool curseSpawningEnabled, bool curseRemovalEnabled, bool tableFlipEnabled, bool tableFlipSecondHalf)
        {
            enableCurseSpawningConfig.Value = curseSpawningEnabled;
            enableCurseRemovalConfig.Value = curseRemovalEnabled;
            enableTableFlipConfig.Value = tableFlipEnabled;
            secondHalfTableFlipConfig.Value = tableFlipSecondHalf;

            enableCurseSpawning = curseSpawningEnabled;
            enableCurseRemoval = curseRemovalEnabled;
            enableTableFlip = tableFlipEnabled;
            secondHalfTableFlip = tableFlipSecondHalf;

            UnityEngine.Debug.Log($"[WWM][Settings][Sync]\nEnable Curse Spawning: {curseSpawningEnabled}\nEnable Curse Removal: {curseRemovalEnabled}\nEnable Table Flip: {tableFlipEnabled}\nTable Flip Second Half Only: {tableFlipSecondHalf}");

            ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            customProperties[SettingCoordinator.PropertyName] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);
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

            for (int _ = 0; _ < pickers.Values.ToArray().Max(); _++)
            {
                for (int i = 0; i < pickers.Keys.Count; i++)
                {
                    var player = pickers.Keys.ToArray()[i];
                    if (pickers[player] > 0)
                    {
                        yield return GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart);
                        CardChoiceVisuals.instance.Show(Enumerable.Range(0, PlayerManager.instance.players.Count).Where(i => PlayerManager.instance.players[i].playerID == player.playerID).First(), true);
                        yield return CardChoice.instance.DoPick(1, player.playerID, PickerType.Player);
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