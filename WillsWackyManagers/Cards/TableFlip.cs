using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnboundLib.Cards;
using WillsWackyManagers.Utils;
using WillsWackyManagers.MonoBehaviours;
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
            cardInfo.categories = new CardCategory[] { RerollManager.instance.NoFlip, RerollManager.instance.rerollCards, tableFlipCardCategory, CustomCardCategories.instance.CardCategory("CardManipulation") };
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Card] {GetTitle()} Built");
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            RerollManager.instance.flippingPlayer = player;
            RerollManager.instance.tableFlipped = true;
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Card] {GetTitle()} Added to Player {player.playerID}");
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Card] {GetTitle()} removed from Player {player.playerID}");
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
            GameObject art;

            try
            {
                art = WillsWackyManagers.instance.WWWMAssets.LoadAsset<GameObject>("C_TableFlip");
                var randColor = art.transform.Find("Foreground/Character").gameObject.AddComponent<RandomGraphicColorOnAwake>();
                randColor.colorA = new Color32(200, 200, 200, 255);
                randColor.colorB = new Color32(75, 75, 75, 255);
                randColor.updateChildren = true;

                var cards = art.transform.Find("Foreground/Cards");

                foreach (Transform child in cards)
                {
                    child.Find("Card Holder").gameObject.AddComponent<GetRandomCardVisualsOnEnable>();
                }
            }
            catch
            {
                art = null;
            }

            return art;
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
