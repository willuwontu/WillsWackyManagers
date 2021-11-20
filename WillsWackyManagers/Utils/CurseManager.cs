using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using UnityEngine;
using System;
using BepInEx.Bootstrap;
using Photon.Pun;

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
        private List<CardInfo> activeCurses = new List<CardInfo>();
        private System.Random random = new System.Random();
        private bool deckCustomizationLoaded = false;

        /// <summary>
        /// The card category for all curses.
        /// </summary>
        public CardCategory curseCategory { get; private set; } = CustomCardCategories.instance.CardCategory("Curse");

        /// <summary>
        /// The card category for cards that interact with cursed players.
        /// </summary>
        public CardCategory curseInteractionCategory { get; private set; } = CustomCardCategories.instance.CardCategory("Cursed");

        /// <summary>
        /// The card category for cards that grants curses to players.
        /// </summary>
        public CardCategory curseSpawnerCategory { get; private set; } = CustomCardCategories.instance.CardCategory("Grants Curses");

        private void Start()
        {
            instance = this;

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
            removeRound = new CurseRemovalOption("-1 round, -1 curse", CondRemoveRound, IRemoveRound);
            removeAllCards = new CurseRemovalOption("Lose all cards, lose all curses", CondRemoveAllCards, IRemoveAllCards);
            giveExtraPick = new CurseRemovalOption("You: -1 curse, Enemies: +1 uncommon", CondGiveExtraPick, IGiveExtraPick);

            RegisterRemovalOption(keepCurse);
            RegisterRemovalOption(giveExtraPick);
            RegisterRemovalOption(removeAllCards);
            RegisterRemovalOption(removeRound);
        }

        private void CheckCurses()
        {
            activeCurses = curses.Intersect(CardManager.cards.Values.ToArray().Where((card) => card.enabled).Select(card => card.cardInfo).ToArray()).ToList();
            foreach (var item in activeCurses)
            {
                UnityEngine.Debug.Log($"[WWM][Debugging] {item.cardName} is an enabled curse.");
            }
        }

        /// <summary>
        /// Returns a random curse from the list of curses, if one exists.
        /// </summary>
        /// <returns>CardInfo for the generated curse.</returns>
        public CardInfo RandomCurse()
        {
            var curse = FallbackMethod(activeCurses.ToArray());
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

            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == curseCategory);

            CardInfo curse = ModdingUtils.Utils.Cards.instance.GetRandomCardWithCondition(player, player.data.weaponHandler.gun, player.data.weaponHandler.gun.GetComponentInChildren<GunAmmo>(), player.data, player.data.healthHandler, player.GetComponent<Gravity>(), player.data.block, player.data.stats, (cardInfo, person, gun, gunAmmo, data, healthHandler, gravity, block, stats) => {
                var result = false;

                result = activeCurses.Contains(cardInfo) && condition(cardInfo, person);

                return result;
            }, 100);

            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(curseCategory);

            if (!curse)
            {
                UnityEngine.Debug.Log($"[WWM][Debugging] curse didn't exist, getting one now.");
                curse = FallbackMethod(activeCurses.ToArray());
                if (!curse)
                {
                    curse = FallbackMethod(curses.ToArray());
                }
            }

            return curse;
        }

        private CardInfo FallbackMethod(CardInfo[] availableChoices)
        {
            CardInfo curse = null;

            var totalWeight = 0f;
            foreach (var cardInfo in availableChoices)
            {
                switch (cardInfo.rarity)
                {
                    case CardInfo.Rarity.Common:
                        totalWeight += 10f;
                        break;
                    case CardInfo.Rarity.Uncommon:
                        totalWeight += 4f;
                        break;
                    case CardInfo.Rarity.Rare:
                        totalWeight += 1f;
                        break;
                }
            }

            var chosenWeight = UnityEngine.Random.Range(0f, totalWeight);

            //UnityEngine.Debug.Log($"[WWM][Debugging] {chosenWeight}/{totalWeight} weight chosen.");

            foreach (var cardInfo in availableChoices)
            {
                switch (cardInfo.rarity)
                {
                    case CardInfo.Rarity.Common:
                        chosenWeight -= 10f;
                        break;
                    case CardInfo.Rarity.Uncommon:
                        chosenWeight -= 4f;
                        break;
                    case CardInfo.Rarity.Rare:
                        chosenWeight -= 1f;
                        break;
                }

                //UnityEngine.Debug.Log($"[WWM][Debugging] {cardInfo.cardName} reduced weight to {chosenWeight}.");

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

            UnityEngine.Debug.Log($"[WWM][Curse Manager] Player {player.playerID} cursed with {curse.cardName}.");
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
                result = activeCurses.ToArray();
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
            RemoveAllCurses(player, null);
        }

        /// <summary>
        /// Removes all curses from the specified player.
        /// </summary>
        /// <param name="player">The player to remove curses from.</param>
        /// <param name="callback">An optional callback to run with each card removed.</param>
        public void RemoveAllCurses(Player player, Action<CardInfo> callback)
        {
            StartCoroutine(IRemoveAllCurses(player, callback));
        }

        private IEnumerator IRemoveAllCurses(Player player, Action<CardInfo> callback)
        {
            List<CardInfo> playerCurses = new List<CardInfo>();
            while (HasCurse(player))
            {
                for (int i = 0; i < player.data.currentCards.Count(); i++)
                {
                    var card = player.data.currentCards[i];
                    if (IsCurse(card))
                    {
                        playerCurses.Add(card);
                        ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, i);
                        break;
                    }
                }

                yield return WaitFor.Frames(40);
            }

            foreach (var curse in playerCurses)
            {
                callback?.Invoke(curse);
                yield return WaitFor.Frames(40);
            }

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
            if (GameModeManager.CurrentHandler.GetTeamScore(player.teamID).rounds > 0)
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

            for (var i = player.data.currentCards.Count() - 1; i >= 0; i--)
            {
                if (instance.IsCurse(player.data.currentCards[i]))
                {
                    ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, i);
                    break;
                }
            }
            yield break;
        }

        private CurseRemovalOption giveExtraPick;

        private bool CondGiveExtraPick(Player player)
        {
            var result = false;

            // It only shows up if they do not have the least amount of cards.
            if (player.data.currentCards.Count() > PlayerManager.instance.players.Select((person) => person.data.currentCards.Count()).ToArray().Min())
            {
                result = true;
            }

            return result;
        }

        private IEnumerator IGiveExtraPick(Player player)
        {
            for (var i = player.data.currentCards.Count() - 1; i >= 0; i--)
            {
                if (instance.IsCurse(player.data.currentCards[i]))
                {
                    var enemies = PlayerManager.instance.players.Where((person) => person.teamID != player.teamID);
                    //var pickers = enemies.ToDictionary((person) => person, (person) => 1);
                    //yield return WillsWackyManagers.ExtraPicks(pickers);

                    foreach (var enemy in enemies)
                    {
                        var gun = enemy.GetComponent<Holding>().holdable.GetComponent<Gun>();
                        var data = enemy.GetComponent<CharacterData>();
                        var health = enemy.GetComponent<HealthHandler>();
                        var gravity = enemy.GetComponent<Gravity>();
                        var block = enemy.GetComponent<Block>();
                        var gunAmmo = gun.GetComponentInChildren<GunAmmo>();
                        var stats = enemy.GetComponent<CharacterStatModifiers>();

                        var card = ModdingUtils.Utils.Cards.instance.GetRandomCardWithCondition(enemy, gun, gunAmmo, data, health, gravity, block, stats, UncommonCondition);

                        ModdingUtils.Utils.Cards.instance.AddCardToPlayer(enemy, card, false, "", 2f, 2f, true);
                    }

                    ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, i);
                    break;
                }
            }
            yield break;
        }

        private bool UncommonCondition(CardInfo card, Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {

            return card.rarity == CardInfo.Rarity.Uncommon;
        }

        private CurseRemovalOption removeAllCards;

        private bool CondRemoveAllCards(Player player)
        {
            var result = false;

            // We only want it to show up if they have 5 or more curses.
            if (instance.GetAllCursesOnPlayer(player).Count() > 4)
            {
                result = true;
            }

            return result;
        }

        private IEnumerator IRemoveAllCards(Player player)
        {
            ModdingUtils.Utils.Cards.instance.RemoveAllCardsFromPlayer(player);
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
                UnityEngine.Debug.Log($"[WWM][Debugging] A curse removal option called '{option.name}' already exists.");
                return;
            }
            removalOptions.Add(option);
        }



        private List<CurseRemovalOption> removalOptions = new List<CurseRemovalOption>();

        private bool playerDeciding = false;
        private bool choseAction = false;
        private Player decidingPlayer;

        private Dictionary<Player, int> curseCount = new Dictionary<Player, int>();

        private IEnumerator GiveCurseRemovalOptions(Player player)
        {
            playerDeciding = true;
            decidingPlayer = player;

            StartCoroutine(TimeOut(player));

            var validOptions = removalOptions.Where((option) => option.condition(player)).ToList();

            foreach (var option in validOptions)
            {
                UnityEngine.Debug.Log($"[WWM][Debugging] '{option.name}' is a valid curse removal option for player {player.playerID}.");
            }

            List<string> choices; 
            
            choices = validOptions.Select((option) => option.name).ToList();
            choices.Remove(keepCurse.name);
            choices.Add(keepCurse.name); // We want keep curse to be the last option presented.

            choseAction = false;

            if (player.data.view.IsMine || PhotonNetwork.OfflineMode)
            {
                try
                {
                    UI.PopUpMenu.instance.Open(choices, OnRemovalOptionChosen);
                }
                catch (NullReferenceException)
                {
                    UnityEngine.Debug.Log($"[WWM][Debugging] Popup menu doesn't exist.");
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
                    UI.PopUpMenu.instance.InvokeMethod("Choose");
                    yield return new WaitForSecondsRealtime(30f);
                }
            }

            yield break;
        }

        private void OnRemovalOptionChosen(string choice)
        {
            if (!PhotonNetwork.OfflineMode)
            {
                decidingPlayer.data.view.RPC(nameof(RPC_ExecuteChosenOption), RpcTarget.All, choice);
            }
            else
            {
                StartCoroutine(IExecuteChosenOption(choice));
            }
        }

        [PunRPC]
        private void RPC_ExecuteChosenOption(string choice)
        {
            ExecuteChosenOption(choice);
            choseAction = true;
        }

        private void ExecuteChosenOption(string choice)
        {
            StartCoroutine(IExecuteChosenOption(choice));
        }

        private IEnumerator IExecuteChosenOption(string choice)
        {
            var chosenAction = removalOptions.Where((option) => option.name == choice).FirstOrDefault().action;

            yield return chosenAction(decidingPlayer);

            playerDeciding = false;
        }


        /**************************
        ***** Game Mode Hooks *****
        **************************/

        private IEnumerator OnPickStart(IGameModeHandler gm)
        {
            // If using the curse removal options
            if (WillsWackyManagers.enableCurseRemoval.Value)
            {
                // Create a list of our current values
                var current = PlayerManager.instance.players.ToDictionary((player) => player, (player) => GetAllCursesOnPlayer(player).Count());

                foreach (var player in PlayerManager.instance.players)
                {
                    // The player values do not exist initially.
                    if (curseCount.TryGetValue(player, out var curses))
                    {
                        // if they've gained curses since last round
                        if (current[player] > curses)
                        {
                            // give option to remove curses
                            yield return GiveCurseRemovalOptions(player);
                        }
                    }
                    else
                    {
                        // if the value didn't exist, we see if they have any curses
                        if (current[player] > 0)
                        {
                            yield return GiveCurseRemovalOptions(player);
                        }
                    }
                }

                yield return new WaitForSecondsRealtime(1f);

                // Clear and rebuild our curse tracker
                curseCount.Clear();
                foreach (var player in PlayerManager.instance.players)
                {
                    curseCount.Add(player, GetAllCursesOnPlayer(player).Count());
                }
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
                UnityEngine.Debug.Log($"[WWM][Debugging] Clearing curse count caused an error");
            }

            try
            {
                curseCount = PlayerManager.instance.players.ToDictionary((player) => player, (player) => 0);
            }
            catch (NullReferenceException)
            {
                UnityEngine.Debug.Log($"[WWM][Debugging] Building a dictionary caused an error.");
            }

            yield break;
        }
    }
}
