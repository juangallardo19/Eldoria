using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyRebindUI : MonoBehaviour
{
    [System.Serializable]
    public class BindingEntry
    {
        public string   actionId;    // used as PlayerPrefs key suffix
        public KeyCode  defaultKey;
        public Button   rebindButton;
        public TMP_Text keyLabel;
        [HideInInspector] public KeyCode currentKey;
    }

    [SerializeField] private BindingEntry[] entries;
    [SerializeField] private Color listeningColor = new Color(1f, 0.85f, 0.4f, 1f);

    private BindingEntry listeningEntry;
    private Image        listeningButtonImage;

    void Start()
    {
        foreach (var e in entries)
        {
            string saved = PlayerPrefs.GetString("Key_" + e.actionId, e.defaultKey.ToString());
            e.currentKey    = (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
            e.keyLabel.text = e.currentKey.ToString();

            var captured = e;
            e.rebindButton.onClick.AddListener(() => BeginListening(captured));
        }
    }

    private void BeginListening(BindingEntry entry)
    {
        if (listeningEntry != null)
            CancelListening();

        listeningEntry = entry;
        listeningButtonImage = entry.rebindButton.GetComponent<Image>();
        listeningButtonImage.color = listeningColor;
        entry.keyLabel.text = "< presiona tecla >";
    }

    private void CancelListening()
    {
        if (listeningEntry == null) return;
        listeningEntry.keyLabel.text = listeningEntry.currentKey.ToString();
        if (listeningButtonImage != null) listeningButtonImage.color = Color.white;
        listeningEntry = null;
        listeningButtonImage = null;
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
            // Ignorar clicks de ratón para no capturar el clic que inició el rebind
            if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6) continue;
            if (!Input.GetKeyDown(kc)) continue;

            listeningEntry.currentKey    = kc;
            listeningEntry.keyLabel.text = kc.ToString();
            PlayerPrefs.SetString("Key_" + listeningEntry.actionId, kc.ToString());
            PlayerPrefs.Save();
            listeningButtonImage.color = Color.white;
            listeningEntry = null;
            listeningButtonImage = null;
            return;
        }
    }

    // Llamar desde PlayerController con: KeyRebindUI.GetKey("Jump")
    public static KeyCode GetKey(string actionId, KeyCode fallback = KeyCode.Space)
    {
        string saved = PlayerPrefs.GetString("Key_" + actionId, fallback.ToString());
        return (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
    }
}
