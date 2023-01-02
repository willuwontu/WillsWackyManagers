using System;
using System.Collections;
using System.Collections.Generic;
using UnboundLib.Cards;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using WillsWackyManagers.Utils;
using CardChoiceSpawnUniqueCardPatch;

namespace WillsWackyManagers.UnityTools
{
    [Serializable]
    public abstract class CardBuilder : SerializedMonoBehaviour
    {
        public string modInitials = "WWC";

        public abstract void BuildCards();

        public virtual void StandardCallback(CardInfo card)
        {
            if (card.GetComponent<ISaveableCard>() != null && card.GetComponent<ISaveableCard>() is ISaveableCard saveable)
            {
                saveable.Card = card;
            }
            if (card.GetComponent<IConditionalCard>() != null && card.GetComponent<IConditionalCard>() is IConditionalCard conditional)
            {
                ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(conditional.Condition);
            }
            if (card.GetComponent<ICurseCard>() != null)
            {
                CurseManager.instance.RegisterCurse(card);
            }
        }
    }
}
