using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor script — Eldoria/Add Localization (Scene)
// Añade LocalizedText a CUALQUIER TMP_Text de la escena activa cuyo contenido
// coincida con una clave registrada en LocalizationManager.
// Aplica también a botones y labels con nombres descriptivos.
// Ejecutar con la escena deseada abierta. Repetible: omite textos ya localizados.
public static class AddLocalizationToScene
{
    [MenuItem("Eldoria/Add Localization (Scene)")]
    static void Run()
    {
        int count = 0;

        foreach (var root in UnityEngine.SceneManagement.SceneManager
                                        .GetActiveScene().GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!ShouldLocalize(text)) continue;
                if (text.GetComponent<LocalizedText>() != null) continue;

                Undo.AddComponent<LocalizedText>(text.gameObject).key = text.text;
                count++;
            }
        }

        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[AddLocalizationToScene] LocalizedText añadido a {count} elementos " +
                  $"en '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'. " +
                  "Guarda con Ctrl+S.");
    }

    static bool ShouldLocalize(TMP_Text text)
    {
        if (string.IsNullOrWhiteSpace(text.text)) return false;
        if (IsExcluded(text)) return false;

        // Clave conocida en la tabla de localización
        if (LocalizationManager.ContainsKey(text.text)) return true;

        // Separadores de sección estilo "── Pantalla ──"
        if (text.text.Contains("──")) return true;

        // Label de fila o nombre descriptivo típico de Eldoria
        string n = text.gameObject.name;
        if (n == "Lbl" || n == "Title" || n == "Header") return true;

        // Texto dentro de un Button
        if (IsInsideButton(text.transform)) return true;

        return false;
    }

    static bool IsExcluded(TMP_Text text)
    {
        if (text.gameObject.name == "ValueLabel") return true;
        if (text.transform.parent?.GetComponent<SelectionControl>() != null) return true;
        if (int.TryParse(text.text.Trim(), out _)) return true;
        if (text.text.Contains("×")) return true; // resoluciones "1920 × 1080"
        return false;
    }

    static bool IsInsideButton(Transform t)
    {
        while (t != null)
        {
            if (t.GetComponent<Button>() != null) return true;
            t = t.parent;
        }
        return false;
    }
}
