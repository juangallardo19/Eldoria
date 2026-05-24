using TMPro;
using UnityEngine;

/// Observer subscriber — actualiza el TMP_Text adjunto cada vez que cambia el idioma.
///
/// Patrones aplicados:
///   Observer  — suscribe a LocalizationManager.OnLanguageChanged (se desuscribe en OnDisable).
///   Flyweight — no almacena la tabla; la pide a LocalizationManager (tabla compartida).
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
