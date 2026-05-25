using UnityEngine;
using TMPro;

// Patrón Command — zona secreta no desbloqueada. Muestra aviso TMP al entrar; nunca carga escena.
[RequireComponent(typeof(Collider2D))]
public class LockedZone : MonoBehaviour
{
    [SerializeField] private string message = "— ZONA NO DESBLOQUEADA —";
    private TMP_Text _label;

    void Awake()
    {
        var t = transform.Find("Label");
        if (t) _label = t.GetComponent<TMP_Text>();
        if (_label) _label.gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_label) { _label.text = message; _label.gameObject.SetActive(true); }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_label) _label.gameObject.SetActive(false);
    }
}
