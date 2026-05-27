using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor script — Eldoria/Add Localization Labels
// Adds the LocalizedText component to all TMP_Text named "Lbl" inside Settings panels,
// and to direct TMP_Text children of tab buttons.
// Run with the Settings scene open, then save with Ctrl+S.
public static class SettingsAddLocalization
{
    [MenuItem("Eldoria/Add Localization Labels")]
    static void AddLocalization()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[SettingsAddLocalization] Canvas not found. Open the Settings scene.");
            return;
        }

        int count = 0;
        foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!ShouldLocalize(text)) continue;
            if (text.GetComponent<LocalizedText>() != null) continue;
            if (string.IsNullOrWhiteSpace(text.text)) continue;

            Undo.AddComponent<LocalizedText>(text.gameObject).key = text.text;
            count++;
        }

        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Debug.Log($"[SettingsAddLocalization] LocalizedText added to {count} labels. Save with Ctrl+S.");
    }

    // Localises any static TMP_Text whose content is a known key in LocalizationManager,
    // or a section separator (contains "──"). Excludes dynamic value labels.
    static bool ShouldLocalize(TMP_Text text)
    {
        if (string.IsNullOrWhiteSpace(text.text)) return false;
        if (IsExcluded(text)) return false;

        // Row label
        if (text.gameObject.name == "Lbl") return true;

        // Section separators (── Screen ──, etc.)
        if (text.text.Contains("──")) return true;

        // Text inside a Button (tabs, Back button, etc.)
        if (IsInsideNamedButton(text.transform)) return true;

        // Any text that is a known localisation key
        return LocalizationManager.ContainsKey(text.text);
    }

    // Excludes TMP_Text that are dynamic content, not static labels.
    static bool IsExcluded(TMP_Text text)
    {
        // SelectionControl's ValueLabel — content driven by code
        if (text.gameObject.name == "ValueLabel") return true;
        // Direct children of a SelectionControl
        if (text.transform.parent?.GetComponent<SelectionControl>() != null) return true;
        // Purely numeric text (resolutions, fps values, etc.)
        if (int.TryParse(text.text, out _)) return true;
        return false;
    }

    // Walks the hierarchy looking for a Button with "tab" or "back" in its name.
    static bool IsInsideNamedButton(Transform t)
    {
        while (t != null)
        {
            if (t.GetComponent<Button>() != null)
            {
                string n = t.gameObject.name.ToLower();
                return n.Contains("tab") || n.Contains("back");
            }
            t = t.parent;
        }
        return false;
    }
}
