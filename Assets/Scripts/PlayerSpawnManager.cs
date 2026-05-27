using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton DontDestroyOnLoad that manages where the player appears after a scene change.
//
// Flow:
//   1. SceneBoundary (or DoorExit) writes PlayerSpawnManager.NextSpawnId = "left" / "right" / "door_HV04" etc.
//   2. The scene loads.
//   3. OnSceneLoaded finds the SpawnPoint with that ID and teleports the player there.
//   4. NextSpawnId resets to "default" for the next transition.
public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    // Ensures a PSM always exists, even if the scene doesn't have one.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[PlayerSpawnManager]");
        go.AddComponent<PlayerSpawnManager>();
    }

    // Write before loading the target scene to control where the player appears.
    public static string NextSpawnId = "default";

    // Running state saved when crossing a SceneBoundary; restored to the new player instance.
    public static bool SavedRunningMode = false;

    // Sanctuary: spawn position without needing a SpawnPoint in the scene.
    public static bool    UsePositionOverride   = false;
    public static Vector2 OverridePositionValue = Vector2.zero;

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
        // Wait one frame for all Awake() calls to complete
        yield return null;

        var player = GameObject.FindGameObjectWithTag("Player");
        // Fallback: if no "Player" tag, search by component
        if (player == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }
        if (player == null) { NextSpawnId = "default"; yield break; }

        // Position override (used by Ara sanctuary respawn)
        if (UsePositionOverride)
        {
            var rbO = player.GetComponent<Rigidbody2D>();
            if (rbO != null) rbO.velocity = Vector2.zero;
            player.transform.position = OverridePositionValue;
            UsePositionOverride = false;
            NextSpawnId         = "default";
            yield break;
        }

        SpawnPoint target = FindSpawnPoint(NextSpawnId);

        // Fall back to "default" ONLY if the requested ID was already "default"
        // (avoids snapping to center when a specific ID simply doesn't exist in the scene).
        if (target == null && NextSpawnId == "default")
            target = FindSpawnPoint("default");

        if (target != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            player.transform.position = target.transform.position;
        }

        var playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.SetRunningMode(SavedRunningMode);

        NextSpawnId = "default";
    }

    static SpawnPoint FindSpawnPoint(string id)
    {
        foreach (var sp in FindObjectsOfType<SpawnPoint>())
            if (sp.spawnId == id) return sp;
        return null;
    }
}
