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

    // Escribir antes de cargar la escena destino para controlar dónde aparece el jugador.
    public static string NextSpawnId = "default";

    // true solo en la primera instancia creada; la escena propia ya cargó antes
    // de que pudiéramos suscribirnos a sceneLoaded, así que llamamos PlacePlayer en Start.
    private bool _isFirstInstance;

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
        // La primera vez que existe el PSM, sceneLoaded ya disparó antes de que
        // nos suscribiéramos. Recuperamos el evento colocando al jugador ahora.
        if (_isFirstInstance)
            StartCoroutine(PlacePlayer());
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        StartCoroutine(PlacePlayer());
    }

    IEnumerator PlacePlayer()
    {
        // Esperar un frame para que todos los Awake() terminen
        yield return null;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { NextSpawnId = "default"; yield break; }

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
