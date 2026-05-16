#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// Unity menu → Eldoria → Fill Settings Content
/// Rellena el RightPanel existente con las 4 pestañas de configuración.
/// Busca objetos por nombre — no borra tu estructura visual.
public static class FillSettingsContent
{
    // ── Paleta ───────────────────────────────────────────────────────────────────
    static readonly Color32 C_TEXT    = new Color32(232, 222, 200, 255);
    static readonly Color32 C_OUTLINE = new Color32(0, 0, 0, 255);
    const float OUTLINE_W = 0.22f;
    const float ROW_H     = 56f;

    // ── Assets ───────────────────────────────────────────────────────────────────
    static TMP_FontAsset s_font;
    static Sprite s_toggleOn, s_toggleOff, s_sliderFill, s_sliderHandle;
    static Sprite s_tabNorm, s_tabActive;

    // ── Referencias recolectadas ─────────────────────────────────────────────────
    static TMP_Text     s_panelTitle;
    static GameObject   s_pGraf, s_pSon, s_pCtrl, s_pAjust;
    static Button       s_bGraf, s_bSon, s_bCtrl, s_bAjust, s_backBtn;
    static Slider       s_musicSl, s_sfxSl;
    static Toggle       s_muteM, s_muteSFX, s_fullscr, s_vsync;
    static TMP_Dropdown s_resDD, s_qualDD, s_langDD;

    // ════════════════════════════════════════════════════════════════════════════
    [MenuItem("Eldoria/Fill Settings Content")]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog("Fill Settings Content",
            "Rellena el RightPanel con las pestañas de configuración.\n" +
            "Guarda la escena primero (Ctrl+S).\n\n¿Continuar?",
            "Sí", "Cancelar")) return;

        LoadAssets();
        CollectButtons();

        var rightPanel = FindRightPanel();
        if (rightPanel == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'RightPanel' en la escena.\n" +
                "Asegúrate de que el GameObject se llama exactamente 'RightPanel'.", "OK");
            return;
        }

        FillPanel(rightPanel);
        ConnectManager();

        EditorUtility.DisplayDialog("¡Listo!",
            "Contenido añadido dentro de RightPanel.\n" +
            "Revisa en el Inspector que el SettingsManager tenga todas las referencias.", "OK");
    }

    // ════════════════════════════════════════════════════════════════════════════
    static void LoadAssets()
    {
        s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Pixelatus TMP.asset");

        s_toggleOn     = A<Sprite>("Assets/UI/Sprites/Toggle/toggleOn.png");
        s_toggleOff    = A<Sprite>("Assets/UI/Sprites/Toggle/toggleOff.png");
        s_sliderFill   = A<Sprite>("Assets/UI/Sprites/Sliders/slider100%.png");
        s_sliderHandle = A<Sprite>("Assets/UI/Sprites/Sliders/sliderButton.png");
        s_tabNorm      = A<Sprite>("Assets/UI/Sprites/Buttons/settingsButtonsNormal.png");
        s_tabActive    = A<Sprite>("Assets/UI/Sprites/Buttons/settingsButtons.png");
    }

    static T A<T>(string path) where T : Object
    {
        var r = AssetDatabase.LoadAssetAtPath<T>(path);
        if (r == null) Debug.LogWarning($"[Eldoria] No encontrado: {path}");
        return r;
    }

    // ── Busca los botones existentes por nombre ───────────────────────────────────
    static void CollectButtons()
    {
        s_bGraf   = Btn("GraphicsButton");
        s_bSon    = Btn("AudioButton");
        s_bCtrl   = Btn("ControlButton");
        s_bAjust  = Btn("SettingsButton");
        s_backBtn = Btn("ExitButton");
    }

    static Button Btn(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) { Debug.LogWarning($"[Eldoria] No encontrado en escena: '{name}'"); return null; }
        var b = go.GetComponent<Button>();
        if (b == null) Debug.LogWarning($"[Eldoria] '{name}' no tiene componente Button.");
        return b;
    }

    static Transform FindRightPanel()
    {
        var go = GameObject.Find("RightPanel");
        return go != null ? go.transform : null;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // RELLENAR EL PANEL DERECHO
    // ════════════════════════════════════════════════════════════════════════════
    static void FillPanel(Transform rightPanel)
    {
        // Recoger el título TMP existente (hijo directo de RightPanel)
        s_panelTitle = null;
        Transform titleTransform = null;
        foreach (Transform child in rightPanel)
        {
            var tmp = child.GetComponent<TMP_Text>();
            if (tmp != null) { s_panelTitle = tmp; titleTransform = child; break; }
        }

        // Borrar hijos que NO sean el título (ContentArea viejo si existe)
        var toDelete = new List<Transform>();
        foreach (Transform child in rightPanel)
        {
            if (child == titleTransform) continue;
            if (child.name == "ContentArea") toDelete.Add(child);
        }
        foreach (var t in toDelete) Undo.DestroyObjectImmediate(t.gameObject);

        // Si no hay título, creamos uno
        if (s_panelTitle == null)
        {
            var titleGO = GO("PanelTitle", rightPanel);
            TopStrip(titleGO, 60, 14, 14);
            s_panelTitle = TXT(titleGO, "GRÁFICOS", 26, TextAlignmentOptions.Center, FontStyles.Bold);
        }
        else
        {
            // Actualizar el texto del título existente
            s_panelTitle.text = "GRÁFICOS";
        }

        // Área de contenido — debajo del título existente
        // Calcula el espacio que ocupa el título para no solaparse
        float titleHeight = 60f;
        if (titleTransform != null)
        {
            var tRT = titleTransform.GetComponent<RectTransform>();
            if (tRT != null) titleHeight = Mathf.Abs(tRT.offsetMin.y > 0 ? tRT.offsetMin.y : tRT.sizeDelta.y) + 10f;
            if (titleHeight < 40f || titleHeight > 200f) titleHeight = 60f; // fallback
        }

        var contentGO = GO("ContentArea", rightPanel);
        var cRT = RT(contentGO);
        cRT.anchorMin = Vector2.zero;
        cRT.anchorMax = Vector2.one;
        cRT.pivot     = new Vector2(0.5f, 0.5f);
        cRT.offsetMin = new Vector2(12f, 12f);
        cRT.offsetMax = new Vector2(-12f, -(titleHeight + 8f));

        // Cuatro paneles superpuestos — State Machine
        s_pGraf  = GraficosPanel(contentGO.transform);
        s_pSon   = SonidoPanel(contentGO.transform);
        s_pCtrl  = ControlesPanel(contentGO.transform);
        s_pAjust = AjustesPanel(contentGO.transform);

        s_pSon.SetActive(false);
        s_pCtrl.SetActive(false);
        s_pAjust.SetActive(false);
    }

    // ── Paneles de contenido ─────────────────────────────────────────────────────
    static GameObject GraficosPanel(Transform p)
    {
        var panel = ContentPanel("GraficosPanel", p);
        DropdownRow(panel.transform, "Resolución",             out s_resDD);
        DropdownRow(panel.transform, "Calidad gráfica",        out s_qualDD);
        ToggleRow(panel.transform,   "Pantalla completa",      out s_fullscr);
        ToggleRow(panel.transform,   "VSync",                  out s_vsync);
        return panel;
    }

    static GameObject SonidoPanel(Transform p)
    {
        var panel = ContentPanel("SonidoPanel", p);
        SliderRow(panel.transform, "Música",  out s_musicSl, out s_muteM);
        SliderRow(panel.transform, "Efectos", out s_sfxSl,   out s_muteSFX);
        return panel;
    }

    static GameObject ControlesPanel(Transform p)
    {
        var panel = ContentPanel("ControlesPanel", p);
        var info = GO("Info", panel.transform);
        Stretch(info, 0, 0, 0, 0);
        var t = TXT(info, "Haz clic en un botón para reasignar la tecla.\nESC cancela.",
                    16, TextAlignmentOptions.Center, FontStyles.Normal);
        t.enableWordWrapping = true;
        return panel;
    }

    static GameObject AjustesPanel(Transform p)
    {
        var panel = ContentPanel("AjustesPanel", p);
        DropdownRow(panel.transform, "Idioma", out s_langDD);
        if (s_langDD != null)
        {
            s_langDD.ClearOptions();
            s_langDD.AddOptions(new List<string> { "Español", "English" });
        }
        return panel;
    }

    // ── Filas ────────────────────────────────────────────────────────────────────
    static void DropdownRow(Transform p, string label, out TMP_Dropdown dd)
    {
        var row = MakeRow(p, label);
        dd = MakeDropdown("DD", row.transform);
        dd.gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;
    }

    static void ToggleRow(Transform p, string label, out Toggle tog)
    {
        var row = MakeRow(p, label);
        tog = MakeToggle("Tog", row.transform);
        tog.gameObject.AddComponent<LayoutElement>().preferredWidth = 52f;
    }

    static void SliderRow(Transform p, string label, out Slider slider, out Toggle mute)
    {
        var wrap = GO("SliderWrap_" + label, p);
        wrap.AddComponent<LayoutElement>().preferredHeight = 88f;
        var wVLG = wrap.AddComponent<VerticalLayoutGroup>();
        wVLG.childForceExpandWidth  = true;
        wVLG.childForceExpandHeight = false;
        wVLG.spacing = 6f;

        var top = GO("TopRow", wrap.transform);
        top.AddComponent<LayoutElement>().preferredHeight = 32f;
        var tHLG = top.AddComponent<HorizontalLayoutGroup>();
        tHLG.childForceExpandWidth  = false;
        tHLG.childForceExpandHeight = true;
        tHLG.spacing = 8f;

        var lbl = GO("Lbl", top.transform);
        lbl.AddComponent<LayoutElement>().flexibleWidth = 1f;
        TXT(lbl, label, 17, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        mute = MakeToggle("Mute", top.transform);
        mute.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;

        var slGO = DefaultControls.CreateSlider(new DefaultControls.Resources());
        slGO.name = "Slider_" + label;
        Undo.RegisterCreatedObjectUndo(slGO, "Slider");
        slGO.transform.SetParent(wrap.transform, false);
        slider = slGO.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        slGO.AddComponent<LayoutElement>().preferredHeight = 38f;
        var fill   = slGO.transform.Find("Fill Area/Fill");
        var handle = slGO.transform.Find("Handle Slide Area/Handle");
        if (fill   && s_sliderFill)   fill.GetComponent<Image>().sprite   = s_sliderFill;
        if (handle && s_sliderHandle) handle.GetComponent<Image>().sprite = s_sliderHandle;
    }

    static GameObject MakeRow(Transform p, string label)
    {
        var row = GO("Row_" + label, p);
        row.AddComponent<LayoutElement>().preferredHeight = ROW_H;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 14;
        hlg.padding = new RectOffset(4, 4, 0, 0);

        var lbl = GO("Lbl", row.transform);
        lbl.AddComponent<LayoutElement>().flexibleWidth = 1f;
        TXT(lbl, label, 17, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        return row;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CONECTAR SETTINGSMANAGER
    // ════════════════════════════════════════════════════════════════════════════
    static void ConnectManager()
    {
        var smGO = GameObject.Find("SettingsManager");
        if (smGO == null)
        {
            smGO = new GameObject("SettingsManager");
            Undo.RegisterCreatedObjectUndo(smGO, "Create SettingsManager");
        }

        var sm = smGO.GetComponent<SettingsManager>() ?? smGO.AddComponent<SettingsManager>();
        var so = new SerializedObject(sm);

        SR(so, "panelTitleLabel",      s_panelTitle);
        SR(so, "graficosPanel",        s_pGraf);
        SR(so, "sonidoPanel",          s_pSon);
        SR(so, "controlesPanel",       s_pCtrl);
        SR(so, "ajustesPanel",         s_pAjust);
        SR(so, "graficosTabButton",    s_bGraf);
        SR(so, "sonidoTabButton",      s_bSon);
        SR(so, "controlesTabButton",   s_bCtrl);
        SR(so, "ajustesTabButton",     s_bAjust);
        SR(so, "backButton",           s_backBtn);
        SR(so, "tabActiveSprite",      s_tabActive);
        SR(so, "tabNormalSprite",      s_tabNorm);
        SR(so, "musicSlider",          s_musicSl);
        SR(so, "sfxSlider",            s_sfxSl);
        SR(so, "muteMusicToggle",      s_muteM);
        SR(so, "muteSFXToggle",        s_muteSFX);
        SR(so, "fullscreenToggle",     s_fullscr);
        SR(so, "vsyncToggle",          s_vsync);
        SR(so, "resolutionDropdown",   s_resDD);
        SR(so, "qualityDropdown",      s_qualDD);
        SR(so, "languageDropdown",     s_langDD);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sm);
    }

    static void SR(SerializedObject so, string field, Object val)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = val;
        else Debug.LogWarning($"[Eldoria] Campo no encontrado en SettingsManager: '{field}'");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CONTROLES
    // ════════════════════════════════════════════════════════════════════════════
    static TMP_Dropdown MakeDropdown(string name, Transform p)
    {
        var root = GO(name, p);
        root.AddComponent<Image>().color = new Color(0.15f, 0.08f, 0.03f, 0.95f);
        var dd = root.AddComponent<TMP_Dropdown>();

        var cap = GO("Label", root.transform);
        Stretch(cap, 8, 2, 28, 2);
        dd.captionText = TXT(cap, "—", 14, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);

        var arr = GO("Arrow", root.transform);
        arr.AddComponent<Image>().color = (Color)C_TEXT;
        var aRT = RT(arr);
        aRT.anchorMin = aRT.anchorMax = new Vector2(1, 0.5f);
        aRT.pivot = new Vector2(1, 0.5f);
        aRT.anchoredPosition = new Vector2(-8, 0);
        aRT.sizeDelta = new Vector2(16, 16);

        var tmpl = GO("Template", root.transform);
        tmpl.AddComponent<Image>().color = new Color(0.14f, 0.08f, 0.03f, 0.98f);
        var tRT = RT(tmpl);
        tRT.anchorMin = new Vector2(0, 0);
        tRT.anchorMax = new Vector2(1, 0);
        tRT.pivot = new Vector2(0.5f, 1);
        tRT.anchoredPosition = new Vector2(0, 2);
        tRT.sizeDelta = new Vector2(0, 180);
        tmpl.SetActive(false);
        var scroll = tmpl.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var vp = GO("Viewport", tmpl.transform);
        Stretch(vp, 0, 0, 0, 0);
        vp.AddComponent<Image>();
        vp.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = RT(vp);

        var cnt = GO("Content", vp.transform);
        var cRT = RT(cnt);
        cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = Vector2.zero;
        scroll.content = cRT;

        var item = GO("Item", cnt.transform);
        var iTog = item.AddComponent<Toggle>();
        var iRT  = RT(item);
        iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(1, 0.5f);
        iRT.pivot = new Vector2(0.5f, 0.5f); iRT.sizeDelta = new Vector2(0, 30);

        var ibg = GO("Item Background", item.transform);
        ibg.AddComponent<Image>().color = new Color(0.25f, 0.14f, 0.06f, 0.9f);
        Stretch(ibg, 0, 0, 0, 0);
        iTog.targetGraphic = ibg.GetComponent<Image>();

        var ichk = GO("Item Checkmark", item.transform);
        var ichkImg = ichk.AddComponent<Image>();
        ichkImg.color = (Color)C_TEXT;
        var ckRT = RT(ichk);
        ckRT.anchorMin = ckRT.anchorMax = new Vector2(0, 0.5f);
        ckRT.pivot = new Vector2(0.5f, 0.5f);
        ckRT.anchoredPosition = new Vector2(12, 0); ckRT.sizeDelta = new Vector2(16, 16);
        iTog.graphic = ichkImg;

        var ilbl = GO("Item Label", item.transform);
        Stretch(ilbl, 24, 1, 4, 2);
        dd.itemText = TXT(ilbl, "Opción", 13, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        dd.template = tRT;
        return dd;
    }

    static Toggle MakeToggle(string name, Transform p)
    {
        var go  = GO(name, p);
        var tog = go.AddComponent<Toggle>();
        var bg  = GO("Bg", go.transform);
        Stretch(bg, 0, 0, 0, 0);
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = s_toggleOff; bgImg.type = Image.Type.Simple;
        tog.targetGraphic = bgImg;
        var chk = GO("Chk", bg.transform);
        Stretch(chk, 0, 0, 0, 0);
        var chkImg = chk.AddComponent<Image>();
        chkImg.sprite = s_toggleOn; chkImg.type = Image.Type.Simple;
        tog.graphic = chkImg;
        tog.isOn = false;
        return tog;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // HELPERS DE LAYOUT
    // ════════════════════════════════════════════════════════════════════════════
    static GameObject GO(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static RectTransform RT(GameObject go) =>
        go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();

    static void Stretch(GameObject go, float l, float b, float r, float t)
    {
        var rt = RT(go);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }

    static void TopStrip(GameObject go, float height, float padL = 0, float padR = 0)
    {
        var rt = RT(go);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(padL, -height); rt.offsetMax = new Vector2(-padR, 0);
    }

    static GameObject ContentPanel(string name, Transform p)
    {
        var go = GO(name, p);
        Stretch(go, 0, 0, 0, 0);
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 6; vlg.padding = new RectOffset(8, 8, 8, 8);
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        return go;
    }

    static TextMeshProUGUI TXT(GameObject go, string text, float size,
        TextAlignmentOptions align, FontStyles style)
    {
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = (Color)C_TEXT;
        tmp.fontStyle = style; tmp.alignment = align;
        tmp.outlineWidth = OUTLINE_W; tmp.outlineColor = C_OUTLINE;
        tmp.enableWordWrapping = false;
        if (s_font != null) tmp.font = s_font;
        return tmp;
    }
}
#endif
