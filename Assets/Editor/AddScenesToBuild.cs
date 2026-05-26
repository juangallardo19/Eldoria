using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class AddScenesToBuild
{
    [MenuItem("Eldoria/Add All Scenes To Build")]
    static void Execute()
    {
        var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        var existing = EditorBuildSettings.scenes.ToList();
        var existingPaths = new HashSet<string>(existing.Select(s => s.path));

        int added = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (existingPaths.Contains(path)) continue;
            existing.Add(new EditorBuildSettingsScene(path, true));
            existingPaths.Add(path);
            added++;
        }

        EditorBuildSettings.scenes = existing.ToArray();
        Debug.Log($"[Eldoria] Build Settings actualizado: {added} escena(s) añadida(s). Total: {existing.Count}.");
    }
}
