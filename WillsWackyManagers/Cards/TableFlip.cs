﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnboundLib.Cards;
using WillsWackyManagers.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using ModdingUtils.Extensions;
using UnityEngine;

namespace WillsWackyManagers.Cards
{
    class TableFlip : CustomCard
    {
        internal static CardCategory tableFlipCardCategory = CustomCardCategories.instance.CardCategory("Table Flip Card");
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.GetAdditionalData().canBeReassigned = false;
            cardInfo.categories = new CardCategory[] { RerollManager.instance.NoFlip, tableFlipCardCategory };
            UnityEngine.Debug.Log($"[{WillsWackyManagers.ModInitials}][Card] {GetTitle()} Built");
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            RerollManager.instance.flippingPlayer = player;
            RerollManager.instance.tableFlipped = true;
            UnityEngine.Debug.Log($"[{WillsWackyManagers.ModInitials}][Card] {GetTitle()} Added to Player {player.playerID}");
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            UnityEngine.Debug.Log($"[{WillsWackyManagers.ModInitials}][Card] {GetTitle()} removed from Player {player.playerID}");
        }

        protected override string GetTitle()
        {
            return "Table Flip";
        }
        protected override string GetDescription()
        {
            return "When rage quitting isn't enough. Removes everyone's cards and replaces it with a random one of the same rarity.";
        }
        protected override GameObject GetCardArt()
        {
            return null;
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return WillsWackyManagers.secondHalfTableFlipConfig.Value ? CardInfo.Rarity.Uncommon : CardInfo.Rarity.Rare;
        }
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
            };
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.EvilPurple;
        }
        public override string GetModName()
        {
            return WillsWackyManagers.ModInitials;
        }
        public override bool GetEnabled()
        {
            return true;
        }
    }
}
