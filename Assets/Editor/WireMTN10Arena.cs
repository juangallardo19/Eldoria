#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Configura las barreras de arena en MTN10:
//  - Añade ArenaBarrier a los ArenaWall que tienen BoxCollider2D
//  - Los desactiva por defecto (el jugador puede pasar antes de que el boss despierte)
//  - Elimina los ArenaWall vacíos en (0,0,0) que son placeholders sin utilidad
// Menú: Eldoria/Boss/4 - Wire MTN10 Arena Barriers
public static class WireMTN10Arena
{
    [MenuItem("Eldoria/Boss/4 - Wire MTN10 Arena Barriers")]
    static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "MTN10")
        {
            EditorUtility.DisplayDialog("Eldoria", "Abre MTN10 antes de ejecutar.", "OK");
            return;
        }

        int fixed_count    = 0;
        int removed_count  = 0;

        // Buscar TODOS los GameObjects con nombre "ArenaWall_Left" o "ArenaWall_Right"
        // incluidos los inactivos
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

            // Real barrier: añadir ArenaBarrier si no tiene, y desactivar GO
            if (go.GetComponent<ArenaBarrier>() == null)
                go.AddComponent<ArenaBarrier>();

            go.SetActive(false);   // inactivo por defecto
            EditorUtility.SetDirty(go);
            fixed_count++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[WireMTN10Arena] ✓ {fixed_count} barreras configuradas (inactivas por defecto), " +
                  $"{removed_count} placeholders eliminados. Listo.");
    }
}
#endif
