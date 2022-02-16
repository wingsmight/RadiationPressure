using Battlehub.RTCommon;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Battlehub.RTEditor
{
    [DefaultExecutionOrder(-90)]
    public class WindowOverlay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private IRTE m_rte;
        private bool m_isPointerOver;

        private void Awake()
        {
            m_rte = IOC.Resolve<IRTE>();
        }

        private void Update()
        {
            if(m_isPointerOver && m_rte.Input.IsAnyKeyDown())
            {
                m_rte.ActivateWindow(null);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_isPointerOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_isPointerOver = false;
        }
    }
}


