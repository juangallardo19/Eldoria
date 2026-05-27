#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

// Assigns boomerang frames directly to BossObsesion inside MTN10.
// This allows the boomerang to work in builds (sprites become serialized in the scene).
// ROOT CAUSE: boomerangFrames was not assigned in the Inspector → builds have no
//             AssetDatabase → LoadFrames returns null → orange fallback box.
// Menu: Eldoria/Boss/5 - Wire Boomerang Frames (MTN10)
public static class WireBossBoomerangFrames
{
    [MenuItem("Eldoria/Boss/5 - Wire Boomerang Frames (MTN10)")]
    static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "MTN10")
        {
            EditorUtility.DisplayDialog("Eldoria", "Open MTN10 before running this script.", "OK");
            return;
        }

        const string SPRITE_PATH = "Assets/Sprites/Boss1Obsesion/Sprite Sheets/boomarang arms.png";

        // Load all sub-sprites from the sheet
        var assets = AssetDatabase.LoadAllAssetsAtPath(SPRITE_PATH);
        var sprites = new List<Sprite>();
        foreach (var a in assets)
            if (a is Sprite s) sprites.Add(s);

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Eldoria",
                "No sub-sprites found in boomarang arms.png.\n" +
                "Make sure Sprite Mode = Multiple in its Import Settings.", "OK");
            return;
        }

        sprites.Sort((a, b) =>
            System.StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));

        // Find BossObsesion in the scene (including inactive objects)
        var allGOs = Resources.FindObjectsOfTypeAll<BossObsesion>();
        BossObsesion boss = null;
        foreach (var b in allGOs)
            if (b.gameObject.scene == scene) { boss = b; break; }

        if (boss == null)
        {
            Debug.LogError("[WireBossBoomerangFrames] BossObsesion not found in MTN10.");
            return;
        }

        // Assign frames via SerializedObject so they are persisted in the scene
        var so   = new SerializedObject(boss);
        var prop = so.FindProperty("boomerangFrames");
        prop.arraySize = sprites.Count;
        for (int i = 0; i < sprites.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(boss);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[WireBossBoomerangFrames] ✓ {sprites.Count} frames assigned to BossObsesion.boomerangFrames in MTN10. " +
                  $"Now works in builds.");
    }
}
#endif
