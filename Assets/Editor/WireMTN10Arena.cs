#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Sets up arena barriers in MTN10:
//  - Adds ArenaBarrier to ArenaWalls that have a BoxCollider2D
//  - Disables them by default (player can pass through before the boss wakes)
//  - Removes empty ArenaWalls at (0,0,0) that are useless placeholders
// Menu: Eldoria/Boss/4 - Wire MTN10 Arena Barriers
public static class WireMTN10Arena
{
    [MenuItem("Eldoria/Boss/4 - Wire MTN10 Arena Barriers")]
    static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "MTN10")
        {
            EditorUtility.DisplayDialog("Eldoria", "Open MTN10 before running.", "OK");
            return;
        }

        int fixed_count    = 0;
        int removed_count  = 0;

        // Search ALL GameObjects named "ArenaWall_Left" or "ArenaWall_Right"
        // including inactive ones
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go.scene != scene) continue;
            if (go.name != "ArenaWall_Left" && go.name != "ArenaWall_Right") continue;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null || col.size.magnitude < 0.1f)
            {
                // Empty placeholder → eliminar
                Object.DestroyImmediate(go);
                removed_count++;
                continue;
            }

            // Real barrier: add ArenaBarrier if missing, then deactivate the GO
            if (go.GetComponent<ArenaBarrier>() == null)
                go.AddComponent<ArenaBarrier>();

            go.SetActive(false);   // inactivo por defecto
            EditorUtility.SetDirty(go);
            fixed_count++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[WireMTN10Arena] ✓ {fixed_count} barriers configured (inactive by default), " +
                  $"{removed_count} placeholders removed. Done.");
    }
}
#endif
