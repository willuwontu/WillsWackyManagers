﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnboundLib.Cards;
using WillsWackyManagers.MonoBehaviours;
using WillsWackyManagers.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using UnityEngine;

namespace WillsWackyManagers.Cards.Curses
{
    class Template : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.categories = new CardCategory[] { CurseManager.instance.curseCategory };
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} Built");
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} added to Player {player.playerID}");
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} removed from Player {player.playerID}");
        }

        protected override string GetTitle()
        {
            return "Template Curse";
        }
        protected override string GetDescription()
        {
            return "Somebody forgot to change this stuff.";
        }
        protected override GameObject GetCardArt()
        {
            return null;
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Common;
        }
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Effect",
                    amount = "No",
                    simepleAmount = CardInfoStat.SimpleAmount.notAssigned
                }
            };
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.EvilPurple;
        }
        public override string GetModName()
        {
            return WillsWackyManagers.CurseInitials;
        }
        public override bool GetEnabled()
        {
            return true;
        }
    }
}
