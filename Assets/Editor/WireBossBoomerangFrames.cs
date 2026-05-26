#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

// Asigna los frames del boomerang directamente en BossObsesion dentro de MTN10.
// Esto permite que el boomerang funcione en builds (los sprites quedan serializados en la escena).
// PROBLEMA RAÍZ: boomerangFrames no estaba asignado en el Inspector → en builds no hay
//               AssetDatabase disponible → LoadFrames retorna null → recuadro naranja.
// Menú: Eldoria/Boss/5 - Wire Boomerang Frames (MTN10)
public static class WireBossBoomerangFrames
{
    [MenuItem("Eldoria/Boss/5 - Wire Boomerang Frames (MTN10)")]
    static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "MTN10")
        {
            EditorUtility.DisplayDialog("Eldoria", "Abre MTN10 antes de ejecutar este script.", "OK");
            return;
        }

        const string SPRITE_PATH = "Assets/Sprites/Boss1Obsesion/Sprite Sheets/boomarang arms.png";

        // Cargar todos los sub-sprites del sheet
        var assets = AssetDatabase.LoadAllAssetsAtPath(SPRITE_PATH);
        var sprites = new List<Sprite>();
        foreach (var a in assets)
            if (a is Sprite s) sprites.Add(s);

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Eldoria",
                "No se encontraron sub-sprites en boomarang arms.png.\n" +
                "Verifica que Sprite Mode = Multiple en su Import Settings.", "OK");
            return;
        }

        sprites.Sort((a, b) =>
            System.StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));

        // Encontrar BossObsesion en la escena (incluso si está inactivo)
        var allGOs = Resources.FindObjectsOfTypeAll<BossObsesion>();
        BossObsesion boss = null;
        foreach (var b in allGOs)
            if (b.gameObject.scene == scene) { boss = b; break; }

        if (boss == null)
        {
            Debug.LogError("[WireBossBoomerangFrames] No se encontró BossObsesion en MTN10.");
            return;
        }

        // Asignar frames usando SerializedObject para que quede guardado en la escena
        var so   = new SerializedObject(boss);
        var prop = so.FindProperty("boomerangFrames");
        prop.arraySize = sprites.Count;
        for (int i = 0; i < sprites.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(boss);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[WireBossBoomerangFrames] ✓ {sprites.Count} frames asignados a BossObsesion.boomerangFrames en MTN10. " +
                  $"Ahora funciona en builds.");
    }
}
#endif
