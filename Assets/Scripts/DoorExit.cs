using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Zona de salida en puerta. Muestra etiqueta flotante al acercarse el Player.
// Patrón: Command — la acción "cambiar de sala" es encapsulada y solo se ejecuta
//          cuando el jugador está en la zona Y presiona la tecla de Interacción.
[RequireComponent(typeof(BoxCollider2D))]
public class DoorExit : MonoBehaviour
{
    [Header("Destino")]
    [SerializeField] private string targetScene = "HV01_Exterior";
    [SerializeField] private string spawnId     = "";   // ID del SpawnPoint en la escena destino

    [Header("Label flotante")]
    [SerializeField] private string labelText      = "[ E ]  SALIR";
    [SerializeField] private Transform labelTransform;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private float bobAmplitude = 0.25f;
    [SerializeField] private float bobSpeed     = 2.5f;

    private bool  _playerNear;
    private float _labelBaseY;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (labelTransform == null)
            labelTransform = BuildLabel();

        if (labelTransform != null)
        {
            // Usa posición WORLD para que el bobbing no dependa de la escala del padre
            _labelBaseY = labelTransform.position.y;
            labelTransform.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!_playerNear) return;

        // Bobbing sinusoidal en espacio WORLD (sin dependencia de escala del padre)
        if (labelTransform != null)
        {
            float baseWorldY = transform.position.y + 3.5f;
            labelTransform.position = new Vector3(
                transform.position.x,
                baseWorldY + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude,
                labelTransform.position.z);
        }

        if (Input.GetKeyDown(KeyRebindUI.GetKey("Interact", KeyCode.E)))
            Exit();
    }

    // ── Detección de jugador ──────────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNear = true;
        if (labelTransform != null) labelTransform.gameObject.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNear = false;
        if (labelTransform != null) labelTransform.gameObject.SetActive(false);
    }

    // ── Transición de sala ────────────────────────────────────────────────────
    private void Exit()
    {
        if (string.IsNullOrEmpty(targetScene)) return;

        if (!string.IsNullOrEmpty(spawnId))
            PlayerSpawnManager.NextSpawnId = spawnId;

        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }

    // ── Crear label en runtime si no fue asignado desde el editor ────────────
    // Usa un Canvas WorldSpace + TextMeshProUGUI para garantizar visibilidad
    // independientemente del sorting layer de los SpriteRenderers.
    private Transform BuildLabel()
    {
        var canvasGO = new GameObject("_DoorLabel");
        // Sin parent: evita heredar la escala del DoorExit trigger.
        // La posición se actualiza en Update() con transform.position del padre.
        canvasGO.transform.position   = transform.position + new Vector3(0f, 3.5f, 0f);
        canvasGO.transform.localScale = new Vector3(0.02f, 0.02f, 1f);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(300f, 60f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);

        var textRT       = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var tmp               = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text              = labelText;
        tmp.fontSize          = 36f;
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.color             = Color.white;
        tmp.enableWordWrapping = false;

        if (labelFont != null) tmp.font = labelFont;

        return canvasGO.transform;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
        Vector3 c = transform.TransformPoint(col.offset);
        Gizmos.DrawCube(c, new Vector3(col.size.x, col.size.y, 0.1f));
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
        Gizmos.DrawWireCube(c, new Vector3(col.size.x, col.size.y, 0.1f));
    }
#endif
}
