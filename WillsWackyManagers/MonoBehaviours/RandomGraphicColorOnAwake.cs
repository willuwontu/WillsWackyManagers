using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WillsWackyManagers.MonoBehaviours
{
    public class RandomGraphicColorOnAwake : MonoBehaviour
    {
        public Color colorA;
        public Color colorB;
        public bool updateChildren = true;
        public Graphic[] graphicsToUpdate;

        [HideInInspector]
        public Color randomColor;
        [HideInInspector]
        public bool colorReset;

        private void OnEnable()
        {
            List<Graphic> graphics = new List<Graphic>();

            if (this.GetComponent<Graphic>())
            {
                graphics.Add(this.GetComponent<Graphic>());
            }

            if (updateChildren)
            {
                for (int i = 0; i < this.transform.childCount; i++)
                {
                    if (transform.GetChild(i).gameObject.GetComponent<Graphic>())
                    {
                        graphics.Add(transform.GetChild(i).gameObject.GetComponent<Graphic>());
                    }
                }
            }

            graphicsToUpdate = graphics.ToArray();

            randomColor = GetRandomColor();
            colorReset = true;
        }

        public virtual Color GetRandomColor()
        {
            var color = new Color(UnityEngine.Random.Range(colorA.r, colorB.r), UnityEngine.Random.Range(colorA.g, colorB.g), UnityEngine.Random.Range(colorA.b, colorB.b), UnityEngine.Random.Range(colorA.a, colorB.a));

            foreach (var graphic in graphicsToUpdate)
            {
                graphic.color = color;
            }

            return color;
        }
    }
}
