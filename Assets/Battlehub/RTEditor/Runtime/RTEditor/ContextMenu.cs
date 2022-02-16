using UnityEngine;
using Battlehub.UIControls.MenuControl;

namespace Battlehub.RTEditor
{
    public interface IContextMenu
    {
        bool IsOpened
        {
            get;
        }
        void Open(MenuItemInfo[] items);
        void Close();
    }

    public class ContextMenu : MonoBehaviour, IContextMenu
    {
        [SerializeField]
        private Menu m_menu = null;

        [SerializeField]
        private RectTransform m_contextMenuArea = null;

        public bool IsOpened
        {
            get { return m_menu.IsOpened; }
        }

        public void Open(MenuItemInfo[] items)
        {
            Canvas canvas = m_contextMenuArea.GetComponentInParent<Canvas>();
            Vector3 position;
            Vector2 pos = Input.mousePosition;

            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (!RectTransformUtility.RectangleContainsScreenPoint(m_contextMenuArea, pos, cam))
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(m_contextMenuArea, pos, cam, out position))
            {
                m_menu.transform.position = position;
                m_menu.Items = items;
                m_menu.Open();
            }
        }

        public void Close()
        {
            m_menu.Close();
        }
    }
}

