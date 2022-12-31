using System;
using System.Collections;
using System.Collections.Generic;
using UnboundLib.Cards;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

namespace WillsWackyManagers.UnityTools
{
    [Serializable]
    public abstract class CardBuilder : SerializedMonoBehaviour
    {
        public string modInitials = "WWC";

        public abstract void BuildCards();

        public virtual void StandardCallback(CardInfo card)
        {
            if (card is ISaveableCard saveable)
            {
                saveable.Card = card;
            }
            if (card is IConditionalCard conditional)
            {
                ModdingUtils.Utils.Cards.instance.AddCardValidationFunction(conditional.Condition);
            }
        }
    }
}
