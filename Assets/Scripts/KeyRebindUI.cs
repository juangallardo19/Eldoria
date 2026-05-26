using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Reasignación de teclas en tiempo real con sprites de teclado (KeysConfigNormal / KeysConfigPresss).
// Observer: cada cambio se guarda en PlayerPrefs inmediatamente; PlayerController lo recoge en el
// siguiente frame sin reinicio porque llama KeyRebindUI.GetKey() en Update cada vez.
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

    [Header("Sprites de tecla (Assets/UI/Sprites/KeysConfig/)")]
    [SerializeField] private Sprite keyNormalSprite;    // KeysConfigNormal.png
    [SerializeField] private Sprite keyListeningSprite; // KeysConfigPresss.png

    [Header("Color de escucha (cuando no hay sprites asignados)")]
    [SerializeField] private Color listeningColor = new Color(1f, 0.85f, 0.4f, 1f);

    private BindingEntry listeningEntry;
    private Image        listeningButtonImage;

    private bool HasSprites => keyNormalSprite != null && keyListeningSprite != null;

    void Start()
    {
        foreach (var e in entries)
        {
            string saved    = PlayerPrefs.GetString("Key_" + e.actionId, e.defaultKey.ToString());
            e.currentKey    = (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
            e.keyLabel.text = FriendlyName(e.currentKey);

            InitButtonSprites(e.rebindButton);

            var captured = e;
            e.rebindButton.onClick.AddListener(() => BeginListening(captured));
        }
    }

    // Configura el botón para usar SpriteSwap con las imágenes de teclado.
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
            PlayerPrefs.SetString("Key_" + listeningEntry.actionId, kc.ToString());
            PlayerPrefs.Save();
            ResetButtonVisual(listeningButtonImage);
            listeningEntry       = null;
            listeningButtonImage = null;
            return;
        }
    }

    // Nombres cortos y legibles para teclas especiales y modificadores.
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

    // Llamar desde PlayerController con: KeyRebindUI.GetKey("Jump", KeyCode.Z)
    public static KeyCode GetKey(string actionId, KeyCode fallback = KeyCode.Space)
    {
        string saved = PlayerPrefs.GetString("Key_" + actionId, fallback.ToString());
        return (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
    }
}
