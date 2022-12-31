using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnboundLib;
using UnboundLib.Utils;
using ModdingUtils.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using Photon.Pun;
using WillsWackyManagers.Networking;
using WillsWackyManagers.Extensions;

namespace WillsWackyManagers.Utils
{
    /// <summary>
    /// Manages effects related to clearing out a player's cards and replacing all of them.
    /// </summary>
    public class RerollManager : MonoBehaviour
    {
        /// <summary>
        /// Instanced version of the class for accessibility from within static functions.
        /// </summary>
        public static RerollManager instance { get; private set; }

        /// <summary>
        /// The card category for cards that should not be given out after a table flip.
        /// </summary>
        public CardCategory NoFlip { get; private set; } = CustomCardCategories.instance.CardCategory("NoFlip");

        /// <summary>
        /// The card category for cards that should not be given out after a table flip.
        /// </summary>
        public CardCategory rerollCards { get; private set; } = CustomCardCategories.instance.CardCategory("rerollCards");

        /// <summary>
        /// The player responsible for the tableflip. Used to add the table flip card to the player.
        /// </summary>
        public Player flippingPlayer;

        /// <summary>
        /// When set to true, a table flip will be initiated at the next end of a player's pick. Initiate the FlipTable() method if you wish to flip before then.
        /// </summary>
        public bool tableFlipped;

        /// <summary>
        /// The table flip card itself. It's automatically given out to the flipping player after a table flip.
        /// </summary>
        public CardInfo tableFlipCard;

        /// <summary>
        /// A list of players to reroll when the next reroll is initiated.
        /// </summary>
        public List<Player> rerollPlayers = new List<Player>();

        /// <summary>
        /// When set to true, a reroll will be initiated at the next end of a player's pick. Initiate the Reroll() method if you wish to reroll before then.
        /// </summary>
        public bool reroll = false;

        /// <summary>
        /// The reroll card itself. It's automatically given out to the rerolling player after a table flip.
        /// </summary>
        public CardInfo rerollCard;

        private System.Random random = new System.Random();

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

        public List<Player> MixUpPlayers = new List<Player>();

        internal IEnumerator IMixUpCards(Player player)
        {
            var triggeringPlayer = player;
            var nonAIPlayers = PlayerManager.instance.players.Where((person) => !ModdingUtils.AIMinion.Extensions.CharacterDataExtension.GetAdditionalData(person.data).isAIMinion).ToArray();
            var originalBoard = nonAIPlayers.ToDictionary((person) => person, (person) => person.data.currentCards.ToArray());

            var newBoard = originalBoard.ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value.ToList());

            for (int i = 0; i < originalBoard[triggeringPlayer].Count() - 1; i++)
            {
                yield return WaitFor.Frames(20);

                if (originalBoard[triggeringPlayer][i].categories.Contains(NoFlip))
                {
                    continue;
                }

                var currentOptions = originalBoard.Where((kvp) => kvp.Value.Length > i).ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value[i]).Where(
                    (kvp) =>
                        !kvp.Value.categories.Contains(NoFlip) &&
                        (
                            ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(triggeringPlayer, kvp.Value) ||
                            kvp.Key == triggeringPlayer
                        ) &&
                        (
                            ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(kvp.Key, originalBoard[triggeringPlayer][i]) ||
                            kvp.Key == triggeringPlayer
                        )
                    ).ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value);

                if (!(currentOptions.Count() > 0))
                {
                    continue;
                }

                var randomSelection = currentOptions.Keys.ToArray()[UnityEngine.Random.Range(0, currentOptions.Keys.ToArray().Count())];

                if (randomSelection = triggeringPlayer)
                {
                    continue;
                }

                var replaced = originalBoard[triggeringPlayer][i];
                var replacement = currentOptions[randomSelection];

                newBoard[triggeringPlayer][i] = replacement;
                newBoard[randomSelection][i] = replaced;

                ModdingUtils.Utils.Cards.instance.ReplaceCard(triggeringPlayer, i, replacement, "", 2f, 2f, true);
                ModdingUtils.Utils.Cards.instance.ReplaceCard(randomSelection, i, replaced, "", 2f, 2f, true);

                UnityEngine.Debug.Log($"[{WillsWackyManagers.ModInitials}][Mix Up][Debugging] Swapped player {triggeringPlayer.playerID}'s {replaced.cardName} with player {randomSelection.playerID}'s {replacement.cardName}.");
            }

            yield break;
        }

        /// <summary>
        /// Initiates any rerolls in the queue.
        /// </summary>
        /// <param name="addCard">Whether a player should be given the Reroll card after they reroll.</param>
        /// <returns></returns>
        public IEnumerator IFlipTableNew(bool addCard = true)
        {
            var rerollers = PlayerManager.instance.players.ToList();
            var rerolled = new List<Player>();
            rerollers.Shuffle();

            int routinesRunning = 0;

            for (int i = 0; i < rerollers.Count(); i++)
            {
                routinesRunning++;
                StartCoroutine(tableFlipRoutine(rerollers[i], false, rerollers[i] != flippingPlayer));
            }

            IEnumerator tableFlipRoutine(Player person, bool addcard, bool removeCard)
            {
                yield return Reroll(person, addcard, removeCard);
                routinesRunning--;
                yield break;
            }

            float startTime = Time.time;

            yield return new WaitUntil(() => {
                return ((routinesRunning <= 0) || (Time.time > startTime + 30f));
            });


            if (flippingPlayer && tableFlipCard && addCard)
            {
                ModdingUtils.Utils.Cards.instance.AddCardToPlayer(flippingPlayer, tableFlipCard, true, "", 2f, 2f, true);
            }

            flippingPlayer = null;
            tableFlipped = false;

            ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            customProperties[SettingCoordinator.TableFlipSyncProperty] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);

            var inSync = false;

            while (!inSync && !PhotonNetwork.OfflineMode)
            {
                inSync = true;
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    if (player.CustomProperties.TryGetValue(SettingCoordinator.TableFlipSyncProperty, out var status))
                    {
                        if (!((bool)status))
                        {
                            inSync = false;
                        }
                    }
                    else
                    {
                        inSync = false;
                    }
                }

                yield return null;
            }

            yield return WaitFor.Frames(10);

            customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            customProperties[SettingCoordinator.TableFlipSyncProperty] = false;
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);

            yield return WaitFor.Frames(10);

            yield return null;
        }

        /// <summary>
        /// Initiates a table flip for all players.
        /// </summary>
        /// <param name="addCard">Whether the flipping player (if one exists) should be given the Table Flip Card (if it exists).</param>
        /// <returns></returns>
        public IEnumerator IFlipTable(bool addCard = true)
        {

                Dictionary<Player, List<Rarity>> cardRarities = new Dictionary<Player, List<Rarity>>();
                Dictionary<Player, List<CardInfo>> playerCards = new Dictionary<Player, List<CardInfo>>();

                foreach (var player in PlayerManager.instance.players)
                {
                    WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Getting card rarities for player {player.playerID}");
                    // Compile List of Rarities
                    cardRarities.Add(player, player.data.currentCards.Select(card => CardRarity(card)).ToList());
                    playerCards.Add(player, player.data.currentCards.ToList());
                    try
                    {
                        ModdingUtils.Utils.Cards.instance.RemoveAllCardsFromPlayer(player);
                    }
                    catch (NullReferenceException)
                    {
                        WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] SOMEBODY NEEDS TO FIX THEIR REMOVECARD FUNCTION.");
                        cardRarities[player].Clear();
                    }
                    WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] {cardRarities[player].Count} card rarities found for player {player.playerID}");
                }
            if (PhotonNetwork.LocalPlayer.IsMasterClient || PhotonNetwork.OfflineMode)
            {
                if (flippingPlayer && (flippingPlayer ? playerCards[flippingPlayer].Count : 0) > 0)
                {
                    // Remove the last card from the flipping player, since it's going to be board wipe
                    cardRarities[flippingPlayer].RemoveAt(cardRarities[flippingPlayer].Count - 1);
                    playerCards[flippingPlayer].RemoveAt(playerCards[flippingPlayer].Count - 1);
                }

                //foreach (var cardSet in playerCards)
                //{
                //    StartCoroutine(IGetCardsForPlayer(cardSet.Key, cardSet.Value.ToArray()));
                //}

                //yield return WaitFor.Frames(5);

                //yield return new WaitUntil(() => !(flippedPlayers.Count > 0));

                yield return WaitFor.Frames(5);

                var allCards = CardManager.cards.Values.ToArray().Where(cardData => cardData.enabled && !(cardData.cardInfo.categories.Contains(NoFlip) || cardData.cardInfo.categories.Contains(CustomCardCategories.instance.CardCategory("AIMinion")) || (cardData.cardInfo.cardName.ToLower() == "shuffle"))).Select(card => card.cardInfo).ToList();
                allCards.Remove(tableFlipCard);

                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] {allCards.Count()} cards are enabled and ready to be swapped out.");

                yield return WaitFor.Frames(20);

                for (int i = 0; i < Mathf.Max(cardRarities.Values.Select(cards => cards.Count()).ToArray()); i++)
                {
                    WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Initiating round {i + 1} of readding cards to players.");
                    foreach (var player in PlayerManager.instance.players)
                    {
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseCategory);
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                        if (CurseManager.instance.HasCurse(player))
                        {
                            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseInteractionCategory);
                            WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID} is available for curse interaction effects");
                        }
                        else if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseInteractionCategory))
                        {
                            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                        }
                        WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Checking player {player.playerID} to see if they are able to have a card added.");
                        if (i < cardRarities[player].Count)
                        {
                            WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID} is able to have a card added.");
                            var rarity = cardRarities[player][i];
                            WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID}'s card was originally {rarity.ToString()}, finding a replacement now.");
                            var cardChoices = allCards.Where(cardInfo => (CardRarity(cardInfo) == rarity) && (ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, cardInfo))).ToArray();
                            WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID} is eligible for {cardChoices.Count()} cards");
                            if (cardChoices.Count() > 0)
                            {
                                var card = RandomCard(cardChoices);
                                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID} is being given {card.cardName}");
                                ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, card, false, "", 2f, 2f, true);
                                //yield return ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, card, 1f);
                            }
                        }
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseCategory);
                        yield return WaitFor.Frames(30);
                    }

                    yield return WaitFor.Frames(30);
                }
                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Finished adding cards to players.");

                if (flippingPlayer && tableFlipCard && addCard)
                {
                    // Add the tableflip card to the player
                    ModdingUtils.Utils.Cards.instance.AddCardToPlayer(flippingPlayer, tableFlipCard, true, "", 2f, 2f, true);
                    //yield return ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(flippingPlayer, tableFlipCard, 2f);
                }
                yield return WaitFor.Frames(20); 
            }

            flippingPlayer = null;
            tableFlipped = false;

            ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            customProperties[SettingCoordinator.TableFlipSyncProperty] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);

            var inSync = false;

            while (!inSync && !PhotonNetwork.OfflineMode)
            {
                inSync = true;
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    if (player.CustomProperties.TryGetValue(SettingCoordinator.TableFlipSyncProperty, out var status))
                    {
                        if (!((bool)status))
                        {
                            inSync = false;
                        }
                    }
                    else
                    {
                        inSync = false;
                    }
                }

                yield return null;
            }

            yield return WaitFor.Frames(20);

            yield return null;

            yield break;
        }

        private List<Player> flippedPlayers = new List<Player>();

        private IEnumerator IGetCardsForPlayer(Player player, CardInfo[] cards)
        {
            flippedPlayers.Add(player);
            List<CardInfo> newCards = new List<CardInfo>();

            foreach (var card in cards)
            {
                Func<CardInfo, Player, Gun, GunAmmo, CharacterData, HealthHandler, Gravity, Block, CharacterStatModifiers, bool> condition = (cardInfo, person, g, ga, d, h, gr, b, s) =>
                {
                    var result = true;

                    if (CardRarity(cardInfo) != CardRarity(card))
                    {
                        result = false;
                    }

                    if (!ModdingUtils.Utils.Cards.instance.CardDoesNotConflictWithCards(cardInfo, newCards.ToArray()))
                    {
                        result = false;
                    }

                    if (cardInfo.categories.Contains(NoFlip))
                    {
                        result = false;
                    }

                    return result;
                };

                // Remove the blacklist for curses if the card is a curse.
                if (CurseManager.instance.IsCurse(card))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseCategory);
                }
                else if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseCategory))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseCategory);
                }

                // Remove the blacklist for curse interaction cards if there is a curse.
                ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                if (newCards.Where(cardInfo => CurseManager.instance.IsCurse(cardInfo)).ToArray().Length > 0 || CurseManager.instance.HasCurse(player))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseInteractionCategory);
                }
                else if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseInteractionCategory))
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                }

                var newCard = GetRandomCardWithCondition(player, condition);
                newCards.Add(newCard);

                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging][Table Flip] Found {newCard.cardName} for Player {player.playerID}.");

                if (newCard.categories.Contains(CurseManager.instance.curseInteractionCategory))
                {
                    ModdingUtils.Utils.Cards.instance.AddCardsToPlayer(player, newCards.ToArray(), false, null, null, null, true);
                    yield return WaitFor.Frames(80);
                    newCards.Clear();
                    newCards = new List<CardInfo>();
                }

                yield return null;
            }

            if (newCards.ToArray().Length > 0)
            {
                ModdingUtils.Utils.Cards.instance.AddCardsToPlayer(player, newCards.ToArray(), false, null, null, null, true);
                yield return WaitFor.Frames(80);
            }

            flippedPlayers.RemoveAll(person => person == player);

            yield break;
        }

        private CardInfo GetRandomCardWithCondition(Player player, Func<CardInfo, Player, Gun, GunAmmo, CharacterData, HealthHandler, Gravity, Block, CharacterStatModifiers, bool> condition, int maxAttempts = 1000)
        {
            return ModdingUtils.Utils.Cards.instance.GetRandomCardWithCondition(player, player.data.weaponHandler.gun, player.data.weaponHandler.gun.GetComponentInChildren<GunAmmo>(), player.data, player.data.healthHandler, player.GetComponent<Gravity>(), player.data.block, player.data.stats, condition, maxAttempts);
        }

        /// <summary>
        /// Initiates any rerolls in the queue.
        /// </summary>
        /// <param name="addCard">Whether a player should be given the Reroll card after they reroll.</param>
        /// <returns></returns>
        public IEnumerator InitiateRerolls(bool addCard = true)
        {
            yield return WaitFor.Frames(40);

            var rerollers = rerollPlayers.Distinct().ToArray();
            var rerolled = new List<Player>();

            int routinesRunning = 0;

            for (int i = 0; i < rerollers.Count(); i++)
            {
                routinesRunning++;
                StartCoroutine(rerollRoutine(rerollers[i]));
            }

            IEnumerator rerollRoutine(Player person)
            {
                yield return Reroll(person);
                routinesRunning--;
                yield break;
            }

            float startTime = Time.time;

            yield return new WaitUntil(() => {
                return ((routinesRunning <= 0) || (Time.time > startTime + 30f));
            });

            rerollPlayers = new List<Player>();
            reroll = false;

            ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            customProperties[SettingCoordinator.RerollSyncProperty] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);

            var inSync = false;

            while (!inSync && !PhotonNetwork.OfflineMode)
            {
                inSync = true;
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    if (player.CustomProperties.TryGetValue(SettingCoordinator.RerollSyncProperty, out var status))
                    {
                        if (!((bool)status))
                        {
                            inSync = false;
                        }
                    }
                    else
                    {
                        inSync = false;
                    }
                }

                yield return null;
            }

            yield return WaitFor.Frames(10);

            customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
            customProperties[SettingCoordinator.RerollSyncProperty] = false;
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties, null, null);

            yield return WaitFor.Frames(10);

            yield return null;
        }

        /// <summary>
        /// <para>Registers an action to be run when a player's cards are rerolled.</para>
        /// </summary>
        /// <param name="rerollAction"><para>An action run when a player's cards are rerolled. The input parameters are the player and their original cards.</para>
        /// <para>The action is run after all cards have been removed from the player, but before the new cards have been added.</para></param>
        public void RegisterRerollAction(Action<Player, CardInfo[]> rerollAction)
        {
            playerRerolledActions.Add(rerollAction);
            //UnityEngine.Debug.Log("Reroll Action registered.");
        }

        /// <summary>
        /// <para>An action run when a player's cards are rerolled. The input parameters are the player and their original cards.</para>
        /// <para>The action is run after all cards have been removed from the player.</para>
        /// </summary>
        private List<Action<Player, CardInfo[]>> playerRerolledActions = new List<Action<Player, CardInfo[]>>();

        /// <summary>
        /// Rerolls any cards the specified player has.
        /// </summary>
        /// <param name="player">The player to reroll.</param>
        /// <param name="addCard">If true, the reroll card will be added to the player, if it exists.</param>
        /// <param name="noRemove">If true, the last card of the player will not be removed as if it were the reroll card.</param>
        /// <returns></returns>
        public IEnumerator Reroll(Player player, bool addCard = true, bool noRemove = false, CardInfo cardToGive = null)
        {
            if (player && (player ? player.data.currentCards.Count : 0) > 0)
            {
                List<CardInfo> originalCards = new List<CardInfo>();
                List<Rarity> cardRarities = new List<Rarity>();

                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Getting card rarities for player {player.playerID}");
                cardRarities = player.data.currentCards.Select(card => 
                    { 
                        return CardRarity(card); 
                    }).ToList();
                originalCards = player.data.currentCards.ToList();
                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] {cardRarities.Count} card rarities found for player {player.playerID}");
                try
                {
                    ModdingUtils.Utils.Cards.instance.RemoveAllCardsFromPlayer(player);
                }
                catch (NullReferenceException)
                {
                    WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] SOMEBODY NEEDS TO FIX THEIR REMOVECARD FUNCTION.");
                    cardRarities.Clear();
                }

                if (cardRarities.Count > 0 && !noRemove)
                {
                    // Remove the last card from the rerolling player, since it's going to be rerolling
                    cardRarities.RemoveAt(cardRarities.Count - 1); 
                }

                List<CardInfo> allCards = CardManager.cards.Values.ToArray().Where(cardData => cardData.enabled && !(cardData.cardInfo.categories.Contains(NoFlip) || cardData.cardInfo.categories.Contains(CustomCardCategories.instance.CardCategory("AIMinion")) || (cardData.cardInfo.cardName.ToLower() == "shuffle"))).Select(card => card.cardInfo).ToList();

                List<CardInfo> hiddenCards = new List<CardInfo>();

                try
                {
                    hiddenCards = (List<CardInfo>)ModdingUtils.Utils.Cards.instance.GetFieldValue("hiddenCards");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("Error fetching hidden cards from modding utils.");
                    UnityEngine.Debug.LogException(e);
                }
                List<CardInfo> nulls = hiddenCards.Where(cardData => cardData.categories.Contains(CustomCardCategories.instance.CardCategory("nullCard"))).ToList();

                if (nulls.Count() > 0)
                {
                    allCards.AddRange(nulls);
                }

                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] {allCards.Count()} cards are enabled and ready to be swapped out.");

                yield return WaitFor.Frames(20);


                //ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseCategory);
                //ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CustomCardCategories.instance.CardCategory("nullCard"));

                for (int i = 0; i < cardRarities.Count(); i++)
                {
                    try
                    {
                        var rarity = cardRarities[i];
                        var originalCard = originalCards[i];

                        if (rarity.isCurse)
                        {
                            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseCategory);
                        }

                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                        if (CurseManager.instance.HasCurse(player))
                        {
                            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseInteractionCategory);
                            WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID} is available for curse interaction effects");
                        }
                        else if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseInteractionCategory))
                        {
                            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                        }

                        WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Checking player {player.playerID} to see if they are able to have a card added.");
                        WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID} is able to have a card added.");
                        WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID}'s card was originally {rarity.ToString()}, finding a replacement now.");
                        var cardChoices = allCards.Where(cardInfo => (CardRarity(cardInfo) == rarity) && (ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, cardInfo))).ToArray();
                        WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID} is eligible for {cardChoices.Count()} cards");
                        if (cardChoices.Count() > 0)
                        {
                            var card = RandomCard(cardChoices);
                            WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Player {player.playerID} is being given {card.cardName}");
                            ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, card, false, "", 2f, 2f, true);
                            //ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, card);
                        }
                        if (rarity.isCurse)
                        {
                            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseCategory);
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                    yield return WaitFor.Frames(40);
                }
                //ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseCategory);
                //ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CustomCardCategories.instance.CardCategory("nullCard"));

                foreach (var rerollAction in playerRerolledActions)
                {
                    //UnityEngine.Debug.Log("Getting Ready to run a reroll action.");
                    try
                    {
                        //UnityEngine.Debug.Log("Trying to run reroll action.");
                        rerollAction(player, originalCards.ToArray());
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.Log($"Exception thrown by reroll action code.");
                        UnityEngine.Debug.LogException(e);
                    }
                }

                WillsWackyManagers.instance.DebugLog($"[WWM][Debugging] Finished adding cards.");

                if (addCard && (rerollCard || cardToGive))
                {
                    // Add the Reroll card to the player
                    try
                    {
                        ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, cardToGive ? cardToGive : rerollCard, true, "", 2f, 2f, true);
                    }catch(Exception e) { UnityEngine.Debug.LogException(e); }
                    //ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, rerollCard);
                }
                yield return WaitFor.Frames(40);
            }

            yield return null;
        }

        private class Rarity
        {
            public CardInfo.Rarity rarity;
            public bool isNull = false;
            public bool isCurse = false;

            public override int GetHashCode()
            {
                var rarityHash = rarity.GetHashCode();
                var nullHash = isNull.GetHashCode();
                var curseHash = isCurse.GetHashCode();

                string hash = rarityHash.ToString() + curseHash.ToString() + nullHash.ToString();

                return hash.GetHashCode();
            }

            public override bool Equals(object obj) => this.Equals(obj as Rarity);

            public bool Equals(Rarity rarity)
            {
                if (rarity is null)
                {
                    return false;
                }

                if (System.Object.ReferenceEquals(this, rarity))
                {
                    return true;
                }

                if (this.GetType() != rarity.GetType())
                {
                    return false;
                }

                return ((this.rarity == rarity.rarity) && (this.isCurse == rarity.isCurse) && (this.isNull == rarity.isNull));
            }

            public static bool operator ==(Rarity r1, Rarity r2)
            {
                if (r1 is null)
                {
                    if (r2 is null)
                    {
                        return true;
                    }
                    return false;
                }

                return r1.Equals(r2);
            }

            public static bool operator !=(Rarity r1, Rarity r2) => !(r1 == r2);

            public override string ToString()
            {
                return $"{this.rarity.ToString()}{(this.isCurse ? " curse" : "")}{(this.isNull ? " null" : "")}";
            }
        }

        private string RarityName(Rarity rarity)
        {
            return $"{rarity.rarity.ToString()}{(rarity.isCurse ? " curse" : "")}{(rarity.isNull ? " null" : "")}";
        }

        private Rarity CardRarity(CardInfo cardInfo)
        {
            Rarity rarity = new Rarity();
            if (cardInfo.categories.Contains(CustomCardCategories.instance.CardCategory("nullCard")))
            {
                rarity.isNull = true;
            }
            if (cardInfo.categories.Contains(CurseManager.instance.curseCategory))
            {
                rarity.isCurse = true;
            }
            rarity.rarity = cardInfo.rarity;

            return rarity;
        }

        private CardInfo RandomCard(CardInfo[] cards)
        {
            if (!(cards.Count() > 0))
            {
                return null;
            }

            return cards[random.Next(cards.Count())];
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
    }
}
