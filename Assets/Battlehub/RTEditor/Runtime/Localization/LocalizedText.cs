using Battlehub.RTCommon;
using TMPro;
using UnityEngine;

namespace Battlehub
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI m_text;

        private ILocalization m_localization;

        private void Start()
        {
            if(m_text == null)
            {
                m_text = GetComponentInChildren<TextMeshProUGUI>();
            }

            if(m_text != null)
            {
                m_localization = IOC.Resolve<ILocalization>();
                if (m_localization != null)
                {
                    m_text.text = m_localization.GetString(m_text.text, null);
                }
            }
        }       
        
    }

}
