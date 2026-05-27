#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Before each build: opens MTN10, injects the 7 boomerang frames into BossObsesion
// and saves the scene. Prevents the orange fallback in standalone builds.
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
            Debug.LogWarning("[BossBoomerangBuildPreProcess] No sprites found at: " + SPRITE_PATH);
            return;
        }

        // If MTN10 is already loaded as the only scene we cannot close it directly.
        // Always open in Additive mode; if it was already loaded reuse the existing instance.
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
            Debug.LogWarning("[BossBoomerangBuildPreProcess] MTN10 could not be loaded during the build. Boomerang frames will not be injected automatically.");
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
                Debug.LogWarning("[BossBoomerangBuildPreProcess] Field 'boomerangFrames' not found on BossObsesion.");
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
            Debug.Log($"[BossBoomerangBuildPreProcess] ✓ {sprites.Count} boomerang frames injected into MTN10.");
        }
        else
        {
            Debug.LogWarning("[BossBoomerangBuildPreProcess] BossObsesion not found in MTN10.");
        }

        // Only close if we opened it (not if it was already loaded)
        if (!alreadyLoaded && EditorSceneManager.sceneCount > 1)
            EditorSceneManager.CloseScene(scene, false);
    }
}
#endif
