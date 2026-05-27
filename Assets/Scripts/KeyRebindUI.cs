using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Real-time key rebinding with keyboard sprites (KeysConfigNormal / KeysConfigPresss).
// Pattern: Observer — each change is saved to PlayerPrefs immediately; PlayerController
// picks it up on the next frame without a restart because it calls KeyRebindUI.GetKey() in Update.
public class KeyRebindUI : MonoBehaviour
{
    [System.Serializable]
    public class BindingEntry
    {
        public string   actionId;
        public KeyCode  defaultKey;
        public Button   rebindButton;
        public TMP_Text keyLabel;
        [HideInInspector] public KeyCode currentKey;
    }

    [SerializeField] private BindingEntry[] entries;

    [Header("Key sprites (Assets/UI/Sprites/KeysConfig/)")]
    [SerializeField] private Sprite keyNormalSprite;    // KeysConfigNormal.png
    [SerializeField] private Sprite keyListeningSprite; // KeysConfigPresss.png

    [Header("Listening highlight colour (fallback when no sprites are assigned)")]
    [SerializeField] private Color listeningColor = new Color(1f, 0.85f, 0.4f, 1f);

    private BindingEntry listeningEntry;
    private Image        listeningButtonImage;

    private bool HasSprites => keyNormalSprite != null && keyListeningSprite != null;

    void Start()
    {
        foreach (var e in entries)
        {
            string saved    = PlayerPrefs.GetString(EldoriaPrefsKeys.KeyPrefix + e.actionId, e.defaultKey.ToString());
            e.currentKey    = (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
            e.keyLabel.text = FriendlyName(e.currentKey);

            InitButtonSprites(e.rebindButton);

            var captured = e;
            e.rebindButton.onClick.AddListener(() => BeginListening(captured));
        }
    }

    // Configures the button to use SpriteSwap with the keyboard images.
    private void InitButtonSprites(Button btn)
    {
        if (!HasSprites) return;

        btn.image.sprite = keyNormalSprite;
        btn.transition   = Selectable.Transition.SpriteSwap;
        btn.spriteState  = new SpriteState
        {
            highlightedSprite = keyListeningSprite,
            pressedSprite     = keyListeningSprite,
            selectedSprite    = keyNormalSprite,
            disabledSprite    = keyNormalSprite
        };
    }

    private void BeginListening(BindingEntry entry)
    {
        if (listeningEntry != null)
            CancelListening();

        listeningEntry       = entry;
        listeningButtonImage = entry.rebindButton.image;

        if (HasSprites)
            listeningButtonImage.sprite = keyListeningSprite;
        else
            listeningButtonImage.color = listeningColor;

        entry.keyLabel.text = "< presiona tecla >";
    }

    private void CancelListening()
    {
        if (listeningEntry == null) return;
        listeningEntry.keyLabel.text = FriendlyName(listeningEntry.currentKey);
        ResetButtonVisual(listeningButtonImage);
        listeningEntry       = null;
        listeningButtonImage = null;
    }

    private void ResetButtonVisual(Image img)
    {
        if (img == null) return;
        if (HasSprites)
            img.sprite = keyNormalSprite;
        else
            img.color = Color.white;
    }

    void Update()
    {
        if (listeningEntry == null || !Input.anyKeyDown) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelListening();
            return;
        }

        foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6) continue;
            if (!Input.GetKeyDown(kc)) continue;

            listeningEntry.currentKey    = kc;
            listeningEntry.keyLabel.text = FriendlyName(kc);
            PlayerPrefs.SetString(EldoriaPrefsKeys.KeyPrefix + listeningEntry.actionId, kc.ToString());
            PlayerPrefs.Save();
            ResetButtonVisual(listeningButtonImage);
            listeningEntry       = null;
            listeningButtonImage = null;
            return;
        }
    }

    // Short, readable display names for special keys and modifiers.
    public static string FriendlyName(KeyCode kc) => kc switch
    {
        KeyCode.LeftControl    => "L.Ctrl",
        KeyCode.RightControl   => "R.Ctrl",
        KeyCode.LeftShift      => "L.Shift",
        KeyCode.RightShift     => "R.Shift",
        KeyCode.LeftAlt        => "L.Alt",
        KeyCode.RightAlt       => "R.Alt",
        KeyCode.LeftCommand    => "L.Cmd",
        KeyCode.RightCommand   => "R.Cmd",
        KeyCode.LeftArrow      => "←",
        KeyCode.RightArrow     => "→",
        KeyCode.UpArrow        => "↑",
        KeyCode.DownArrow      => "↓",
        KeyCode.Return         => "Enter",
        KeyCode.KeypadEnter    => "KP Enter",
        KeyCode.Space          => "Espacio",
        KeyCode.Backspace      => "Retroceso",
        KeyCode.Delete         => "Supr",
        KeyCode.Tab            => "Tab",
        KeyCode.CapsLock       => "Bloq.May",
        KeyCode.Insert         => "Ins",
        KeyCode.Home           => "Inicio",
        KeyCode.End            => "Fin",
        KeyCode.PageUp         => "Re.Pág",
        KeyCode.PageDown       => "Av.Pág",
        KeyCode.Keypad0        => "KP 0",
        KeyCode.Keypad1        => "KP 1",
        KeyCode.Keypad2        => "KP 2",
        KeyCode.Keypad3        => "KP 3",
        KeyCode.Keypad4        => "KP 4",
        KeyCode.Keypad5        => "KP 5",
        KeyCode.Keypad6        => "KP 6",
        KeyCode.Keypad7        => "KP 7",
        KeyCode.Keypad8        => "KP 8",
        KeyCode.Keypad9        => "KP 9",
        KeyCode.KeypadPlus     => "KP +",
        KeyCode.KeypadMinus    => "KP -",
        KeyCode.KeypadMultiply => "KP *",
        KeyCode.KeypadDivide   => "KP /",
        KeyCode.KeypadPeriod   => "KP .",
        _                      => kc.ToString()
    };

    // Usage: KeyRebindUI.GetKey("Jump", KeyCode.Z)
    public static KeyCode GetKey(string actionId, KeyCode fallback = KeyCode.Space)
    {
        string saved = PlayerPrefs.GetString(EldoriaPrefsKeys.KeyPrefix + actionId, fallback.ToString());
        return (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
    }
}
