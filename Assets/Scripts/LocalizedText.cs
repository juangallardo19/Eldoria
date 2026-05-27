using TMPro;
using UnityEngine;

// Observer subscriber — updates the attached TMP_Text whenever the language changes.
// Patterns:
//   Observer  — subscribes to LocalizationManager.OnLanguageChanged (unsubscribes in OnDisable).
//   Flyweight — does not store the table; requests it from LocalizationManager (shared table).
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] public string key;

    TMP_Text _text;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        if (string.IsNullOrEmpty(key)) key = _text.text;
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += Refresh;
        Refresh(LocalizationManager.CurrentLanguage);
    }

    void OnDisable() => LocalizationManager.OnLanguageChanged -= Refresh;

    void Refresh(string _) => _text.text = LocalizationManager.Get(key);
}
