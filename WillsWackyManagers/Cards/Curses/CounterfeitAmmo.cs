﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnboundLib.Cards;
using WillsWackyManagers.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using UnityEngine;
using WillsWackyManagers.UnityTools;

namespace WillsWackyManagers.Cards.Curses
{
    class CounterfeitAmmo : CustomCard, ICurseCard, IConditionalCard
    {
        private static CardInfo card;
        public CardInfo Card { get => card; set { if (!card) { card = value; } } }
        public bool Condition(Player player, CardInfo card)
        {
            if (card != CounterfeitAmmo.card)
            {
                return true;
            }

            if (!player || !player.data || !player.data.weaponHandler || !player.data.weaponHandler.gun)
            {
                return true;
            }

            if (player.data.weaponHandler.gun.GetComponentInChildren<GunAmmo>().maxAmmo < 3)
            {
                return false;
            }

            return true;
        }
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            gun.ammo = -2;
            cardInfo.categories = new CardCategory[] { CurseManager.instance.curseCategory };
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} Built");
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            gun.bursts = Mathf.Max(gun.bursts -1, 0);
            gun.numberOfProjectiles = Mathf.Max(gun.numberOfProjectiles - 1, 1);
            gunAmmo.reloadTimeAdd += 0.25f / gunAmmo.reloadTimeMultiplier;

            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} added to Player {player.playerID}");
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} removed from Player {player.playerID}");
        }

        protected override string GetTitle()
        {
            return "Counterfeit Ammo";
        }
        protected override string GetDescription()
        {
            return "Should've bought it from a reputable source.";
        }
        protected override GameObject GetCardArt()
        {
            return null;
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Reload Time",
                    amount = "+0.25s",
                    simepleAmount = CardInfoStat.SimpleAmount.aLotOf
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Ammo",
                    amount = "-2",
                    simepleAmount = CardInfoStat.SimpleAmount.lower
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Bullet",
                    amount = "-1",
                    simepleAmount = CardInfoStat.SimpleAmount.slightlyLower
                },
                new CardInfoStat()
                {
                    positive = false,
                    stat = "Burst",
                    amount = "-1",
                    simepleAmount = CardInfoStat.SimpleAmount.slightlyLower
                }
            };
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CurseManager.instance.FallenPurple;
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
