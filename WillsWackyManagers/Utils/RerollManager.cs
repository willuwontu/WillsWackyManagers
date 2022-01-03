﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnboundLib;
using UnboundLib.Utils;
using ModdingUtils.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;

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
        public bool reroll;

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
        /// Initiates a table flip for all players.
        /// </summary>
        /// <param name="addCard">Whether the flipping player (if one exists) should be given the Table Flip Card (if it exists).</param>
        /// <returns></returns>
        public IEnumerator FlipTable(bool addCard = true)
        {
            Dictionary<Player, List<Rarity>> cardRarities = new Dictionary<Player, List<Rarity>>();

            foreach (var player in PlayerManager.instance.players)
            {
                UnityEngine.Debug.Log($"[WWM][Debugging] Getting card rarities for player {player.playerID}");
                // Compile List of Rarities
                cardRarities.Add(player, player.data.currentCards.Select(card => CardRarity(card)).ToList());
                try
                {
                    ModdingUtils.Utils.Cards.instance.RemoveAllCardsFromPlayer(player);
                }
                catch (NullReferenceException)
                {
                    UnityEngine.Debug.Log($"[WWM][Debugging] SOMEBODY NEEDS TO FIX THEIR REMOVECARD FUNCTION.");
                    cardRarities[player].Clear();
                }
                UnityEngine.Debug.Log($"[WWM][Debugging] {cardRarities[player].Count} card rarities found for player {player.playerID}");
            }

            if (flippingPlayer && (flippingPlayer ? flippingPlayer.data.currentCards.Count : 0) > 0)
            {
                // Remove the last card from the flipping player, since it's going to be board wipe
                cardRarities[flippingPlayer].RemoveAt(cardRarities[flippingPlayer].Count - 1);
            }

            var allCards = CardManager.cards.Values.ToArray().Where(cardData => cardData.enabled && !(cardData.cardInfo.categories.Contains(NoFlip) || (cardData.cardInfo.cardName.ToLower() == "shuffle"))).Select(card => card.cardInfo).ToList();
            allCards.Remove(tableFlipCard);

            UnityEngine.Debug.Log($"[WWM][Debugging] {allCards.Count()} cards are enabled and ready to be swapped out.");

            yield return WaitFor.Frames(20);

            for (int i = 0; i < Mathf.Max(cardRarities.Values.Select(cards => cards.Count()).ToArray()); i++)
            {
                UnityEngine.Debug.Log($"[WWM][Debugging] Initiating round {i+1} of readding cards to players.");
                foreach (var player in PlayerManager.instance.players)
                {
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseCategory);
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                    if (CurseManager.instance.HasCurse(player))
                    {
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseInteractionCategory);
                        UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID} is available for curse interaction effects");
                    }
                    else if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseInteractionCategory))
                    {
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                    }
                    UnityEngine.Debug.Log($"[WWM][Debugging] Checking player {player.playerID} to see if they are able to have a card added.");
                    if (i < cardRarities[player].Count)
                    {
                        UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID} is able to have a card added.");
                        var rarity = cardRarities[player][i];
                        UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID}'s card was originally {RarityName(rarity)}, finding a replacement now.");
                        var cardChoices = allCards.Where(cardInfo => (CardRarity(cardInfo) == rarity) && (ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, cardInfo))).ToArray();
                        UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID} is eligible for {cardChoices.Count()} cards");
                        if (cardChoices.Count() > 0)
                        {
                            var card = RandomCard(cardChoices);
                            UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID} is being given {card.cardName}");
                            ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, card, false, "", 2f, 2f, true);
                            ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, card);
                        }
                    }
                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseCategory);
                    yield return WaitFor.Frames(40);
                }

                yield return WaitFor.Frames(40); 
            }
            UnityEngine.Debug.Log($"[WWM][Debugging] Finished adding cards to players.");

            if (flippingPlayer && tableFlipCard && addCard)
            {
                // Add the tableflip card to the player
                ModdingUtils.Utils.Cards.instance.AddCardToPlayer(flippingPlayer, tableFlipCard, true, "", 2f, 2f, true);
                ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(flippingPlayer, tableFlipCard);
            }
            yield return WaitFor.Frames(40);

            flippingPlayer = null;
            tableFlipped = false;
            yield return null;
        }

        /// <summary>
        /// Initiates any rerolls in the queue.
        /// </summary>
        /// <param name="addCard">Whether a player should be given the Reroll card after they reroll.</param>
        /// <returns></returns>
        public IEnumerator InitiateRerolls(bool addCard = true)
        {
            var rerollers = rerollPlayers.ToArray();
            var rerolled = new List<Player>();
            foreach (var reroller in rerollers)
            {
                if (!rerolled.Contains(reroller))
                {
                    rerolled.Add(reroller);
                    yield return Reroll(reroller);
                }
            }
            
            rerollPlayers = new List<Player>();
            reroll = false;
            yield return null;
        }

        /// <summary>
        /// Rerolls any cards the specified player has.
        /// </summary>
        /// <param name="player">The player to reroll.</param>
        /// <param name="addCard">If true, the reroll card will be added to the player, if it exists.</param>
        /// <returns></returns>
        public IEnumerator Reroll(Player player, bool addCard = true)
        {
            if (player && (player ? player.data.currentCards.Count : 0) > 0)
            {
                List<Rarity> cardRarities = new List<Rarity>();

                UnityEngine.Debug.Log($"[WWM][Debugging] Getting card rarities for player {player.playerID}");
                cardRarities = player.data.currentCards.Select(card => CardRarity(card)).ToList();
                UnityEngine.Debug.Log($"[WWM][Debugging] {cardRarities.Count} card rarities found for player {player.playerID}");
                try
                {
                    ModdingUtils.Utils.Cards.instance.RemoveAllCardsFromPlayer(player);
                }
                catch (NullReferenceException)
                {
                    UnityEngine.Debug.Log($"[WWM][Debugging] SOMEBODY NEEDS TO FIX THEIR REMOVECARD FUNCTION.");
                    cardRarities.Clear();
                }

                if (cardRarities.Count > 0)
                {
                    // Remove the last card from the rerolling player, since it's going to be rerolling
                    cardRarities.RemoveAt(cardRarities.Count - 1); 
                }

                var allCards = CardManager.cards.Values.ToArray().Where(cardData => cardData.enabled && !(cardData.cardInfo.categories.Contains(NoFlip) || (cardData.cardInfo.cardName.ToLower() == "shuffle"))).Select(card => card.cardInfo).ToList();

                UnityEngine.Debug.Log($"[WWM][Debugging] {allCards.Count()} cards are enabled and ready to be swapped out.");

                yield return WaitFor.Frames(20);


                ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseCategory);

                foreach (var rarity in cardRarities)
                {

                    ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                    if (CurseManager.instance.HasCurse(player))
                    {
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == CurseManager.instance.curseInteractionCategory);
                        UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID} is available for curse interaction effects");
                    }
                    else if (!ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Contains(CurseManager.instance.curseInteractionCategory))
                    {
                        ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseInteractionCategory);
                    }

                    UnityEngine.Debug.Log($"[WWM][Debugging] Checking player {player.playerID} to see if they are able to have a card added.");
                    UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID} is able to have a card added.");
                    UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID}'s card was originally {RarityName(rarity)}, finding a replacement now.");
                    var cardChoices = allCards.Where(cardInfo => (CardRarity(cardInfo) == rarity) && (ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, cardInfo))).ToArray();
                    UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID} is eligible for {cardChoices.Count()} cards");
                    if (cardChoices.Count() > 0)
                    {
                        var card = RandomCard(cardChoices);
                        UnityEngine.Debug.Log($"[WWM][Debugging] Player {player.playerID} is being given {card.cardName}");
                        ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, card, false, "", 2f, 2f, true);
                        ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, card);
                    }

                    yield return WaitFor.Frames(40);
                }
                ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(CurseManager.instance.curseCategory);
                UnityEngine.Debug.Log($"[WWM][Debugging] Finished adding cards.");

                if (rerollCard && addCard)
                {
                    // Add the Reroll card to the player
                    ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, rerollCard, true, "", 2f, 2f, true);
                    ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, rerollCard);
                }
                yield return WaitFor.Frames(40);
            }

            yield return null;
        }

        private enum Rarity
        {
            Rare,
            Uncommon,
            Common,
            CommonCurse,
            UncommonCurse,
            RareCurse
        }

        private string RarityName(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare:
                    return "Rare";
                case Rarity.Uncommon:
                    return "Uncommon";
                case Rarity.Common:
                    return "Common";
                case Rarity.RareCurse:
                    return "Rare Curse";
                case Rarity.UncommonCurse:
                    return "Uncommon Curse";
                case Rarity.CommonCurse:
                    return "Common Curse";
                default:
                    return "No Rarity?";
            }
        }
        private Rarity CardRarity(CardInfo cardInfo)
        {
            Rarity rarity;

            if (cardInfo.categories.Contains(CurseManager.instance.curseCategory))
            {
                switch (cardInfo.rarity)
                {
                    case CardInfo.Rarity.Rare:
                        rarity = Rarity.RareCurse;
                        break;
                    case CardInfo.Rarity.Uncommon:
                        rarity = Rarity.UncommonCurse;
                        break;
                    default:
                        rarity = Rarity.CommonCurse;
                        break;
                }
            }
            else
            {
                switch (cardInfo.rarity)
                {
                    case CardInfo.Rarity.Rare:
                        rarity = Rarity.Rare;
                        break;
                    case CardInfo.Rarity.Uncommon:
                        rarity = Rarity.Uncommon;
                        break;
                    default:
                        rarity = Rarity.Common;
                        break;
                }
            }

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
