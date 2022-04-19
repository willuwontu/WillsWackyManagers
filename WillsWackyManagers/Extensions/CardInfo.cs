using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace WillsWackyManagers.Extensions
{
    // ADD FIELDS TO CARDINFO
    [Serializable]
    public class CardInfoAdditionalData
    {

        public CardInfoAdditionalData()
        {

        }
    }
    public static class CardInfoExtension
    {
        public static readonly ConditionalWeakTable<CardInfo, CardInfoAdditionalData> data =
            new ConditionalWeakTable<CardInfo, CardInfoAdditionalData>();

        public static CardInfoAdditionalData GetAdditionalData(this CardInfo cardInfo)
        {
            return data.GetOrCreateValue(cardInfo);
        }

        public static void AddData(this CardInfo cardInfo, CardInfoAdditionalData value)
        {
            try
            {
                data.Add(cardInfo, value);
            }
            catch (Exception) { }
        }
    }
}
