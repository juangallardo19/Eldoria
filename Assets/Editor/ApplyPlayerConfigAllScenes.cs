#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Aplica la configuración actualizada del jugador (dash, hitbox de ataque) a TODAS las escenas.
// Menú: Eldoria/Apply Player Config To All Scenes
public static class ApplyPlayerConfigAllScenes
{
    [MenuItem("Eldoria/Apply Player Config To All Scenes")]
    static void Run()
    {
        string[] scenePaths = new[]
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

        var activeScene = EditorSceneManager.GetActiveScene().path;

        foreach (string path in scenePaths)
        {
            if (!System.IO.File.Exists(path)) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool modified = false;

            foreach (var go in scene.GetRootGameObjects())
                modified |= PatchHierarchy(go);

            if (modified)
                EditorSceneManager.SaveScene(scene);
        }

        // Reabrir la escena que estaba activa antes de comenzar
        if (!string.IsNullOrEmpty(activeScene))
            EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);

        Debug.Log("[ApplyPlayerConfig] Listo — configuración del jugador aplicada a todas las escenas.");
    }

    static bool PatchHierarchy(GameObject root)
    {
        bool modified = false;
        modified |= PatchPlayerController(root.GetComponentInChildren<PlayerController>(true));
        modified |= PatchPlayerCombat(root.GetComponentInChildren<PlayerCombat>(true));
        return modified;
    }

    static bool PatchPlayerController(PlayerController pc)
    {
        if (pc == null) return false;
        var so = new SerializedObject(pc);

        SetFloat(so, "dashForce",    45f);
        SetFloat(so, "dashDuration", 0.22f);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(pc);
        return true;
    }

    static bool PatchPlayerCombat(PlayerCombat combat)
    {
        if (combat == null) return false;
        var so = new SerializedObject(combat);

        SetVector2(so, "hitboxOffset", new Vector2(1.2f, 1.5f));
        SetVector2(so, "hitboxSize",   new Vector2(3f, 3f));

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(combat);
        return true;
    }

    static void SetFloat(SerializedObject so, string propName, float value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null) prop.floatValue = value;
    }

    static void SetVector2(SerializedObject so, string propName, Vector2 value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null)
        {
            prop.FindPropertyRelative("x").floatValue = value.x;
            prop.FindPropertyRelative("y").floatValue = value.y;
        }
    }
}
#endif
