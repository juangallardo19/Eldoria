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
        if (t != null)
        {
            _label = t.GetComponent<TMP_Text>();
        }
        else
        {
            // Crear label programáticamente — no depende de un hijo preexistente en escena
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 4f, 0f);
            var tmp = labelGO.AddComponent<TextMeshPro>();
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.fontSize   = 3.5f;
            tmp.fontStyle  = FontStyles.Bold;
            tmp.color      = Color.white;
#if UNITY_EDITOR
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
            var font = Resources.Load<TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
            if (font != null) tmp.font = font;
            _label = tmp;
        }

        _label.gameObject.SetActive(false);
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
