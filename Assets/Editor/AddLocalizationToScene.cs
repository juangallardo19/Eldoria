using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor script — Eldoria/Add Localization (Scene)
// Adds LocalizedText to ANY TMP_Text in the active scene whose content
// matches a key registered in LocalizationManager.
// Also applies to buttons and labels with descriptive names.
// Run with the desired scene open. Idempotent: skips already-localised texts.
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

        Debug.Log($"[AddLocalizationToScene] LocalizedText added to {count} elements " +
                  $"in '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'. " +
                  "Save with Ctrl+S.");
    }

    static bool ShouldLocalize(TMP_Text text)
    {
        if (string.IsNullOrWhiteSpace(text.text)) return false;
        if (IsExcluded(text)) return false;

        // Known key in the localisation table
        if (LocalizationManager.ContainsKey(text.text)) return true;

        // Section separators like "── Screen ──"
        if (text.text.Contains("──")) return true;

        // Row label or typical Eldoria descriptive name
        string n = text.gameObject.name;
        if (n == "Lbl" || n == "Title" || n == "Header") return true;

        // Text inside a Button
        if (IsInsideButton(text.transform)) return true;

        return false;
    }

    static bool IsExcluded(TMP_Text text)
    {
        if (text.gameObject.name == "ValueLabel") return true;
        if (text.transform.parent?.GetComponent<SelectionControl>() != null) return true;
        if (int.TryParse(text.text.Trim(), out _)) return true;
        if (text.text.Contains("×")) return true; // resolution strings "1920 × 1080"
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
