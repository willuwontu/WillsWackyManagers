using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.UI;
using WillsWackyManagers.Cards;
using WillsWackyManagers.Cards.Curses;
using WillsWackyManagers.MonoBehaviours;
using WillsWackyManagers.Networking;
using WillsWackyManagers.UnityTools;
using WillsWackyManagers.Utils;

namespace WillsWackyManagers
{
    // These are the mods required for our mod to work
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("root.rarity.lib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("root.cardtheme.lib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.CrazyCoders.Rounds.RarityBundle", BepInDependency.DependencyFlags.HardDependency)]
    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]
    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]
    public class WillsWackyManagers : BaseUnityPlugin
    {
        public const string ModId = "com.willuwontu.rounds.managers";
        private const string ModName = "Will's Wacky Managers";
        public const string Version = "1.5.0"; // What version are we on (major.minor.patch)?
        internal const string ModInitials = "WWM";
        public const string CurseInitials = "Curse";

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

        public AssetBundle WWMAssets { get; private set; }

        private const bool debug = false;

        void Awake()
        {
            instance = this;
            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            typeof(Unbound).GetField("templateCard", BindingFlags.Default | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.GetField).SetValue(null, Resources.Load<GameObject>("0 Cards/0. PlainCard").GetComponent<CardInfo>());

            List<ThemeInfo> themes = new List<ThemeInfo>();
            themes.Add(new ThemeInfo("CurseGray", new CardThemeColor() { bgColor = new Color(0.34f, 0f, 0.44f), targetColor = new Color(0.24f, 0.24f, 0.24f) }));
            themes.Shuffle();
            themes.Shuffle();

            for (int i = 0; i < themes.Count; i++)
            {
                RegisterTheme(themes[i]);
            }

            gameObject.GetOrAddComponent<RerollManager>();
            gameObject.GetOrAddComponent<CurseManager>();
            gameObject.AddComponent<SettingCoordinator>();

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

            WWMAssets = AssetUtils.LoadAssetBundleFromResources("wwccards", typeof(WillsWackyManagers).Assembly);

            GameObject cardLoader = WWMAssets.LoadAsset<GameObject>("WWM CardManager");
            foreach (CardBuilder cardBuilder in cardLoader.GetComponentsInChildren<CardBuilder>())
            {
                cardBuilder.BuildCards();
            }
        }

        void Start()
        {

            Unbound.RegisterMenu("Will's Wacky Options", () => { }, NewGUI, null, false);


            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, PlayerPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, PlayerPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, PickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, PickStart);
            GameModeManager.AddHook(GameModeHooks.HookGameEnd, GameEnd);


            //CustomCard.BuildCard<TableFlip>((cardInfo) => { RerollManager.instance.tableFlipCard = cardInfo; });
            //CustomCard.BuildCard<Reroll>((cardInfo) => { RerollManager.instance.rerollCard = cardInfo; });

            //{ // Curses
            //    CustomCard.BuildCard<PastaShells>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<CrookedLegs>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<DrivenToEarth>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<Misfire>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<UncomfortableDefense>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<CounterfeitAmmo>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<NeedleBullets>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<WildShots>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<RabbitsFoot>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<LuckyClover>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<DefectiveTrigger>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<MisalignedSights>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<Damnation>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<FragileBody>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<AmmoRegulations>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<AirResistance>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<LeadBullets>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<AnimePhysics>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<TakeANumber>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //    CustomCard.BuildCard<HeavyShields>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
            //}
        }

        public void DebugLog(object message)
        {
            if (debug)
            {
                UnityEngine.Debug.Log(message);
            }
        }

        IEnumerator PlayerPickEnd(IGameModeHandler gm)
        {
            yield return new WaitForSecondsRealtime(1f);

            yield return GroupWinnings.ExtraPicks();

            yield break;
        }

        IEnumerator PlayerPickStart(IGameModeHandler gm)
        {
            yield break;
        }

        IEnumerator PickEnd(IGameModeHandler gm)
        {
            if (RerollManager.instance.tableFlipped)
            {
                StartCoroutine(RerollManager.instance.IFlipTableNew());
            }
            yield return new WaitUntil(() => RerollManager.instance.tableFlipped == false);

            if (!PhotonNetwork.OfflineMode)
            {
                ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
                customProperties[SettingCoordinator.TableFlipSyncProperty] = false;
                PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);
            }

            if (RerollManager.instance.reroll)
            {
                StartCoroutine(RerollManager.instance.InitiateRerolls());
            }
            yield return new WaitUntil(() => RerollManager.instance.reroll == false);

            if (!PhotonNetwork.OfflineMode)
            {
                ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
                customProperties[SettingCoordinator.RerollSyncProperty] = false;
                PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);
            }

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
                CurseManager.instance.PlayerCanDrawCurses(player,false);
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
            }

            yield break;
        }

        IEnumerator GameEnd(IGameModeHandler gm)
        {
            DestroyAll<Misfire_Mono>();
            yield break;
        }

        void DestroyAll<T>() where T : UnityEngine.Object
        {
            var objects = GameObject.FindObjectsOfType<T>();
            for (int i = objects.Length - 1; i >= 0; i--)
            {
                UnityEngine.Debug.Log($"Attempting to Destroy {objects[i].GetType().Name} number {i}");
                UnityEngine.Object.Destroy(objects[i]);
            }
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
                    if (gm.GetTeamScore(player.teamID).rounds >= ((roundsToWin / 2) + 1 * roundsToWin % 2))
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

            yield return WaitFor.Frames(10);

            ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            customProperties[SettingCoordinator.TableFlipSyncProperty] = false;
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);

            yield return WaitFor.Frames(10);

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
            var secondHalf = MenuHandler.CreateToggle(secondHalfTableFlipConfig.Value, "Table Flip becomes uncommon, and can only show up when someone has half the rounds needed to win.", menu, value => {
                secondHalfTableFlipConfig.Value = value; secondHalfTableFlip = value; if (secondHalfTableFlip)
                {
                    RerollManager.instance.tableFlipCard.rarity = CardInfo.Rarity.Uncommon;
                }
                else
                {
                    RerollManager.instance.tableFlipCard.rarity = CardInfo.Rarity.Rare;
                }
            });
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

            if (secondHalfTableFlip)
            {
                RerollManager.instance.tableFlipCard.rarity = CardInfo.Rarity.Uncommon;
            }
            else
            {
                RerollManager.instance.tableFlipCard.rarity = CardInfo.Rarity.Rare;
            }

            WillsWackyManagers.instance.DebugLog($"[WWM][Settings][Sync]\nEnable Curse Spawning: {curseSpawningEnabled}\nEnable Curse Removal: {curseRemovalEnabled}\nEnable Table Flip: {tableFlipEnabled}\nTable Flip Second Half Only: {tableFlipSecondHalf}");

            ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            customProperties[SettingCoordinator.SettingsPropertyName] = true;
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
                        CardChoiceVisuals.instance.Show(Enumerable.Range(0, PlayerManager.instance.players.Count).Where(i2 => PlayerManager.instance.players[i2].playerID == player.playerID).First(), true);
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

        public void InjectUIElements()
        {
            var uiGo = GameObject.Find("/Game/UI");
            var gameGo = uiGo.transform.Find("UI_Game").Find("Canvas").gameObject;

            if (!gameGo.transform.Find("PopUpMenuWWM"))
            {
                var popupGo = new GameObject("PopUpMenuWWM");
                popupGo.transform.SetParent(gameGo.transform);
                popupGo.transform.localScale = Vector3.one;
                popupGo.AddComponent<UI.PopUpMenu>();
            }
        }

        private static class WaitFor
        {
            public static IEnumerator Frames(int frameCount)
            {
                if (frameCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("frameCount", "Cannot wait for less that 1 frame");
                }

                while (frameCount > 0)
                {
                    frameCount--;
                    yield return null;
                }
            }
        }

        class RarityInfo
        {
            public string name;
            public float relativeRarity;
            public Color color;
            public Color colorOff;

            public RarityInfo(string name, float relativeRarity, Color color, Color colorOff)
            {
                this.name = name;
                this.relativeRarity = relativeRarity;
                this.color = color;
                this.colorOff = colorOff;
            }
        }

        static int RegisterRarity(RarityInfo info)
        {
            try
            {
                return RarityLib.Utils.RarityUtils.AddRarity(info.name, info.relativeRarity, info.color, info.colorOff);
            }
            catch
            {
                return 0;
            }
        }

        class ThemeInfo
        {
            public string name;
            public CardThemeColor cardThemeColor;

            public ThemeInfo(string name, CardThemeColor cardThemeColor)
            {
                this.name = name;
                this.cardThemeColor = cardThemeColor;
            }
        }

        static void RegisterTheme(ThemeInfo info)
        {
            try
            {
                CardThemeLib.CardThemeLib.instance.CreateOrGetType(info.name, info.cardThemeColor);
            }
            catch
            {

            }
        }
    }
}