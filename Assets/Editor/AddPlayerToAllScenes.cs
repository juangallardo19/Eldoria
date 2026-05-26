// Menú: Eldoria/Add Player To All Game Scenes
// Añade una instancia del Player prefab a todas las escenas de juego que no lo tengan.
// Seguro de re-ejecutar: si la escena ya tiene un objeto con tag "Player" no hace nada en esa escena.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AddPlayerToAllScenes
{
    static readonly string[] GameScenes =
    {
        "Assets/Scenes/HV01_Interior.unity",
        "Assets/Scenes/HubCentral/HV01_Exterior.unity",
        "Assets/Scenes/HubCentral/HV02_PlazaCentral.unity",
        "Assets/Scenes/HubCentral/HV04.unity",
        "Assets/Scenes/HubCentral/HV05.unity",
        "Assets/Scenes/HubCentral/HV06.unity",
        "Assets/Scenes/HubCentral/HV07.unity",
        "Assets/Scenes/Montanas/MTN01_Exterior.unity",
        "Assets/Scenes/Montanas/MTN01_Interior.unity",
        "Assets/Scenes/Montanas/MTN02.unity",
        "Assets/Scenes/Montanas/MTN03.unity",
        "Assets/Scenes/Montanas/MTN04.unity",
        "Assets/Scenes/Montanas/MTN05.unity",
        "Assets/Scenes/Montanas/MTN06.unity",
        "Assets/Scenes/Montanas/MTN08.unity",
        "Assets/Scenes/Montanas/MTN09.unity",
        "Assets/Scenes/Montanas/MTN10.unity",
        "Assets/Scenes/Montanas/PreMTN10.unity",
    };

    [MenuItem("Eldoria/Add Player To All Game Scenes")]
    static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Eldoria] Sal del Play Mode antes de ejecutar este menú.");
            return;
        }

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (playerPrefab == null)
        {
            Debug.LogError("[Eldoria] No se encontró Assets/Prefabs/Player.prefab");
            return;
        }

        string activePath = EditorSceneManager.GetActiveScene().path;
        int added = 0;
        int skipped = 0;

        foreach (string scenePath in GameScenes)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[Eldoria] Escena no encontrada: {scenePath}");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            bool hasPlayer = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (FindPlayerInChildren(root.transform))
                {
                    hasPlayer = true;
                    break;
                }
            }

            if (hasPlayer)
            {
                skipped++;
                continue;
            }

            // Instanciar el prefab y colocarlo en la posición del primer SpawnPoint "default", o en (0,0)
            var playerGO = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerGO.transform.localScale = new Vector3(2, 2, 1);

            // Buscar SpawnPoint "default" para colocar el player cerca
            var spawnPos = Vector3.zero;
            foreach (var root in scene.GetRootGameObjects())
            {
                var sp = FindDefaultSpawn(root.transform);
                if (sp != null) { spawnPos = sp.position; break; }
            }
            playerGO.transform.position = spawnPos;

            UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(playerGO, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            added++;
            Debug.Log($"[Eldoria] Player añadido a {System.IO.Path.GetFileNameWithoutExtension(scenePath)} en pos {spawnPos}");
        }

        // Reabrir escena original
        if (!string.IsNullOrEmpty(activePath) && System.IO.File.Exists(activePath))
            EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);

        Debug.Log($"[Eldoria] Listo — Player añadido a {added} escenas, {skipped} ya tenían Player.");
    }

    static bool FindPlayerInChildren(Transform t)
    {
        if (t.CompareTag("Player")) return true;
        for (int i = 0; i < t.childCount; i++)
            if (FindPlayerInChildren(t.GetChild(i))) return true;
        return false;
    }

    static Transform FindDefaultSpawn(Transform t)
    {
        var sp = t.GetComponent<SpawnPoint>();
        if (sp != null && sp.spawnId == "default") return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var found = FindDefaultSpawn(t.GetChild(i));
            if (found != null) return found;
        }
        return null;
    }
}
#endif
