using System;
using System.Collections;
using System.Collections.Generic;
using UnboundLib.Cards;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Photon.Pun;
using UnboundLib;

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
                    try
                    {
                        customCard.block = customCard.gameObject.GetOrAddComponent<Block>();
                        customCard.block.objectsToSpawn = new List<GameObject>();
                        customCard.gun = customCard.gameObject.GetOrAddComponent<Gun>();
                        customCard.gun.objectsToSpawn = new ObjectsToSpawn[0];
                        customCard.gun.projectiles = new ProjectilesToSpawn[0];
                        customCard.statModifiers = customCard.gameObject.GetOrAddComponent<CharacterStatModifiers>();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }

                    try
                    {
                        customCard.BuildUnityCard(StandardCallback);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
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
