using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Editor script — Eldoria/Add Settings Content
// Clona filas del GraficosPanel (que ya funcionan visualmente) en lugar de crear desde cero.
// Esto garantiza que sprites, fuentes, colores y SliderFillReveal sean idénticos.
public static class SettingsContentSetup
{
    [MenuItem("Eldoria/Add Settings Content")]
    static void AddContent()
    {
        var graficosPanel  = FindInScene("GraficosPanel");
        var sonido         = FindInScene("SonidoPanel");
        var controles      = FindInScene("ControlesPanel");
        var ajustes        = FindInScene("AjustesPanel");
        var smGO           = FindInScene("SettingsManager");
        var bgGO           = FindInScene("BackGround");

        if (!graficosPanel || !sonido || !ajustes || !smGO || !controles)
        {
            Debug.LogError("[SettingsContentSetup] GraficosPanel, SonidoPanel, ControlesPanel, AjustesPanel o SettingsManager no encontrados. Abre la escena Settings.");
            return;
        }

        var gScrollViewTF  = graficosPanel.transform.Find("Scroll View");
        var gContentTF     = gScrollViewTF?.Find("Viewport/Content");
        var sliderTpl      = gContentTF?.Find("Row_Brillo")?.gameObject;
        var selectionTpl   = gContentTF?.Find("Row_Resolucion")?.gameObject;
        var toggleTpl      = FindToggleRowTemplate(gContentTF);

        if (gScrollViewTF == null || sliderTpl == null || selectionTpl == null)
        {
            Debug.LogError("[SettingsContentSetup] No se encontraron Row_Brillo o Row_Resolucion en GraficosPanel/Scroll View/Viewport/Content.");
            return;
        }
        if (toggleTpl == null)
            Debug.LogWarning("[SettingsContentSetup] No se encontró ninguna fila con Toggle en GraficosPanel. La fila 'Mostrar FPS' no se creará.");

        // ── Arreglar BackgroundVideoDisplay.fallbackClip ─────────────────
        if (bgGO != null)
        {
            var bvd  = bgGO.GetComponent<BackgroundVideoDisplay>();
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Video/BgEldoriaStartScreen.mp4");
            if (bvd != null && clip != null)
            {
                Undo.RecordObject(bvd, "Fix BG Video");
                var bvdSO = new SerializedObject(bvd);
                bvdSO.FindProperty("fallbackClip").objectReferenceValue = clip;
                bvdSO.ApplyModifiedProperties();
                Debug.Log("[SettingsContentSetup] fallbackClip asignado correctamente.");
            }
            else if (clip == null)
                Debug.LogWarning("[SettingsContentSetup] Video no encontrado en Assets/Video/BgEldoriaStartScreen.mp4");
        }

        Undo.RegisterFullObjectHierarchyUndo(sonido,  "Add Sonido Content");
        Undo.RegisterFullObjectHierarchyUndo(ajustes, "Add Ajustes Content");
        Undo.RecordObject(smGO.GetComponent<SettingsManager>(), "Wire SettingsManager");

        // ── SONIDO ────────────────────────────────────────────────────────
        ClearChildren(sonido);
        var sContent = CloneScrollView(sonido, gScrollViewTF.gameObject);

        var masterS  = CloneSliderRow(sContent, sliderTpl, "Volumen maestro",    1f);
        var musicS   = CloneSliderRow(sContent, sliderTpl, "Música",             1f);
        var sfxS     = CloneSliderRow(sContent, sliderTpl, "Efectos de sonido",  1f);
        var voicesS  = CloneSliderRow(sContent, sliderTpl, "Voces",              1f);
        var uiS      = CloneSliderRow(sContent, sliderTpl, "Interfaz",           1f);

        // ── AJUSTES ───────────────────────────────────────────────────────
        ClearChildren(ajustes);
        var aContent = CloneScrollView(ajustes, gScrollViewTF.gameObject);

        var langSel    = CloneSelectionRow(aContent, selectionTpl, "Idioma");
        Toggle showFpsTog = null;
        if (toggleTpl != null)
            showFpsTog = CloneToggleRow(aContent, toggleTpl, "Mostrar FPS");

        // ── Cablear SettingsManager ───────────────────────────────────────
        var smSO = new SerializedObject(smGO.GetComponent<SettingsManager>());
        // Paneles
        smSO.FindProperty("graficosPanel") .objectReferenceValue = graficosPanel;
        smSO.FindProperty("sonidoPanel")   .objectReferenceValue = sonido;
        smSO.FindProperty("controlesPanel").objectReferenceValue = controles;
        smSO.FindProperty("ajustesPanel")  .objectReferenceValue = ajustes;
        // Sliders de sonido
        smSO.FindProperty("masterVolumeSlider").objectReferenceValue = masterS;
        smSO.FindProperty("musicSlider")       .objectReferenceValue = musicS;
        smSO.FindProperty("sfxSlider")         .objectReferenceValue = sfxS;
        smSO.FindProperty("voicesSlider")      .objectReferenceValue = voicesS;
        smSO.FindProperty("uiSlider")          .objectReferenceValue = uiS;
        // Ajustes
        smSO.FindProperty("languageSelector").objectReferenceValue = langSel;
        smSO.FindProperty("showFpsToggle")   .objectReferenceValue = showFpsTog;
        smSO.ApplyModifiedProperties();

        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(sonido.scene);
        Debug.Log("[SettingsContentSetup] Listo. Guarda la escena Settings con Ctrl+S.");
    }

    // Clona el Scroll View del GraficosPanel, lo parenta al panel destino,
    // y devuelve el Content vacío listo para recibir filas.
    static GameObject CloneScrollView(GameObject panel, GameObject scrollViewTemplate)
    {
        var clone = (GameObject)Object.Instantiate(scrollViewTemplate);
        clone.name = "Scroll View";
        GameObjectUtility.SetParentAndAlign(clone, panel);

        var content = clone.transform.Find("Viewport/Content");
        if (content != null)
        {
            ClearChildren(content.gameObject);
            // Reset top padding — GraficosPanel tiene relleno extra para su elemento Tittle (335px);
            // los demás paneles no tienen Tittle, así que reseteamos a un margen pequeño.
            var vlg = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg != null)
                vlg.padding = new RectOffset(vlg.padding.left, vlg.padding.right, 10, vlg.padding.bottom);
        }
        return content?.gameObject;
    }

    // Clona Row_Brillo, cambia el label y el valor inicial del slider.
    static Slider CloneSliderRow(GameObject content, GameObject template, string label, float initVal)
    {
        var clone = (GameObject)Object.Instantiate(template);
        clone.name = "Row_" + label;
        GameObjectUtility.SetParentAndAlign(clone, content);

        var lbl = clone.transform.Find("Lbl")?.GetComponent<TMP_Text>();
        if (lbl != null) lbl.text = label;

        var slider = clone.GetComponentInChildren<Slider>(true);
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value    = initVal;
        }
        return slider;
    }

    // Clona Row_Resolucion, cambia el label y limpia el SelectionControl.
    static SelectionControl CloneSelectionRow(GameObject content, GameObject template, string label)
    {
        var clone = (GameObject)Object.Instantiate(template);
        clone.name = "Row_" + label;
        GameObjectUtility.SetParentAndAlign(clone, content);

        var lbl = clone.transform.Find("Lbl")?.GetComponent<TMP_Text>();
        if (lbl != null) lbl.text = label;

        var sc = clone.GetComponentInChildren<SelectionControl>(true);
        // Limpia el texto del valor para evitar que muestre la resolución anterior
        var scSO = new SerializedObject(sc);
        var valLabelProp = scSO.FindProperty("valueLabel");
        if (valLabelProp?.objectReferenceValue is TMP_Text valLbl)
            valLbl.text = "—";
        scSO.ApplyModifiedProperties();

        return sc;
    }

    // Busca la primera fila con Toggle en el Content del GraficosPanel para usarla como plantilla.
    static GameObject FindToggleRowTemplate(Transform contentTF)
    {
        if (contentTF == null) return null;
        foreach (Transform child in contentTF)
            if (child.GetComponentInChildren<Toggle>(true) != null)
                return child.gameObject;
        return null;
    }

    // Clona una fila con Toggle y cambia el label.
    static Toggle CloneToggleRow(GameObject content, GameObject template, string label)
    {
        var clone = (GameObject)Object.Instantiate(template);
        clone.name = "Row_" + label;
        GameObjectUtility.SetParentAndAlign(clone, content);

        var lbl = clone.transform.Find("Lbl")?.GetComponent<TMP_Text>();
        if (lbl != null) lbl.text = label;

        return clone.GetComponentInChildren<Toggle>(true);
    }

    static void ClearChildren(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
    }

    // Busca por nombre en toda la escena activa incluyendo objetos inactivos.
    // Necesario porque los paneles de Settings están desactivados por defecto y
    // GameObject.Find() solo detecta objetos activos.
    static GameObject FindInScene(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager
                                        .GetActiveScene().GetRootGameObjects())
        {
            var found = FindInChildren(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    static Transform FindInChildren(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
