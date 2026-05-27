using System;
using UnityEngine;
using TMPro;

// Patrón Command — NPC que el jugador puede activar con E para disparar un diálogo.
[RequireComponent(typeof(Collider2D))]
public class NPCInteract : MonoBehaviour
{
    public event Action OnInteract;

    TMP_Text _prompt;
    bool     _playerNear;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        var go = new GameObject("Prompt");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        var tmp       = go.AddComponent<TextMeshPro>();
        tmp.text      = "[E] Hablar";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = 2.8f;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;

#if UNITY_EDITOR
        var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
        var font = Resources.Load<TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
        if (font != null) tmp.font = font;

        _prompt = tmp;
        _prompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_playerNear || DialogueManager.IsActive) return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            _prompt.gameObject.SetActive(false);
            OnInteract?.Invoke();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNear = true;
        if (_prompt != null && !DialogueManager.IsActive)
            _prompt.gameObject.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNear = false;
        if (_prompt != null) _prompt.gameObject.SetActive(false);
    }
}
