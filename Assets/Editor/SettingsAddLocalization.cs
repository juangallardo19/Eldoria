using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor script — Eldoria/Add Localization Labels
// Añade el componente LocalizedText a todos los TMP_Text con nombre "Lbl" dentro de los
// paneles de Settings, y a los TMP_Text hijos directos de los botones de pestaña.
// Ejecutar con la escena Settings abierta y después guardar con Ctrl+S.
public static class SettingsAddLocalization
{
    [MenuItem("Eldoria/Add Localization Labels")]
    static void AddLocalization()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[SettingsAddLocalization] Canvas no encontrado. Abre la escena Settings.");
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
        Debug.Log($"[SettingsAddLocalization] LocalizedText añadido a {count} labels. Guarda con Ctrl+S.");
    }

    // Localiza cualquier TMP_Text estático cuyo texto sea una clave conocida en LocalizationManager,
    // o que sea un separador de sección (contiene "──"). Excluye los value labels dinámicos.
    static bool ShouldLocalize(TMP_Text text)
    {
        if (string.IsNullOrWhiteSpace(text.text)) return false;
        if (IsExcluded(text)) return false;

        // Etiqueta de fila
        if (text.gameObject.name == "Lbl") return true;

        // Separadores de sección (── Pantalla ──, etc.)
        if (text.text.Contains("──")) return true;

        // Texto dentro de un Button (pestañas, botón Volver, etc.)
        if (IsInsideNamedButton(text.transform)) return true;

        // Cualquier texto que sea una clave de localización conocida
        return LocalizationManager.ContainsKey(text.text);
    }

    // Excluye TMP_Text que son contenido dinámico, no etiquetas estáticas.
    static bool IsExcluded(TMP_Text text)
    {
        // ValueLabel de SelectionControl — contenido que cambia por código
        if (text.gameObject.name == "ValueLabel") return true;
        // Hijos directos de SelectionControl
        if (text.transform.parent?.GetComponent<SelectionControl>() != null) return true;
        // Texto puramente numérico (resoluciones, fps values, etc.)
        if (int.TryParse(text.text, out _)) return true;
        return false;
    }

    // Sube la jerarquía buscando un Button con "tab" o "back" en el nombre.
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
