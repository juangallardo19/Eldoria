using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Exit zone at a door. Shows a floating label when the Player approaches.
// Pattern: Command — the "change room" action is encapsulated and only executed
//          when the player is in the zone AND presses the Interact key.
[RequireComponent(typeof(BoxCollider2D))]
public class DoorExit : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string targetScene = "HV01_Exterior";
    [SerializeField] private string spawnId     = "";   // SpawnPoint ID in the destination scene

    [Header("Floating label")]
    [SerializeField] private string labelText       = "[ E ]  SALIR";
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
            // Use WORLD position so bobbing is independent of parent scale
            _labelBaseY = labelTransform.position.y;
            labelTransform.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!_playerNear) return;

        // Sinusoidal bob in WORLD space (no parent scale dependency)
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

    // ── Player detection ──────────────────────────────────────────────────────
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

    // ── Room transition ───────────────────────────────────────────────────────
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

    // ── Build label at runtime when none was assigned from the editor ─────────
    // Uses a WorldSpace Canvas + TextMeshProUGUI to ensure visibility
    // regardless of SpriteRenderer sorting layers.
    private Transform BuildLabel()
    {
        var canvasGO = new GameObject("_DoorLabel");
        // No parent: avoids inheriting DoorExit trigger scale.
        // Position is updated each Update() from the parent's transform.position.
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
