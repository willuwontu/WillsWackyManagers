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
    class MechCardInfoDisplayTransfer : MonoBehaviour
    {
        private void OnTransformChildrenChanged()
        {
            foreach (Transform child in this.transform)
            {
                if (child.gameObject == this.trapEffectText)
                {
                    real.text = trap.text;
                    UnityEngine.GameObject.Destroy(child);
                }
            }
        }

        public GameObject trapEffectText;
        public TextMeshProUGUI trap;
        public GameObject realEffectText;
        public TextMeshProUGUI real;
    }
}
