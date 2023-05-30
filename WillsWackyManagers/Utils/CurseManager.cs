using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using UnboundLib.Networking;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using UnityEngine;
using System;
using BepInEx.Bootstrap;
using Photon.Pun;
using WillsWackyManagers.UI;
using ModdingUtils.Patches;
using System.Collections.ObjectModel;
using static UnityEngine.EventSystems.EventTrigger;

namespace WillsWackyManagers.Utils
{
    /// <summary>
    /// Manages the collection of curse cards given out, and contains various methods for utilizing them.
    /// </summary>
    public class CurseManager : MonoBehaviour
    {
        /// <summary>
        /// Instanced version of the class for accessibility from within static functions.
        /// </summary>
        public static CurseManager instance { get; private set; }
        private List<CardInfo> curses = new List<CardInfo>();
        private List<CardInfo> ActiveCurses => curses.Intersect(CardManager.cards.Values.ToArray().Where((card) => card.enabled).Select(card => card.cardInfo).ToArray()).ToList();
        private System.Random random = new System.Random();
        private bool deckCustomizationLoaded = false;

        public ReadOnlyCollection<CardInfo> Curses => new ReadOnlyCollection<CardInfo>(curses);

        public CardThemeColor.CardThemeColorType CurseGray => CardThemeLib.CardThemeLib.instance.CreateOrGetType("CurseGray", new CardThemeColor() { bgColor = new Color(0.34f, 0f, 0.44f), targetColor = new Color(0.24f, 0.24f, 0.24f) });
        /// <summary>
        /// Counterpart to DestructiveRed
        /// </summary>
        public CardThemeColor.CardThemeColorType CorruptedRed => CardThemeLib.CardThemeLib.instance.CreateOrGetType("CorruptedRed", new CardThemeColor() { bgColor = new Color32(60,0,0,200), targetColor = new Color32(110,20,20,200) });
        /// <summary>
        /// Counterpart to MagicPink
        /// </summary>
        public CardThemeColor.CardThemeColorType CursedPink => CardThemeLib.CardThemeLib.instance.CreateOrGetType("CursedPink", new CardThemeColor() { bgColor = new Color32(60, 10, 30, 200), targetColor = new Color32(110, 20, 70, 200) });
        /// <summary>
        /// Counterpart to FirepowerYellow
        /// </summary>
        public CardThemeColor.CardThemeColorType FoolsGold => CardThemeLib.CardThemeLib.instance.CreateOrGetType("FoolsGold", new CardThemeColor() { bgColor = new Color32(50, 50, 10, 200), targetColor = new Color32(110, 110, 20, 200) });
        /// <summary>
        /// Counterpart to PoisonGreen
        /// </summary>
        public CardThemeColor.CardThemeColorType ToxicGreen => CardThemeLib.CardThemeLib.instance.CreateOrGetType("ToxicGreen", new CardThemeColor() { bgColor = new Color32(0, 40, 0, 200), targetColor = new Color32(20, 100, 20, 200) });
        /// <summary>
        /// Counterpart to EvilPurple
        /// </summary>
        public CardThemeColor.CardThemeColorType FallenPurple => CardThemeLib.CardThemeLib.instance.CreateOrGetType("FallenPurple", new CardThemeColor() { bgColor = new Color32(50, 10, 50, 200), targetColor = new Color32(110, 20, 110, 200) });
        /// <summary>
        /// Counterpart to NatureBrown
        /// </summary>
        public CardThemeColor.CardThemeColorType ShitBrown => CardThemeLib.CardThemeLib.instance.CreateOrGetType("ShitBrown", new CardThemeColor() { bgColor = new Color32(50, 40, 0, 200), targetColor = new Color32(110, 90, 20, 200) });
        /// <summary>
        /// Counterpart to ColdBlue
        /// </summary>
        public CardThemeColor.CardThemeColorType FrozenBlue => CardThemeLib.CardThemeLib.instance.CreateOrGetType("FrozenBlue", new CardThemeColor() { bgColor = new Color32(0, 40, 50, 200), targetColor = new Color32(20, 90, 110, 200) });
        /// <summary>
        /// Counterpart to FracturedBlue
        /// </summary>
        public CardThemeColor.CardThemeColorType FracturedBlue => CardThemeLib.CardThemeLib.instance.CreateOrGetType("FracturedBlue", new CardThemeColor() { bgColor = new Color32(0, 0, 50, 200), targetColor = new Color32(20, 20, 110, 200) });
        /// <summary>
        /// Counterpart to TechWhite
        /// </summary>
        public CardThemeColor.CardThemeColorType ObseleteWhite => CardThemeLib.CardThemeLib.instance.CreateOrGetType("ObseleteWhite", new CardThemeColor() { bgColor = new Color32(40, 40, 40, 200), targetColor = new Color32(110, 110, 110, 200) });
        /// <summary>
        /// Counterpart to DefaultBlack
        /// </summary>
        public CardThemeColor.CardThemeColorType DefaultWhite => CardThemeLib.CardThemeLib.instance.CreateOrGetType("DefaultWhite", new CardThemeColor() { bgColor = new Color32(255, 255, 255, 255), targetColor = new Color32(255, 255, 255, 255) });

        /// <summary>
        /// The card category for all curses.
        /// </summary>
        public CardCategory curseCategory { get; private set; } = CustomCardCategories.instance.CardCategory("Curse");

        /// <summary>
        /// Curses that are nice to the palyer they're given.
        /// </summary>
        public CardCategory kindCurseCategory { get; private set; } = CustomCardCategories.instance.CardCategory("kindCurse");

        /// <summary>
        /// The card category for cards that aren't curses but count as them.
        /// </summary>
        public CardCategory countAsCurseCategory { get; private set; } = CustomCardCategories.instance.CardCategory("countAsCurse");

        /// <summary>
        /// The card category for cards that interact with cursed players.
        /// </summary>
        public CardCategory curseInteractionCategory { get; private set; } = CustomCardCategories.instance.CardCategory("Cursed");

        /// <summary>
        /// The card category for cards that grants curses to players.
        /// </summary>
        public CardCategory curseSpawnerCategory { get; private set; } = CustomCardCategories.instance.CardCategory("Grants Curses");

        private Dictionary<Player, bool> canDrawCurses = new Dictionary<Player, bool>();

        public void PlayerCanDrawCurses(Player player, bool canDraw = true)
        {
            canDrawCurses[player] = canDraw;
        }

        public bool CanPlayerDrawCurses(Player player)
        {
            if (canDrawCurses.TryGetValue(player, out bool canDraw))
            {
                return canDraw;
            }
            return false;
        }

        private bool CanDrawCursesCondition(Player player, CardInfo card)
        {
            if (!card)
            {
                return true;
            }

            if (!curses.Contains(card))
            {
                return true;
            }

            if (!player)
            {
                return true;
            }

            if (canDrawCurses.TryGetValue(player, out bool value))
            {
                return value;
            }
            return false;
        }

        private bool CanDrawCurseInteractionCondition(Player player, CardInfo card)
        {
            if (!card.categories.Contains(curseInteractionCategory))
            {
                return true;
            }

            if (!player)
            {
                return true;
            }

            if (!HasCurse(player))
            {
                return false;
            }

            return true;
        }

        private bool CanDrawCurseSpawnerCondition(Player player, CardInfo card)
        {
            if (!card.categories.Contains(curseSpawnerCategory))
            {
                return true;
            }

            if (!WillsWackyManagers.enableCurseSpawning)
            {
                return false;
            }

            return true;
        }

        private void Start()
        {
            ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(CanDrawCursesCondition);
            ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(CanDrawCurseInteractionCondition);
            ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(CanDrawCurseSpawnerCondition);
            this.ExecuteAfterFrames(50, () =>
            {
                foreach (var plugin in Chainloader.PluginInfos)
                {
                    if (plugin.Key == "pykess.rounds.plugins.deckcustomization")
                    {
                        deckCustomizationLoaded = true;
                        break;
                    }
                }

                if (deckCustomizationLoaded)
                {

                }
            });

            GameModeManager.AddHook(GameModeHooks.HookGameStart, GameStart);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, OnPickStart);

            keepCurse = new CurseRemovalOption("Keep Curse", (player) => true, IKeepCurse);
            removeRound = new CurseRemovalOption("-1 round, -2 curses", CondRemoveRound, IRemoveRound);
            removeAllCards = new CurseRemovalOption("Lose all cards, lose all curses", CondRemoveAllCards, IRemoveAllCards);
            giveExtraPick = new CurseRemovalOption("You: -2 curses, Enemies: +1 Pick", CondGiveExtraPick, IGiveExtraPick);
            doNotAsk = new CurseRemovalOption("Do Not Ask Again", (player) => true, IStopAsking);

            RegisterRemovalOption(keepCurse);
            RegisterRemovalOption(giveExtraPick);
            RegisterRemovalOption(removeAllCards);
            RegisterRemovalOption(removeRound);
            RegisterRemovalOption(doNotAsk);
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                DestroyImmediate(this);
                return;
            }
        }

        private void CheckCurses()
        {
            var _ = ActiveCurses;
            //foreach (var item in activeCurses)
            //{
            //    WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] {item.cardName} is an enabled curse.");
            //}
        }

        /// <summary>
        /// Returns a random curse from the list of curses, if one exists.
        /// </summary>
        /// <returns>CardInfo for the generated curse.</returns>
        public CardInfo RandomCurse()
        {
            var curse = CardChoicePatchGetRanomCard.OrignialGetRanomCard(ActiveCurses.ToArray()).GetComponent<CardInfo>();
            if (!curse)
            {
                curse = FallbackMethod(curses.ToArray());
            }

            return curse;
        }

        /// <summary>
        /// Returns a random curse from the list of curses, if one exists.
        /// </summary>
        /// <param name="player">A player for whom the curse has to be valid for.</param>
        /// <returns>CardInfo for the generated curse.</returns>
        public CardInfo RandomCurse(Player player)
        {
            return RandomCurse(player, (card, person) => { return true; });
        }

        /// <summary>
        /// Returns a random curse from the list of curses, if one exists.
        /// </summary>
        /// <param name="player">A player for whom the curse has to be valid for.</param>
        /// <param name="condition">A condition for returning a valid curse. If none meet the condition, a random curse will be given instead.</param>
        /// <returns>CardInfo for the generated curse.</returns>
        public CardInfo RandomCurse(Player player, Func<CardInfo, Player, bool> condition)
        {
            CheckCurses();

            bool canDraw = CanPlayerDrawCurses(player);
            canDrawCurses[player] = true;

            var availableCurses = ActiveCurses.Where((card) => ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, card) && condition(card, player)).ToArray();

            //CardChoice.instance.cards = availableCurses;

            CardInfo curse = null;

            try
            {
                curse = CardChoicePatchGetRanomCard.OrignialGetRanomCard(availableCurses.ToArray()).GetComponent<CardInfo>();
            }
            catch (NullReferenceException a)
            {

            }
            catch (Exception e) 
            { 
                UnityEngine.Debug.LogException(e); 
            }

            //curse = ((GameObject)CardChoice.instance.InvokeMethod("GetRanomCard")).GetComponent<CardInfo>();

            //CardChoice.instance.cards = enabled;

            canDrawCurses[player] = canDraw;

            if (!curse || !curses.Contains(curse))
            {
                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] curse didn't exist, getting one now.");
                curse = FallbackMethod(availableCurses.ToArray());
                if (!curse)
                {
                    curse = FallbackMethod(ActiveCurses.ToArray());
                }
                if (!curse)
                {
                    curse = FallbackMethod(curses.ToArray());
                }
            }

            return curse;
        }

        public bool PlayerIsAllowedCurse(Player player, CardInfo card)
        {
            if (player == null)
            {
                return true;
            }

            if (card == null || !curses.Contains(card))
            {
                return false;
            }

            bool canDraw = CurseManager.instance.CanPlayerDrawCurses(player);
            CurseManager.instance.PlayerCanDrawCurses(player, true);
            bool allowed = ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, card);
            CurseManager.instance.PlayerCanDrawCurses(player, canDraw);

            if (allowed)
            {
                return true;
            }

            return false;
        }

        private CardInfo FallbackMethod(CardInfo[] availableChoices)
        {
            CardInfo curse = null;

            var totalWeight = 0f;

            var rarities = RarityLib.Utils.RarityUtils.Rarities.Values.ToDictionary(r => r.value, r => r);

            foreach (var cardInfo in availableChoices)
            {
                if (rarities.TryGetValue(cardInfo.rarity, out RarityLib.Utils.Rarity value))
                {
                    totalWeight += (value.relativeRarity * 10f);
                }
            }

            var chosenWeight = UnityEngine.Random.Range(0f, totalWeight);

            //WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] {chosenWeight}/{totalWeight} weight chosen.");

            foreach (var cardInfo in availableChoices)
            {
                if (rarities.TryGetValue(cardInfo.rarity, out RarityLib.Utils.Rarity value))
                {
                    chosenWeight -= (value.relativeRarity * 10f);
                }

                //WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] {cardInfo.cardName} reduced weight to {chosenWeight}.");

                if (chosenWeight <= 0f)
                {
                    curse = cardInfo;
                    break;
                }
            }

            return curse;
        }

        /// <summary>
        /// Curses a player with a random curse.
        /// </summary>
        /// <param name="player">The player to curse.</param>
        public void CursePlayer(Player player)
        {
            CursePlayer(player, null, null);
        }

        /// <summary>
        /// Curses a player with a random curse.
        /// </summary>
        /// <param name="player">The player to curse.</param>
        /// <param name="callback">An action to run with the information of the curse.</param>
        public void CursePlayer(Player player, Action<CardInfo> callback)
        {
            CursePlayer(player, callback, null);
        }

        /// <summary>
        /// Curses a player with a random curse.
        /// </summary>
        /// <param name="player">The player to curse.</param>
        /// <param name="condition">A condition for the curse. If no curses meet the condition, a random one will be given instead.</param>
        public void CursePlayer(Player player, Func<CardInfo, Player, bool> condition)
        {
            CursePlayer(player, null, condition);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player">The player to curse.</param>
        /// <param name="callback">An action to run with the information of the curse.</param>
        /// <param name="condition">A condition for the curse. If no curses meet the condition, a random one will be given instead.</param>
        public void CursePlayer(Player player, Action<CardInfo> callback, Func<CardInfo, Player, bool> condition)
        {
            CardInfo curse = null;
            if (condition != null)
            {
                curse = RandomCurse(player, condition);
            }
            else
            {
                curse = RandomCurse(player);
            }

            WillsWackyManagers.instance.DebugLog($"[WWM][Curse Manager] Player {player.playerID} cursed with {curse.cardName}.");
            ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, curse, false, "", 2f, 2f, true);
            callback?.Invoke(curse);
        }

        /// <summary>
        /// Adds the curse to the list of available curses.
        /// </summary>
        /// <param name="cardInfo">The card to register.</param>
        public void RegisterCurse(CardInfo cardInfo)
        {
            curses.Add(cardInfo);
            //ModdingUtils.Utils.Cards.instance.AddHiddenCard(cardInfo);
            //CheckCurses();
        }

        /// <summary>
        /// Returns an array containing all curses.
        /// </summary>
        public CardInfo[] GetRaw(bool activeOnly = false)
        {
            CardInfo[] result;

            if (activeOnly)
            {
                result = ActiveCurses.ToArray();
            }
            else
            {
                result = curses.ToArray();
            }

            return result;
        }

        /// <summary>
        /// Checks to see if a player has a curse.
        /// </summary>
        /// <param name="player">The player to check for a curse.</param>
        /// <returns>Returns true if a player has a registered curse.</returns>
        public bool HasCurse(Player player)
        {
            bool result = false;

            result = curses.Intersect(player.data.currentCards).Any();

            return result;
        }

        /// <summary>
        /// Checks to see if a card is a curse or not.
        /// </summary>
        /// <param name="cardInfo">The card to check.</param>
        /// <returns>Returns true if the card is a registered curse.</returns>
        public bool IsCurse(CardInfo cardInfo)
        {
            return curses.Contains(cardInfo);
        }

        /// <summary>
        /// Returns an array of all curse cards on a player. May contain duplicates. 
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>Array of curse cards.</returns>
        public CardInfo[] GetAllCursesOnPlayer(Player player)
        {
            List<CardInfo> cards = new List<CardInfo>();

            foreach (var card in player.data.currentCards)
            {
                if (IsCurse(card))
                {
                    cards.Add(card);
                }
            }

            return cards.ToArray();
        }

        /// <summary>
        /// Removes all curses from the specified player.
        /// </summary>
        /// <param name="player">The player to remove curses from.</param>
        public void RemoveAllCurses(Player player)
        {
            Action<CardInfo[]> callback = null;
            RemoveAllCurses(player, callback);
        }

        /// <summary>
        /// Removes all curses from the specified player.
        /// </summary>
        /// <param name="player">The player to remove curses from.</param>
        /// <param name="callback">An optional callback to run with each curse removed.</param>
        public void RemoveAllCurses(Player player, Action<CardInfo> callback)
        {
            StartCoroutine(IRemoveAllCurses(player, callback));
        }

        /// <summary>
        /// Removes all curses from the specified player.
        /// </summary>
        /// <param name="player">The player to remove curses from.</param>
        /// <param name="callback">An optional callback to run with all removed curse.</param>
        public void RemoveAllCurses(Player player, Action<CardInfo[]> callback)
        {
            StartCoroutine(IRemoveAllCurses(player, callback));
        }

        private IEnumerator IRemoveAllCurses(Player player, Action<CardInfo> callback)
        {
            int[] curseIndeces = Enumerable.Range(0, player.data.currentCards.Count()).Where((index) => IsCurse(player.data.currentCards[index])).ToArray();
            CardInfo[] playerCurses = ModdingUtils.Utils.Cards.instance.RemoveCardsFromPlayer(player, curseIndeces);

            foreach (var curse in playerCurses)
            {
                callback?.Invoke(curse);
                yield return WaitFor.Frames(20);
            }

            yield break;
        }

        private IEnumerator IRemoveAllCurses(Player player, Action<CardInfo[]> callback)
        {
            int[] curseIndeces = Enumerable.Range(0, player.data.currentCards.Count()).Where((index) => IsCurse(player.data.currentCards[index])).ToArray();
            CardInfo[] playerCurses = ModdingUtils.Utils.Cards.instance.RemoveCardsFromPlayer(player, curseIndeces);

            callback?.Invoke(playerCurses);
            yield return WaitFor.Frames(20);

            yield break;
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

        /***************************
        ****************************
        ** Curse Removal handling **
        ****************************
        ***************************/

        /************************
        ** Curse Removal Class **
        ************************/
        /// <summary>
        /// A way to remove curses.
        /// </summary>
        public struct CurseRemovalOption
        {
            public readonly string name;
            public readonly Func<Player, bool> condition;
            public readonly Func<Player, IEnumerator> action;

            /// <summary>
            /// Creates a Curse Removal Option
            /// </summary>
            /// <param name="optionName">The text the player sees for choosing the option.</param>
            /// <param name="optionCondition">A function that takes in a player object as input and outputs a bool. When true the option is available for players.</param>
            /// <param name="optionAction">An IEnumerator that takes in a player object as input. Run when the option is selected. If it wishes to remove a curse, it must do so.</param>
            public CurseRemovalOption(string optionName, Func<Player, bool> optionCondition, Func<Player, IEnumerator> optionAction)
            {
                name = optionName.ToUpper();
                condition = optionCondition;
                action = optionAction;
            }
        }

        /**********************************
        ** Default curse removal options **
        **********************************/
        //Always shows up.
        private CurseRemovalOption keepCurse;

        private IEnumerator IKeepCurse(Player player)
        {
            yield break;
        }

        private CurseRemovalOption removeRound;

        private bool CondRemoveRound(Player player)
        {
            var result = false;
            // Only shows up if they have a round point to remove.
            if (GameModeManager.CurrentHandler.GetTeamScore(player.teamID).rounds > 0 && GetAllCursesOnPlayer(player).Count() > 1)
            {
                result = true;
            }

            return result;
        }

        private IEnumerator IRemoveRound(Player player)
        {
            var score = GameModeManager.CurrentHandler.GetTeamScore(player.teamID);
            GameModeManager.CurrentHandler.SetTeamScore(player.teamID, new TeamScore(score.points, score.rounds - 1));

            var roundCounter = GameObject.Find("/Game/UI/UI_Game/Canvas/RoundCounter").GetComponent<RoundCounter>();
            roundCounter.InvokeMethod("ReDraw");

            // Get the indexes of all curses on the player.
            var curseIndices = Enumerable.Range(0, player.data.currentCards.Count()).Where((index) => player.data.currentCards[index].categories.Contains(curseCategory)).ToList();

            // Create a new list for the indices.
            var indicesToRemove = new List<int>();

            // Randomly select 2 curses.
            for (int i = 0; i < curseIndices.Count(); i++)
            {
                if (UnityEngine.Random.Range(0f, 1f) <= 2f / (curseIndices.Count() - i - indicesToRemove.Count()))
                {
                    indicesToRemove.Add(curseIndices[i]);
                }

                if (indicesToRemove.Count >= 2)
                {
                    break;
                }
            }

            ModdingUtils.Utils.Cards.instance.RemoveCardsFromPlayer(player, indicesToRemove.ToArray());

            // Wait a second to let any card effects to occur.
            yield return new WaitForSecondsRealtime(1f);

            yield break;
        }

        private CurseRemovalOption giveExtraPick;

        private bool CondGiveExtraPick(Player player)
        {
            var result = true;

            // It only shows up if they do not have the least amount of cards.
            if (player.data.currentCards.Count() <= PlayerManager.instance.players.Select((person) => person.data.currentCards.Count()).ToArray().Min())
            {
                result = false;
            }

            // Need to have at least 2 curses to remove.
            if (GetAllCursesOnPlayer(player).Count() < 2)
            {
                result = false;
            }

            return result;
        }

        private IEnumerator IGiveExtraPick(Player player)
        {
            // Get all our enemies and give them a pick.
            var enemies = PlayerManager.instance.players.Where((person) => person.teamID != player.teamID);
            var pickers = enemies.ToDictionary((person) => person, (person) => 1);
            yield return WillsWackyManagers.ExtraPicks(pickers);

            // Wait a second to let any card effects to occur.
            yield return new WaitForSecondsRealtime(1f);

            // Get the indexes of all curses on the player.
            var curseIndices = Enumerable.Range(0, player.data.currentCards.Count()).Where((index) => player.data.currentCards[index].categories.Contains(curseCategory)).ToList();

            // Create a new list for the indices.
            var indicesToRemove = new List<int>();

            // Randomly select 2 curses.
            for (int i = 0; i < curseIndices.Count(); i++)
            {
                if (UnityEngine.Random.Range(0f, 1f) <= 2f / (curseIndices.Count() - i - indicesToRemove.Count()))
                {
                    indicesToRemove.Add(curseIndices[i]);
                }

                if (indicesToRemove.Count >= 2)
                {
                    break;
                }
            }

            ModdingUtils.Utils.Cards.instance.RemoveCardsFromPlayer(player, indicesToRemove.ToArray());

            // Wait a second to let any card effects to occur.
            yield return new WaitForSecondsRealtime(1f);

            // Exit our action
            yield break;
        }

        private CurseRemovalOption removeAllCards;

        private bool CondRemoveAllCards(Player player)
        {
            var result = true;

            // We only want it to show up if they have 5 or more curses.
            if (instance.GetAllCursesOnPlayer(player).Count() <= 4)
            {
                result = false;
            }

            return result;
        }

        private IEnumerator IRemoveAllCards(Player player)
        {
            ModdingUtils.Utils.Cards.instance.RemoveAllCardsFromPlayer(player);
            yield break;
        }

        private CurseRemovalOption doNotAsk;

        private IEnumerator IStopAsking(Player player)
        {
            doNotAskPlayers.Add(player);
            yield break;
        }

        /************************
        ** Curse removal logic ** 
        ************************/

        /// <summary>
        /// Registers a curse removal option for players to use.
        /// </summary>
        /// <param name="optionName">The text the player sees for choosing the option.</param>
        /// <param name="optionCondition">When should the option be shown? Takes a player value as input.</param>
        /// <param name="optionAction">If the option is selected, what happens? If a curse is to be removed, this action must do so. Takes a player as input.</param>
        public void RegisterRemovalOption(string optionName, Func<Player, bool> optionCondition, Func<Player, IEnumerator> optionAction)
        {
            RegisterRemovalOption(new CurseRemovalOption(optionName, optionCondition, optionAction));
        }

        /// <summary>
        /// Registers a curse removal option for players to use.
        /// </summary>
        /// <param name="option">The option to register.</param>
        public void RegisterRemovalOption(CurseRemovalOption option)
        {
            if (removalOptions.Select((item) => item.name).Contains(option.name))
            {
                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] A curse removal option called '{option.name}' already exists.");
                return;
            }
            removalOptions.Add(option);
        }



        private List<CurseRemovalOption> removalOptions = new List<CurseRemovalOption>();

        private bool playerDeciding = false;
        private bool choseAction = false;
        private Player decidingPlayer;

        private Dictionary<Player, int> curseCount = new Dictionary<Player, int>();

        private List<Player> doNotAskPlayers = new List<Player>();

        private IEnumerator GiveCurseRemovalOptions(Player player)
        {
            WillsWackyManagers.instance.DebugLog($"[WWM][Curse Removal] Presenting Curse Removal options for player {player.playerID}.");
            //StartCoroutine(TimeOut(player));

            var validOptions = removalOptions.Where((option) => option.condition(player)).ToList();

            foreach (var option in validOptions)
            {
                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] '{option.name}' is a valid curse removal option for player {player.playerID}.");
            }

            List<string> choices = new List<string>();

            choices.Add(keepCurse.name); // We want keep curse to be the first option presented.
            choices = choices.Concat(validOptions.Where((option) => option.name != keepCurse.name).Select((option) => option.name).ToList()).ToList();

            playerDeciding = true;
            decidingPlayer = player;
            choseAction = false;

            if (player.data.view.IsMine || PhotonNetwork.OfflineMode)
            {
                try
                {
                    PopUpMenu.instance.Open(choices, OnRemovalOptionChosen);
                }
                catch (NullReferenceException)
                {
                    WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Popup menu doesn't exist.");
                    choseAction = true;
                    playerDeciding = false;
                }
            }
            else
            {
                string playerName = PhotonNetwork.CurrentRoom.GetPlayer(player.data.view.OwnerActorNr).NickName;
                UIHandler.instance.ShowJoinGameText($"WAITING FOR {playerName}", PlayerSkinBank.GetPlayerSkinColors(player.teamID).winText);
            }

            yield return new WaitUntil(() => !playerDeciding);

            UIHandler.instance.HideJoinGameText();

            yield break;
        }

        private IEnumerator TimeOut(Player player)
        {
            yield return new WaitForSecondsRealtime(60f);

            if (decidingPlayer == player && playerDeciding)
            {
                if (choseAction)
                {
                    playerDeciding = false;
                }
                else
                {
                    PopUpMenu.instance.InvokeMethod("Choose");
                    yield return new WaitForSecondsRealtime(30f);
                }
            }

            yield break;
        }

        private void OnRemovalOptionChosen(string choice)
        {
            if (!PhotonNetwork.OfflineMode)
            {
                //decidingPlayer.data.view.RPC(nameof(RPCA_ExecuteChosenOption), RpcTarget.AllViaServer, choice);
                NetworkingManager.RPC(typeof(CurseManager), nameof(RPCA_ExecuteChosenOption), choice);
            }
            else
            {
                StartCoroutine(IExecuteChosenOption(choice));
            }
        }

        [UnboundRPC]
        private static void RPCA_ExecuteChosenOption(string choice)
        {
            CurseManager.instance.ExecuteChosenOption(choice);
        }

        private void ExecuteChosenOption(string choice)
        {
            StartCoroutine(IExecuteChosenOption(choice));
        }

        private IEnumerator IExecuteChosenOption(string choice)
        {
            UIHandler.instance.HideJoinGameText();

            WillsWackyManagers.instance.DebugLog($"[WWM][Curse Removal] Player {decidingPlayer.playerID} picked the \"{choice}\" curse removal option. Now executing.");

            choseAction = true;
            var chosenAction = removalOptions.Where((option) => option.name == choice).FirstOrDefault().action;

            yield return chosenAction(decidingPlayer);

            //decidingPlayer.data.view.RPC(nameof(RPCA_ExecutionOver), RpcTarget.AllViaServer, decidingPlayer.playerID);
            if (!PhotonNetwork.OfflineMode)
            {
                //decidingPlayer.data.view.RPC(nameof(RPCA_ExecutionOver), RpcTarget.AllViaServer, decidingPlayer.playerID);
                NetworkingManager.RPC(typeof(CurseManager), nameof(RPCA_ExecutionOver), decidingPlayer.playerID);
            }
            else
            {
                playerDeciding = false;
            }
        }

        [UnboundRPC]
        private static void RPCA_ExecutionOver(int playerID)
        {
            if (CurseManager.instance.decidingPlayer == PlayerManager.instance.players.Where(player => player.playerID == playerID).First())
            {
                CurseManager.instance.playerDeciding = false;
            }
        }

        /**************************
        ***** Game Mode Hooks *****
        **************************/

        private IEnumerator OnPickStart(IGameModeHandler gm)
        {
            // If using the curse removal options
            if (WillsWackyManagers.enableCurseRemoval && Networking.SettingCoordinator.instance.Synced)
            {
                WillsWackyManagers.instance.DebugLog($"[WWM][Curse Removal] Curse Removal Options are enabled.");

                foreach (var player in PlayerManager.instance.players)
                {
                    var currentCurses = GetAllCursesOnPlayer(player).Count();
                    var presentRemovalOption = false;

                    // The player values do not exist initially.
                    if (curseCount.TryGetValue(player, out var curses))
                    {
                        // if they've gained curses since last round
                        if (GetAllCursesOnPlayer(player).Count() > curses)
                        {
                            // give option to remove curses
                            presentRemovalOption = true;
                        }
                    }
                    else
                    {
                        // if the value didn't exist, we see if they have any curses
                        if (currentCurses > 0)
                        {
                            presentRemovalOption = true;
                        }
                    }

                    if (currentCurses > 0)
                    {
                        presentRemovalOption = true;
                    }

                    if (doNotAskPlayers.Contains(player))
                    {
                        presentRemovalOption = false;
                    }

                    if (presentRemovalOption)
                    {
                        yield return GiveCurseRemovalOptions(player);
                    }
                }

                yield return new WaitForSecondsRealtime(1f);

                // Clear and rebuild our curse tracker
                curseCount.Clear();

                curseCount = PlayerManager.instance.players.ToDictionary((player) => player, (player) => GetAllCursesOnPlayer(player).Count());
            }

            yield break;
        }

        private IEnumerator GameStart(IGameModeHandler gm)
        {
            try
            {
                curseCount.Clear();
            }
            catch (NullReferenceException)
            {
                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Clearing curse count caused an error");
            }

            try
            {
                curseCount = PlayerManager.instance.players.ToDictionary((player) => player, (player) => 0);
            }
            catch (NullReferenceException)
            {
                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Building a dictionary caused an error.");
            }

            canDrawCurses = new Dictionary<Player, bool>();

            yield break;
        }
    }
}
