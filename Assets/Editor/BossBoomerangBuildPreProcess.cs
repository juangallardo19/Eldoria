#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Antes de cada build: abre MTN10, inyecta los 7 frames del boomerang en BossObsesion
// y guarda la escena. Evita el fallback naranja en builds standalone.
public class BossBoomerangBuildPreProcess : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        const string SCENE_PATH  = "Assets/Scenes/Montanas/MTN10.unity";
        const string SPRITE_PATH = "Assets/Sprites/Boss1Obsesion/Sprite Sheets/boomarang arms.png";

        var assets = AssetDatabase.LoadAllAssetsAtPath(SPRITE_PATH);
        var sprites = new List<Sprite>();
        foreach (var a in assets)
            if (a is Sprite s) sprites.Add(s);
        sprites.Sort((a, b) =>
            System.StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));

        if (sprites.Count == 0)
        {
            Debug.LogWarning("[BossBoomerangBuildPreProcess] No se encontraron sprites en: " + SPRITE_PATH);
            return;
        }

        // Si MTN10 ya está cargada como única escena, no podemos cerrarla directamente.
        // La abrimos siempre en modo Additive; si ya estaba cargada reutilizamos la existente.
        bool alreadyLoaded = false;
        UnityEngine.SceneManagement.Scene scene = default;
        for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
        {
            var s = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
            if (s.path == SCENE_PATH) { scene = s; alreadyLoaded = true; break; }
        }
        if (!alreadyLoaded)
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Additive);

        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[BossBoomerangBuildPreProcess] MTN10 no pudo cargarse durante el build. Los frames de boomerang no se inyectarán automáticamente.");
            return;
        }

        bool wired = false;

        foreach (var root in scene.GetRootGameObjects())
        {
            var boss = root.GetComponentInChildren<BossObsesion>(true);
            if (boss == null) continue;

            var so   = new SerializedObject(boss);
            var prop = so.FindProperty("boomerangFrames");
            if (prop == null)
            {
                Debug.LogWarning("[BossBoomerangBuildPreProcess] Campo 'boomerangFrames' no encontrado en BossObsesion.");
                break;
            }

            prop.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(boss);
            wired = true;
            break;
        }

        if (wired)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BossBoomerangBuildPreProcess] ✓ {sprites.Count} frames de boomerang inyectados en MTN10.");
        }
        else
        {
            Debug.LogWarning("[BossBoomerangBuildPreProcess] BossObsesion no encontrado en MTN10.");
        }

        // Solo cerrar si la abrimos nosotros (no si ya estaba cargada)
        if (!alreadyLoaded && EditorSceneManager.sceneCount > 1)
            EditorSceneManager.CloseScene(scene, false);
    }
}
#endif
