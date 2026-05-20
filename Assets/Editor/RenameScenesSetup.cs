#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Renombra Game.unity → HV01_Interior.unity y HubCentral.unity → HV01_Exterior.unity.
// Preserva GUIDs para que todas las referencias del proyecto queden válidas.
// Menú: Eldoria → Rename Scenes to HV01
public static class RenameScenesSetup
{
    [MenuItem("Eldoria/Rename Scenes to HV01")]
    static void RenameScenes()
    {
        Rename("Assets/Scenes/Game.unity",                    "HV01_Interior");
        Rename("Assets/Scenes/HubCentral/HubCentral.unity",  "HV01_Exterior");
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RenameScenesSetup] ✓ Escenas renombradas. Build Settings actualizados.");
    }

    static void Rename(string oldPath, string newName)
    {
        var err = AssetDatabase.RenameAsset(oldPath, newName);
        if (string.IsNullOrEmpty(err))
            Debug.Log($"[RenameScenesSetup] {oldPath} → {newName}.unity ✓");
        else
            Debug.LogError($"[RenameScenesSetup] Error renombrando {oldPath}: {err}");
    }

    static void UpdateBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        var updated = new List<EditorBuildSettingsScene>();
        foreach (var s in scenes)
        {
            string path = s.path;
            if (path == "Assets/Scenes/Game.unity")
                path = "Assets/Scenes/HV01_Interior.unity";
            else if (path == "Assets/Scenes/HubCentral/HubCentral.unity")
                path = "Assets/Scenes/HubCentral/HV01_Exterior.unity";
            updated.Add(new EditorBuildSettingsScene(path, s.enabled));
        }
        EditorBuildSettings.scenes = updated.ToArray();
    }
}
#endif
