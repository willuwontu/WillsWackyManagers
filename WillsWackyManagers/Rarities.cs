public static class Rarities
{
    public static CardInfo.Rarity GetRarity(string name)
    {
        return RarityLib.Utils.RarityUtils.GetRarity(name);
    }
    public static CardInfo.Rarity Trinket => RarityLib.Utils.RarityUtils.GetRarity("Trinket");
    public static CardInfo.Rarity Common => CardInfo.Rarity.Common;
    public static CardInfo.Rarity Scarce => RarityLib.Utils.RarityUtils.GetRarity("Scarce");
    public static CardInfo.Rarity Uncommon => CardInfo.Rarity.Uncommon;
    public static CardInfo.Rarity Exotic => RarityLib.Utils.RarityUtils.GetRarity("Exotic");
    public static CardInfo.Rarity Rare => CardInfo.Rarity.Rare;
    public static CardInfo.Rarity Epic => RarityLib.Utils.RarityUtils.GetRarity("Epic");
    public static CardInfo.Rarity Legendary => RarityLib.Utils.RarityUtils.GetRarity("Legendary");
    public static CardInfo.Rarity Mythical => RarityLib.Utils.RarityUtils.GetRarity("Mythical");
    public static CardInfo.Rarity Divine => RarityLib.Utils.RarityUtils.GetRarity("Divine");
}