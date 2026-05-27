using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Editor script — Eldoria/Fix Settings Camera
// Adds a MainCamera to the Settings scene and switches the Canvas to Screen Space - Camera
// so ScreenColorEffect (brightness/contrast/saturation) visually affects the UI.
// Run with the Settings scene open, then save with Ctrl+S.
public static class SettingsSceneFix
{
    [MenuItem("Eldoria/Fix Settings Camera")]
    static void FixSettingsCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            var camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(camGO, "Add MainCamera to Settings");

            cam = camGO.AddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = Color.black;
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.cullingMask      = -1;  // Everything — required for Screen Space-Camera Canvas to render the UI layer
            cam.depth            = -1;

            Debug.Log("[SettingsSceneFix] MainCamera created in the Settings scene.");
        }
        else
        {
            Debug.Log("[SettingsSceneFix] MainCamera already exists; reusing.");
        }

        // Switch Canvas to Screen Space - Camera so OnRenderImage affects the UI
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Undo.RecordObject(canvas, "Fix Canvas Render Mode");
            canvas.renderMode   = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera  = cam;
            canvas.planeDistance = 1f;
            Debug.Log("[SettingsSceneFix] Canvas → Screen Space - Camera.");
        }
        else
        {
            Debug.LogError("[SettingsSceneFix] No Canvas found in the scene.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SettingsSceneFix] Done. Save the scene with Ctrl+S.");
    }
}
