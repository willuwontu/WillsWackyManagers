using System;
using System.Collections;
using System.Collections.Generic;
using UnboundLib.Cards;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace WillsWackyManagers.UnityTools
{
    [Serializable]
    public class NormalCardBuilder : CardBuilder
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
                    customCard.BuildUnityCard(StandardCallback);
                }
                else
                {
                    var card = cardsToRegister[i].GetComponent<CardInfo>();
                    if (card) 
                    {
                        CustomCard.RegisterUnityCard(cardsToRegister[i], this.modInitials, card.cardName, true, StandardCallback);
                    }
                }
            }
        }
    }
}
