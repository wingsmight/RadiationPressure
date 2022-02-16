using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public abstract class UIButton : MonoBehaviour
{
    private const string CLICK_SOUND_PATH = "Sounds/ButtonClickSound";


    [SerializeField] private bool hasSound;


    protected Button button;
    protected AudioClip clickSound;


    protected virtual void Awake()
    {
        clickSound = (AudioClip)Resources.Load(CLICK_SOUND_PATH);

        button = GetComponent<Button>();
        button.onClick.AddListener(ActButton);
    }
    protected virtual void OnDestroy()
    {
        button.onClick.RemoveListener(ActButton);
    }

    protected abstract void OnClick();

    private void ActButton()
    {
        if (clickSound != null && hasSound)
        {
            AudioSource.PlayClipAtPoint(clickSound, new Vector3(0, 0, 0));
        }

        OnClick();
    }


    public Button Button => button;
}
