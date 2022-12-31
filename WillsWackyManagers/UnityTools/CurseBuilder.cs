using System;
using System.Collections;
using System.Collections.Generic;
using UnboundLib.Cards;
using UnityEngine;
using Sirenix.OdinInspector;
using WillsWackyManagers.Utils;

namespace WillsWackyManagers.UnityTools
{
    [Serializable]
    public class CurseBuilder : CardBuilder
    {
        [ShowInInspector]
        public GameObject[] cardsToRegister;

        public override void BuildCards()
        {
            for (int i = 0; i < cardsToRegister.Length; i++)
            {
                var customCard = cardsToRegister[i].GetComponent<CustomCard>();

                if (customCard)
                {
                    customCard.BuildUnityCard(card => { StandardCallback(card); CurseManager.instance.RegisterCurse(card); });
                }
            }
        }
    }
}
