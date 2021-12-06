using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnboundLib.Cards;
using WillsWackyManagers.Utils;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using UnityEngine;
using Photon.Pun;

namespace WillsWackyManagers.Cards
{
    class GenerateRandomCurse : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.categories = new CardCategory[] { CurseManager.instance.curseCategory };
            UnityEngine.Debug.Log($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} Built");
        }
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            UnityEngine.Debug.Log($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} added to Player {player.playerID}");
        }
        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            UnityEngine.Debug.Log($"[{WillsWackyManagers.ModInitials}][Curse] {GetTitle()} removed from Player {player.playerID}");
        }

        protected override string GetTitle()
        {
            return "Randomly Generated Curse";
        }
        protected override string GetDescription()
        {
            return "Randomly generates a curse. If this is still on you, report it as a bug to Willuwontu.";
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

            };
        }
        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.DestructiveRed;
        }
        public override string GetModName()
        {
            return "Curse";
        }
        public override bool GetEnabled()
        {
            return true;
        }
    }

    public sealed class CurseGenerator : MonoBehaviourPun
    {
        public readonly static CurseGenerator instance = new CurseGenerator();
        private static readonly System.Random random = new System.Random();

        public bool generating = false;

        public List<string> people = new List<string> { "King", "Knight", "Fool", "Peasant", "Farmer", "Soldier", "Prince", "Princess", "Queen", "Adventurer", "Priest", "Heretic", "Cultist", "Druid", "Fighter", "Wizard", "Trucker", "Lich", "Salesman", "Streamer", "Developer", "Merchant", "Philanthropist", "Writer", "Scholar", "Poet", "Pirate", "Ninja", "Baby", "Diva", "Viking", "Disco Dancer", "Elf", "Dwarf", "Orc", "Troll", "Dragon", "Rogue", "Hunter", "Assassin", "President", "Bicyclist", "Driver", "Maid", "Butler", "Servant", "Woodsman", "Mage", "Witch", "Gnome", "Halfling", "Astronaut", "Pilot", "Pterodactyl", "Dinosaur", "Sailor", "Spy", "Tinker", "Tailor", "Clocksmith", "Captain", "Lunatic" };
        public List<string> personDescription = new List<string> { "Crazed", "Mad", "Delusional", "Poor", "Lost", "Rich", "Ruined", "Broken", "Enraged", "Blind", "Foolish", "Unfortunate", "Sleeping", "Poisoned", "Drowned", "Forgotten", "Desolate", "Lonely", "Sealed", "Trapped", "Kidnapped", "Vengeful", "Restless", "Insomiac", "Sad", "Crying", "Cursed", "Screeching", "Robbed", "Confused", "Slumbering", "Cruel", "Insolent", "Double-Crossed", "Backstabbed", "Halfwitted", "Absurd" };
        public List<string> items = new List<string> { "Cloak", "Robe", "Shoes", "Cap", "Hat", "Sword", "Pike", "Axe", "Lance", "Turboencabulator", "Rifle", "Pistol", "Belt", "Shirt", "Pants", "Trousers", "Nightgown", "Underwear", "Socks", "Shield", "Chest", "Key", "Coat", "Onlyfans", "Tears", "Bathwater", "Dreams", "Facade", "Castle", "Movie", "Gloves", "Spherical Cow", "Internet", "Chair", "Table", "Plate", "Fork", "Spoon", "Knife", "Dagger", "Potion", "Afro", "Rope", "Handcuffs", "Fuzzy Cuffs", "Brain", "Eyes", "Lemon", "Wafflemaker", "Knife", "Banana", "Tooth" };
        public List<string> itemState = new List<string> { "Shattered", "Broken", "Destroyed", "Salty", "Smelly", "Slippery", "Cursed", "Melted", "Fake", "Useless", "Lost", "Weak", "Flimsy", "Torn", "Slimy", "Scorched", "Frozen", "Tattered", "Cardboard", "Terrible", "Forgotten", "Illusory", "Toppled", "Slow", "Laggy", "Trapped", "Rusted", "Bent", "Confused", "Hollow", "Ruined", "Heavy", "Nasty", "Gross", "Overpriced", "Moldy", "Shoddy", "Rejected", "Crappy", "Splintered", "Fragmented", "Crushed", "Ruptured", "Fractured", "Wrecked", "Putrid", "Rotten", "Decayed", "Foul", "Crumbling" };

        public string GenerateTitle()
        {
            return string.Format("The {0} {1} of the {2} {3}", itemState[random.Next(0, itemState.Count)], items[random.Next(0, items.Count)], personDescription[random.Next(0, personDescription.Count)], people[random.Next(0, people.Count)]);
        }

        private Dictionary<string, Dictionary<CardInfo.Rarity, object>> stats =
            new Dictionary<string, Dictionary<CardInfo.Rarity, object>> {  };

    }

    class BasicStatInfo
    {
        public Dictionary<CardInfo.Rarity, object> rarityValues;
        public string name;
        public Type component;
        public object defaultValue;
        public bool OnAddCard;
        public object minimumValue;

    }

    class RandomCurse_Mono : MonoBehaviour
    {

    }
}
