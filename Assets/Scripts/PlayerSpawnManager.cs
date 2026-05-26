using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton DontDestroyOnLoad que gestiona dónde aparece el jugador al cambiar de escena.
//
// Flujo:
//   1. SceneBoundary (o DoorExit) escribe PlayerSpawnManager.NextSpawnId = "left" / "right" / "door_HV04" etc.
//   2. La escena carga.
//   3. OnSceneLoaded busca el SpawnPoint con ese ID y teletransporta al Player ahí.
//   4. NextSpawnId se resetea a "default" para la próxima transición.
public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    // Garantiza que siempre exista un PSM, incluso si la escena no tiene uno.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[PlayerSpawnManager]");
        go.AddComponent<PlayerSpawnManager>();
    }

    // Escribir antes de cargar la escena destino para controlar dónde aparece el jugador.
    public static string NextSpawnId = "default";

    // Santuario: posición de reaparición sin necesitar un SpawnPoint en escena.
    public static bool    UsePositionOverride    = false;
    public static Vector2 OverridePositionValue  = Vector2.zero;

    private bool _isFirstInstance;
    // sceneLoaded fires before Start(); this flag lets Start() skip PlacePlayer
    // when OnSceneLoaded already handled it, preventing the double-coroutine bug.
    private bool _sceneLoadHandled;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        _isFirstInstance = true;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Only runs when PSM is added at runtime mid-scene (sceneLoaded won't re-fire).
        if (_isFirstInstance && !_sceneLoadHandled)
            StartCoroutine(PlacePlayer());
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _sceneLoadHandled = true;
        StopAllCoroutines();
        StartCoroutine(PlacePlayer());
    }

    IEnumerator PlacePlayer()
    {
        // Esperar un frame para que todos los Awake() terminen
        yield return null;

        var player = GameObject.FindGameObjectWithTag("Player");
        // Fallback: si no tiene tag "Player", buscar por componente
        if (player == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }
        if (player == null) { NextSpawnId = "default"; yield break; }

        // Override de posición (usado por respawn en santuario de Ara)
        if (UsePositionOverride)
        {
            var rbO = player.GetComponent<Rigidbody2D>();
            if (rbO != null) rbO.velocity = Vector2.zero;
            player.transform.position = OverridePositionValue;
            UsePositionOverride   = false;
            NextSpawnId           = "default";
            yield break;
        }

        SpawnPoint target = FindSpawnPoint(NextSpawnId);

        // Fallback a "default" SOLO si el ID pedido era "default" (no caer al centro
        // cuando el ID es específico y el SpawnPoint simplemente no existe en la escena).
        if (target == null && NextSpawnId == "default")
            target = FindSpawnPoint("default");

        if (target != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            player.transform.position = target.transform.position;
        }

        NextSpawnId = "default";
    }

    static SpawnPoint FindSpawnPoint(string id)
    {
        foreach (var sp in FindObjectsOfType<SpawnPoint>())
            if (sp.spawnId == id) return sp;
        return null;
    }
}
