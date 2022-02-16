using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Battlehub.UIControls.TooltipControl
{
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField]
        private RectTransform.Edge m_location = RectTransform.Edge.Right;

        [SerializeField]
        private TooltipUI m_prefab;

        [SerializeField]
        private RectTransform m_target;

        [SerializeField]
        private RectTransform m_layer;

        [TextArea]
        [SerializeField]
        private string m_text = "Tooltip";

        public string Text
        {
            get { return m_text; }
            set
            {
                m_text = string.Join(System.Environment.NewLine, SplitToLines(value, 100));
                if(m_tooltip != null)
                {
                    m_tooltip.Text = m_text;
                }
            }
        }

        private TooltipUI m_tooltip;

        private void Awake()
        {
            if(m_prefab == null)
            {
                m_prefab = Resources.Load<TooltipUI>("TooltipUI");
            }

            if(m_target == null)
            {
                m_target = GetComponent<RectTransform>();
            }

            Text = m_text;
        }

        private void Start()
        {
            if(m_layer == null)
            {
                TooltipLayer layer = FindObjectOfType<TooltipLayer>();
                if(layer != null)
                {
                    m_layer = layer.RectTransform;
                }   
            }
        }

        private void OnDisable()
        {
            if(m_tooltip != null)
            {
                m_tooltip.Hide();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(m_tooltip != null)
            {
                m_tooltip.Show();
            }
            else
            {
                ShowToolTip();
            }
        }

        public void Refresh()
        {
            if(m_tooltip != null)
            {
                DestroyImmediate(m_tooltip.gameObject);
                m_tooltip = null;
                ShowToolTip();
            }
        }

        private void ShowToolTip()
        {
            if (m_prefab != null)
            {
                m_tooltip = Instantiate(m_prefab, m_target);
                m_tooltip.Text = m_text;
                m_tooltip.RectTransform.SetInsetAndSizeFromParentEdge(m_location, -3, 0);

                if (m_layer != null)
                {
                    m_tooltip.transform.SetParent(m_layer, true);
                }

                m_tooltip.Show();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(m_tooltip != null)
            {
                m_tooltip.Hide();
            }
        }


        public void OnPointerDown(PointerEventData eventData)
        {
        }

        private IEnumerable<string> SplitToLines(string stringToSplit, int maximumLineLength)
        {
            var words = stringToSplit.Split(' ').Concat(new[] { "" });
            return
                words
                    .Skip(1)
                    .Aggregate(
                        words.Take(1).ToList(),
                        (a, w) =>
                        {
                            var last = a.Last();
                            while (last.Length > maximumLineLength)
                            {
                                a[a.Count() - 1] = last.Substring(0, maximumLineLength);
                                last = last.Substring(maximumLineLength);
                                a.Add(last);
                            }
                            var test = last + " " + w;
                            if (test.Length > maximumLineLength)
                            {
                                a.Add(w);
                            }
                            else
                            {
                                a[a.Count() - 1] = test;
                            }
                            return a;
                        });
        }

    }
}
