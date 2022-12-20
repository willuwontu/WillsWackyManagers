using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WillsWackyManagers.MonoBehaviours
{
    [Serializable]
    public class MechCardVisuals : CardVisuals
    {
        private void Start()
        {
            this.card = this.GetComponentInParent<CardInfo>();
            this.gameObject.transform.root.GetComponentInChildren<Canvas>().sortingLayerName = "MostFront";
            if (card.cardArt)
            {
                GameObject art = GameObject.Instantiate<GameObject>(card.cardArt, artContainer.transform.position, artContainer.transform.rotation, artContainer.transform);
                art.transform.localPosition = Vector3.zero;
                art.transform.SetAsFirstSibling();
                art.transform.localScale = Vector3.one;
            }
            try
            {
                typeof(CardVisuals).GetField("group", BindingFlags.Default | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, gridGroup);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Grid Group throwing errors.");
            }
            try
            {
                this.defaultColor = CardChoice.instance.GetCardColor(card.colorTheme);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("default color");
            }
            try
            {
                typeof(CardVisuals).GetField("selectedColor", BindingFlags.Default | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, CardChoice.instance.GetCardColor(card.colorTheme));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Selected Color");
            }

            try
            {
                typeof(CardVisuals).GetField("part", BindingFlags.Default | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, fakePart);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("particles");
            }
            try
            {
                typeof(CardVisuals).GetField("shake", BindingFlags.Default | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, this.GetComponent<ScaleShake>());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Shake throwing errors.");
            }
            try
            {
                typeof(CardVisuals).GetField("cardAnims", BindingFlags.Default | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, this.GetComponentsInChildren<CardAnimation>());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("card anims throwing errors.");
            }
            try
            {
                this.isSelected = !this.firstValueToSet;
                this.ChangeSelected(this.firstValueToSet);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("selection throwing errors.");
            }

            if (card is MechCardInfo mech)
            {
                if (mech.accessory != null)
                {
                    accessory = UnityEngine.GameObject.Instantiate(mech.accessory.transform, this.accessoryLocation);
                }
                else
                {
                    if (defaultAccessory != null)
                    {
                        accessory = UnityEngine.GameObject.Instantiate(defaultAccessory.transform, this.accessoryLocation);

                    }
                }
            }
            else
            {
                if (defaultAccessory != null)
                {
                    accessory = UnityEngine.GameObject.Instantiate(defaultAccessory.transform, this.accessoryLocation);

                }
            }
        }

        public GameObject artContainer;
        public GeneralParticleSystem fakePart;
        public CanvasGroup gridGroup;
        [HideInInspector]
        public CardInfo card;

        public Transform accessoryLocation;
        public GameObject defaultAccessory;
        public TextMeshProUGUI modNameText;

        private Transform accessory;
    }
}
