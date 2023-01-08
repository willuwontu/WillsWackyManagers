using System;
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
using WillsWackyManagers.UnityTools;
using RarityLib.Utils;
using static UnityEngine.Experimental.UIElements.UxmlAttributeDescription;

namespace WillsWackyManagers.Cards.Curses
{
    class SeepingCorruption : CustomCard, ICurseCard
    {
        private static Dictionary<Player, float> rarityAdjustment = new Dictionary<Player, float>();
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.allowMultiple = true;
            cardInfo.categories = new CardCategory[] { CurseManager.instance.curseCategory };
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} Built");
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (CurseManager.instance.CanPlayerDrawCurses(player))
            {
                if (rarityAdjustment.TryGetValue(player, out float value))
                {
                    rarityAdjustment[player] = value + 1f;
                }
                else
                {
                    rarityAdjustment.Add(player, 1f);
                }

                if (player.data.view.IsMine || Photon.Pun.PhotonNetwork.OfflineMode)
                {
                    foreach (var curse in CurseManager.instance.Curses)
                    {
                        RarityUtils.AjustCardRarityModifier(curse, 1, 0);
                    }
                }
            }
            else
            {
                CurseManager.instance.PlayerCanDrawCurses(player, true);
            }
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} added to Player {player.playerID}");
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            CurseManager.instance.PlayerCanDrawCurses(player, false);

            foreach (var kvp in rarityAdjustment)
            {
                if (kvp.Key.data.view.IsMine || Photon.Pun.PhotonNetwork.OfflineMode)
                {
                    foreach (var curse in CurseManager.instance.Curses)
                    {
                        RarityUtils.AjustCardRarityModifier(curse, -1*kvp.Value, 0);
                    }
                }
            }
            rarityAdjustment.Clear();
            
            WillsWackyManagers.instance.DebugLog($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} removed from Player {player.playerID}");
        }

        protected override string GetTitle()
        {
            return "Seeping Corruption";
        }
        protected override string GetDescription()
        {
            return "The wards were ever tested by the darkness, tendrils of shadows looking for a crack to seep in through and pollute their victims.";
        }
        protected override GameObject GetCardArt()
        {
            return null;
        }
        protected override CardInfo.Rarity GetRarity()
        {
            return Rarities.Legendary;
        }
        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
            };
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CurseManager.instance.CurseGray;
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
