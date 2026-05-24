using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Editor script — Eldoria/Fix Settings Camera
// Añade una MainCamera a la escena Settings y cambia el Canvas a Screen Space - Camera
// para que ScreenColorEffect (brillo/contraste/saturación) afecte visualmente a la UI.
// Ejecutar con la escena Settings abierta y después guardar con Ctrl+S.
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
            cam.cullingMask      = -1;  // Everything — necesario para que el Canvas Screen Space-Camera renderice el layer UI
            cam.depth            = -1;

            Debug.Log("[SettingsSceneFix] MainCamera creada en la escena Settings.");
        }
        else
        {
            Debug.Log("[SettingsSceneFix] MainCamera ya existe; reutilizando.");
        }

        // Cambiar Canvas a Screen Space - Camera para que OnRenderImage afecte la UI
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
            Debug.LogError("[SettingsSceneFix] No se encontró Canvas en la escena.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SettingsSceneFix] Listo. Guarda la escena con Ctrl+S.");
    }
}
