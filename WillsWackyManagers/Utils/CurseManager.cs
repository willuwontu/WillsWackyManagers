using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnboundLib;
using UnboundLib.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using UnityEngine;
using System;
using BepInEx.Bootstrap;

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
        public CardCategory curseCategory { get; private set; }  = CustomCardCategories.instance.CardCategory("Curse");

        /// <summary>
        /// The card category for cards that interact with cursed players.
        /// </summary>
        public CardCategory curseInteractionCategory { get; private set; }  = CustomCardCategories.instance.CardCategory("Cursed");

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
            });
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
        /// <param name="player"></param>
        /// <returns>CardInfo for the generated curse.</returns>
        public CardInfo RandomCurse(Player player)
        {
            CheckCurses();

            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.RemoveAll(category => category == curseCategory);
            var availableChoices = activeCurses.Where((cardInfo) => ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, cardInfo)).ToArray();
            ModdingUtils.Extensions.CharacterStatModifiersExtension.GetAdditionalData(player.data.stats).blacklistedCategories.Add(curseCategory);
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

            if (!curse)
            {
                UnityEngine.Debug.Log($"[WWM][Debugging] curse didn't exist, getting one now.");
                curse = curses.ToArray()[random.Next(curses.Count)];
            }

            return curse;
        }

        /// <summary>
        /// Curses a player with a random curse.
        /// </summary>
        /// <param name="player">The player to curse.</param>
        public void CursePlayer(Player player)
        {
            CursePlayer(player, null);
        }

        /// <summary>
        /// Curses a player with a random curse.
        /// </summary>
        /// <param name="player">The player to curse.</param>
        /// <param name="callback">An action to run with the information of the curse.</param>
        public void CursePlayer(Player player, Action<CardInfo> callback)
        {
            var curse = RandomCurse(player);
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
        /// Returns an array containing all curses available.
        /// </summary>
        public CardInfo[] GetRaw()
        {
            return curses.ToArray();
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
            while (HasCurse(player))
            {
                for (int i = 0; i < player.data.currentCards.Count(); i++)
                {
                    var card = player.data.currentCards[i];
                    if (IsCurse(card))
                    {
                        callback?.Invoke(card);
                        ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, i);
                        break;
                    }
                }

                yield return WaitFor.Frames(20);
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
    }
}
