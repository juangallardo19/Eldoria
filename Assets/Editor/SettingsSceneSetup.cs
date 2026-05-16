#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// Unity menu → Eldoria → Setup Settings Scene
/// Posicionamiento 100% manual con anchors — sin Layout Groups en el nivel estructural.
/// Reversión: Ctrl+Z  o  File → Revert (guarda antes con Ctrl+S).
public static class SettingsSceneSetup
{
    // ── Paleta ───────────────────────────────────────────────────────────────────
    static readonly Color32 C_TEXT    = new Color32(232, 222, 200, 255);
    static readonly Color32 C_OUTLINE = new Color32(0, 0, 0, 255);
    static readonly Color   C_MAIN    = new Color(0.10f, 0.06f, 0.02f, 0.88f);
    const float OUTLINE_W = 0.22f;

    // ── Medidas (en píxeles a 1920×1080) ────────────────────────────────────────
    const float MAIN_W  = 1680f;
    const float MAIN_H  = 860f;
    const float LEFT_W  = 300f;
    const float GAP     = 14f;
    const float TITLE_H = 65f;
    const float BOT_H   = 82f;
    const float PAD     = 14f;
    const float TAB_H   = 72f;
    const float TAB_SP  = 10f;
    const float ROW_H   = 60f;

    // ── Assets ───────────────────────────────────────────────────────────────────
    static TMP_FontAsset s_font;
    static Sprite s_container, s_tabNorm, s_tabHover, s_tabPress, s_tabActive;
    static Sprite s_btnNorm, s_btnHover, s_btnPress;
    static Sprite s_toggleOn, s_toggleOff, s_sliderFill, s_sliderHandle;
    static RenderTexture s_videoRT;

    // ── Referencias para SettingsManager ─────────────────────────────────────────
    static TMP_Text     s_panelTitle;
    static GameObject   s_pGraf, s_pSon, s_pCtrl, s_pJug, s_pCred;
    static Button       s_bGraf, s_bSon, s_bCtrl, s_bJug, s_bCred;
    static Slider       s_musicSl, s_sfxSl;
    static Toggle       s_muteM, s_muteSFX, s_fullscr, s_vsync;
    static TMP_Dropdown s_resDD, s_qualDD;
    static Button       s_applyBtn, s_resetBtn, s_backBtn;

    // ════════════════════════════════════════════════════════════════════════════
    [MenuItem("Eldoria/Setup Settings Scene")]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog("Setup Settings Scene",
            "Borra y recrea la UI de Settings.\n" +
            "Guarda primero (Ctrl+S) para poder hacer File → Revert si algo falla.\n\n¿Continuar?",
            "Sí", "Cancelar")) return;

        LoadAssets();
        ClearExisting();
        Build();
        EditorUtility.DisplayDialog("¡Listo!",
            "Escena lista. Guarda con Ctrl+S.\n" +
            "Puedes mover y ajustar todo desde el Inspector.", "OK");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // ASSETS
    // ════════════════════════════════════════════════════════════════════════════
    static void LoadAssets()
    {
        const string fp = "Assets/UI/Fonts/Pixelatus TMP.asset";
        s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fp);
        if (s_font == null)
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Pixelatus.ttf");
            if (ttf != null)
            {
                s_font = TMP_FontAsset.CreateFontAsset(ttf);
                AssetDatabase.CreateAsset(s_font, fp);
                AssetDatabase.SaveAssets();
                Debug.Log("[Eldoria] Creado Pixelatus TMP.asset");
            }
            else
            {
                s_font = A<TMP_FontAsset>("Assets/UI/Fonts/MinecraftTMP.asset");
                Debug.LogWarning("[Eldoria] Pixelatus.ttf no encontrada; usando MinecraftTMP.");
            }
        }

        s_container    = A<Sprite>("Assets/UI/Sprites/Containers/Container1.png");
        s_tabNorm      = A<Sprite>("Assets/UI/Sprites/Buttons/settingsButtonsNormal.png");
        s_tabHover     = A<Sprite>("Assets/UI/Sprites/Buttons/settingsButtonsHover.png");
        s_tabPress     = A<Sprite>("Assets/UI/Sprites/Buttons/settingsButtonsPress.png");
        s_tabActive    = A<Sprite>("Assets/UI/Sprites/Buttons/settingsButtons.png");
        s_btnNorm      = A<Sprite>("Assets/UI/Sprites/Buttons/normalButton.png");
        s_btnHover     = A<Sprite>("Assets/UI/Sprites/Buttons/hoverButton.png");
        s_btnPress     = A<Sprite>("Assets/UI/Sprites/Buttons/pressButton.png");
        s_toggleOn     = A<Sprite>("Assets/UI/Sprites/Toggle/toggleOn.png");
        s_toggleOff    = A<Sprite>("Assets/UI/Sprites/Toggle/toggleOff.png");
        s_sliderFill   = A<Sprite>("Assets/UI/Sprites/Sliders/slider100%.png");
        s_sliderHandle = A<Sprite>("Assets/UI/Sprites/Sliders/sliderButton.png");
        s_videoRT      = A<RenderTexture>("Assets/Video/VideoRenderTexture.renderTexture");
    }

    static T A<T>(string path) where T : Object
    {
        var r = AssetDatabase.LoadAssetAtPath<T>(path);
        if (r == null) Debug.LogWarning($"[Eldoria] No encontrado: {path}");
        return r;
    }

    static void ClearExisting()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Kill(canvas.transform, "Background");
            Kill(canvas.transform, "MainPanel");
        }
        var sm = GameObject.Find("SettingsManager");
        if (sm != null) Undo.DestroyObjectImmediate(sm);
    }

    static void Kill(Transform parent, string childName)
    {
        var t = parent.Find(childName);
        if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // BUILD
    // ════════════════════════════════════════════════════════════════════════════
    static void Build()
    {
        // ── Canvas ───────────────────────────────────────────────────────────────
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cGO = GO("Canvas", null);
            canvas = cGO.AddComponent<Canvas>();
            cGO.AddComponent<GraphicRaycaster>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;

        // ── Fondo de video — solo RawImage; VideoPlayer viene de BackgroundVideoManager ──
        var bg = GO("Background", canvas.transform);
        bg.AddComponent<RawImage>().texture = s_videoRT;
        Stretch(bg, 0, 0, 0, 0);

        // ── Panel principal centrado (1680×860) ──────────────────────────────────
        var main = GO("MainPanel", canvas.transform);
        var mRT  = RT(main);
        mRT.anchorMin = mRT.anchorMax = new Vector2(0.5f, 0.5f);
        mRT.pivot        = new Vector2(0.5f, 0.5f);
        mRT.sizeDelta    = new Vector2(MAIN_W, MAIN_H);
        mRT.anchoredPosition = Vector2.zero;
        Img(main, C_MAIN);

        // ── Columnas ─────────────────────────────────────────────────────────────
        LeftColumn(main.transform);
        RightColumn(main.transform);

        // ── Fila de acción — fuera de los containers, sobre el fondo del panel ───
        BottomRow(main.transform);

        ConnectManager();
    }

    // ════════════════════════════════════════════════════════════════════════════
    // COLUMNA IZQUIERDA
    // ════════════════════════════════════════════════════════════════════════════
    static void LeftColumn(Transform parent)
    {
        var left = GO("LeftPanel", parent);
        // Ancho fijo a la izquierda; deja espacio inferior para la fila de botones
        var lRT = RT(left);
        lRT.anchorMin = new Vector2(0, 0);
        lRT.anchorMax = new Vector2(0, 1);
        lRT.pivot     = new Vector2(0, 0.5f);
        lRT.offsetMin = new Vector2(0, BOT_H + GAP);
        lRT.offsetMax = new Vector2(LEFT_W, 0);
        Img(left, Color.white, s_container);   // sin tinte — sprite tal cual

        var title = GO("TitleLabel", left.transform);
        TopStrip(title, TITLE_H, PAD, PAD);
        TXT(title, "OPCIONES", 26, TextAlignmentOptions.Center, FontStyles.Bold);

        HLine("DivL", left.transform, TITLE_H, 2);

        var tabs = GO("TabsArea", left.transform);
        Stretch(tabs, PAD, PAD, PAD, TITLE_H + 4f);
        var vlg = tabs.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = TAB_SP;
        vlg.padding = new RectOffset(0, 0, 10, 10);

        s_bGraf = TabBtn("GraficosTab",    tabs.transform, "GRÁFICOS");
        s_bSon  = TabBtn("SonidoTab",      tabs.transform, "SONIDO");
        s_bCtrl = TabBtn("ControlesTab",   tabs.transform, "CONTROLES");
        s_bJug  = TabBtn("JugabilidadTab", tabs.transform, "JUGABILIDAD");
        s_bCred = TabBtn("CreditosTab",    tabs.transform, "CRÉDITOS");
    }

    static Button TabBtn(string name, Transform parent, string label)
    {
        var go = GO(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = TAB_H;

        var img = go.AddComponent<Image>();
        img.sprite = s_tabNorm;
        img.type   = Image.Type.Simple;
        img.color  = Color.white;

        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        btn.spriteState = new SpriteState
        {
            highlightedSprite = s_tabHover,
            pressedSprite     = s_tabPress,
            selectedSprite    = s_tabActive
        };

        var lbl = GO("Lbl", go.transform);
        Stretch(lbl, 20, 4, 4, 4);
        TXT(lbl, label, 18, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        return btn;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // COLUMNA DERECHA
    // ════════════════════════════════════════════════════════════════════════════
    static void RightColumn(Transform parent)
    {
        var right = GO("RightPanel", parent);
        // Rellena el espacio a la derecha de LeftPanel; deja espacio inferior para botones
        var rRT = RT(right);
        rRT.anchorMin = Vector2.zero;
        rRT.anchorMax = Vector2.one;
        rRT.pivot     = new Vector2(0.5f, 0.5f);
        rRT.offsetMin = new Vector2(LEFT_W + GAP, BOT_H + GAP);
        rRT.offsetMax = Vector2.zero;
        Img(right, Color.white, s_container);  // sin tinte — sprite tal cual

        var tt = GO("PanelTitle", right.transform);
        TopStrip(tt, TITLE_H, PAD, PAD);
        s_panelTitle = TXT(tt, "GRÁFICOS", 26, TextAlignmentOptions.Center, FontStyles.Bold);

        HLine("DivR", right.transform, TITLE_H, 2);

        // Área de contenido — ocupa todo el panel debajo del título
        var content = GO("ContentArea", right.transform);
        var cRT = RT(content);
        cRT.anchorMin = Vector2.zero;
        cRT.anchorMax = Vector2.one;
        cRT.pivot     = new Vector2(0.5f, 0.5f);
        cRT.offsetMin = new Vector2(PAD, PAD);
        cRT.offsetMax = new Vector2(-PAD, -(TITLE_H + 6f));

        // Cinco paneles superpuestos — solo 1 visible a la vez (State Machine)
        s_pGraf = GraficosPanel(content.transform);
        s_pSon  = SonidoPanel(content.transform);
        s_pCtrl = ControlesPanel(content.transform);
        s_pJug  = Placeholder(content.transform, "JugabilidadPanel", "Próximamente");
        s_pCred = Placeholder(content.transform, "CreditosPanel",
                              "ELDORIA  ©  2026\n\nProyecto universitario\nDiseño de Interfaces");

        s_pSon.SetActive(false);
        s_pCtrl.SetActive(false);
        s_pJug.SetActive(false);
        s_pCred.SetActive(false);
    }

    // ── Paneles de contenido ─────────────────────────────────────────────────────
    static GameObject GraficosPanel(Transform p)
    {
        var panel = ContentPanel("GraficosPanel", p);
        DropdownRow(panel.transform, "Resolución",             out s_resDD);
        DropdownRow(panel.transform, "Calidad gráfica",        out s_qualDD);
        ToggleRow(panel.transform,   "Pantalla completa",      out s_fullscr);
        ToggleRow(panel.transform,   "Sincronización vertical", out s_vsync);
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
        panel.AddComponent<KeyRebindUI>();
        var info = GO("Info", panel.transform);
        Stretch(info, 0, 0, 0, 0);
        var tmp = TXT(info, "Haz clic en un botón para reasignar la tecla.\nESC cancela.",
                      18, TextAlignmentOptions.Center, FontStyles.Normal);
        tmp.enableWordWrapping = true;
        return panel;
    }

    static GameObject Placeholder(Transform p, string name, string msg)
    {
        var panel = ContentPanel(name, p);
        var go = GO("Msg", panel.transform);
        Stretch(go, 0, 0, 0, 0);
        var tmp = TXT(go, msg, 20, TextAlignmentOptions.Center, FontStyles.Normal);
        tmp.enableWordWrapping = true;
        return panel;
    }

    // ── Filas de control ─────────────────────────────────────────────────────────
    static void DropdownRow(Transform p, string label, out TMP_Dropdown dd)
    {
        var row = MakeRow(p, label);
        dd = MakeDropdown("DD", row.transform);
        dd.gameObject.AddComponent<LayoutElement>().preferredWidth = 240;
    }

    static void ToggleRow(Transform p, string label, out Toggle tog)
    {
        var row = MakeRow(p, label);
        tog = MakeToggle("Tog", row.transform);
        tog.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;
    }

    static void SliderRow(Transform p, string label, out Slider slider, out Toggle mute)
    {
        var wrap = GO("SliderWrap_" + label, p);
        wrap.AddComponent<LayoutElement>().preferredHeight = 92;
        var wVLG = wrap.AddComponent<VerticalLayoutGroup>();
        wVLG.childForceExpandWidth  = true;
        wVLG.childForceExpandHeight = false;
        wVLG.spacing = 6;

        var top = GO("TopRow", wrap.transform);
        top.AddComponent<LayoutElement>().preferredHeight = 36;
        var tHLG = top.AddComponent<HorizontalLayoutGroup>();
        tHLG.childForceExpandWidth  = false;
        tHLG.childForceExpandHeight = true;
        tHLG.spacing = 10;

        var lbl = GO("Lbl", top.transform);
        lbl.AddComponent<LayoutElement>().flexibleWidth = 1;
        TXT(lbl, label, 18, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        mute = MakeToggle("Mute", top.transform);
        mute.gameObject.AddComponent<LayoutElement>().preferredWidth = 52;

        var slGO = DefaultControls.CreateSlider(new DefaultControls.Resources());
        slGO.name = "Slider_" + label;
        Undo.RegisterCreatedObjectUndo(slGO, "Create Slider");
        slGO.transform.SetParent(wrap.transform, false);
        slider = slGO.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        slGO.AddComponent<LayoutElement>().preferredHeight = 42;
        var fill = slGO.transform.Find("Fill Area/Fill");
        if (fill   && s_sliderFill)   fill.GetComponent<Image>().sprite   = s_sliderFill;
        var handle = slGO.transform.Find("Handle Slide Area/Handle");
        if (handle && s_sliderHandle) handle.GetComponent<Image>().sprite = s_sliderHandle;
    }

    // Fila genérica: label flexible izquierda + control fijo derecha
    static GameObject MakeRow(Transform p, string label)
    {
        var row = GO("Row_" + label, p);
        row.AddComponent<LayoutElement>().preferredHeight = ROW_H;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 16;
        hlg.padding = new RectOffset(6, 6, 0, 0);

        var lbl = GO("Lbl", row.transform);
        lbl.AddComponent<LayoutElement>().flexibleWidth = 1;
        TXT(lbl, label, 18, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        return row;
    }

    // Fila inferior — fuera de los containers, directamente sobre MainPanel
    static void BottomRow(Transform p)
    {
        var row = GO("BottomRow", p);
        var rt  = RT(row);
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot     = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(LEFT_W + GAP, 8f);
        rt.offsetMax = new Vector2(-PAD, BOT_H - 10f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 24;

        s_applyBtn = ActionBtn("ApplyBtn", row.transform, "APLICAR");
        s_resetBtn = ActionBtn("ResetBtn", row.transform, "RESTABLECER");
        s_backBtn  = ActionBtn("BackBtn",  row.transform, "VOLVER");
    }

    static Button ActionBtn(string name, Transform p, string label)
    {
        var go = GO(name, p);
        go.AddComponent<LayoutElement>().preferredWidth = 200;
        var img = go.AddComponent<Image>();
        img.sprite = s_btnNorm; img.type = Image.Type.Simple; img.color = Color.white;
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        btn.spriteState = new SpriteState { highlightedSprite = s_btnHover, pressedSprite = s_btnPress };
        var lbl = GO("Lbl", go.transform);
        Stretch(lbl, 0, 0, 0, 0);
        TXT(lbl, label, 18, TextAlignmentOptions.Center, FontStyles.Bold);
        return btn;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CONECTAR SETTINGSMANAGER
    // ════════════════════════════════════════════════════════════════════════════
    static void ConnectManager()
    {
        var smGO = new GameObject("SettingsManager");
        Undo.RegisterCreatedObjectUndo(smGO, "Create SettingsManager");
        var sm = smGO.AddComponent<SettingsManager>();
        var so = new SerializedObject(sm);

        SR(so, "panelTitleLabel",      s_panelTitle);
        SR(so, "graficosPanel",        s_pGraf);
        SR(so, "sonidoPanel",          s_pSon);
        SR(so, "controlesPanel",       s_pCtrl);
        SR(so, "jugabilidadPanel",     s_pJug);
        SR(so, "creditosPanel",        s_pCred);
        SR(so, "graficosTabButton",    s_bGraf);
        SR(so, "sonidoTabButton",      s_bSon);
        SR(so, "controlesTabButton",   s_bCtrl);
        SR(so, "jugabilidadTabButton", s_bJug);
        SR(so, "creditosTabButton",    s_bCred);
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
        SR(so, "applyButton",          s_applyBtn);
        SR(so, "resetButton",          s_resetBtn);
        SR(so, "backButton",           s_backBtn);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sm);
    }

    static void SR(SerializedObject so, string field, Object val)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = val;
        else Debug.LogWarning($"[Eldoria] Campo no encontrado: '{field}'");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CONTROLES
    // ════════════════════════════════════════════════════════════════════════════
    static TMP_Dropdown MakeDropdown(string name, Transform p)
    {
        var root = GO(name, p);
        root.AddComponent<Image>().color = new Color(0.18f, 0.10f, 0.05f, 0.96f);
        var dd = root.AddComponent<TMP_Dropdown>();

        var cap = GO("Label", root.transform);
        Stretch(cap, 8, 2, 32, 2);
        dd.captionText = TXT(cap, "—", 15, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);

        var arr = GO("Arrow", root.transform);
        arr.AddComponent<Image>().color = (Color)C_TEXT;
        var aRT = RT(arr);
        aRT.anchorMin = aRT.anchorMax = new Vector2(1, 0.5f);
        aRT.pivot = new Vector2(1, 0.5f);
        aRT.anchoredPosition = new Vector2(-10, 0);
        aRT.sizeDelta = new Vector2(18, 18);

        var tmpl = GO("Template", root.transform);
        tmpl.AddComponent<Image>().color = new Color(0.16f, 0.09f, 0.04f, 0.98f);
        var tRT = RT(tmpl);
        tRT.anchorMin = new Vector2(0, 0);
        tRT.anchorMax = new Vector2(1, 0);
        tRT.pivot = new Vector2(0.5f, 1);
        tRT.anchoredPosition = new Vector2(0, 2);
        tRT.sizeDelta = new Vector2(0, 200);
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
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = Vector2.zero;
        scroll.content = cRT;

        var item = GO("Item", cnt.transform);
        var iTog = item.AddComponent<Toggle>();
        var iRT  = RT(item);
        iRT.anchorMin = new Vector2(0, 0.5f);
        iRT.anchorMax = new Vector2(1, 0.5f);
        iRT.pivot     = new Vector2(0.5f, 0.5f);
        iRT.sizeDelta = new Vector2(0, 34);

        var ibg = GO("Item Background", item.transform);
        var ibgImg = ibg.AddComponent<Image>();
        ibgImg.color = new Color(0.28f, 0.16f, 0.07f, 0.9f);
        Stretch(ibg, 0, 0, 0, 0);
        iTog.targetGraphic = ibgImg;

        var ichk = GO("Item Checkmark", item.transform);
        var ichkImg = ichk.AddComponent<Image>();
        ichkImg.color = (Color)C_TEXT;
        var ckRT = RT(ichk);
        ckRT.anchorMin = ckRT.anchorMax = new Vector2(0, 0.5f);
        ckRT.pivot = new Vector2(0.5f, 0.5f);
        ckRT.anchoredPosition = new Vector2(14, 0);
        ckRT.sizeDelta = new Vector2(18, 18);
        iTog.graphic = ichkImg;

        var ilbl = GO("Item Label", item.transform);
        Stretch(ilbl, 28, 1, 4, 2);
        dd.itemText = TXT(ilbl, "Opción", 15, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        dd.template = tRT;
        return dd;
    }

    static Toggle MakeToggle(string name, Transform p)
    {
        var go  = GO(name, p);
        var tog = go.AddComponent<Toggle>();

        var bg = GO("Bg", go.transform);
        Stretch(bg, 0, 0, 0, 0);
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = s_toggleOff;
        bgImg.type   = Image.Type.Simple;
        tog.targetGraphic = bgImg;

        var chk = GO("Chk", bg.transform);
        Stretch(chk, 0, 0, 0, 0);
        var chkImg = chk.AddComponent<Image>();
        chkImg.sprite = s_toggleOn;
        chkImg.type   = Image.Type.Simple;
        tog.graphic   = chkImg;

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

    // Rellena el padre con padding (l=left b=bottom r=right t=top)
    static void Stretch(GameObject go, float l, float b, float r, float t)
    {
        var rt = RT(go);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    // Franja horizontal pegada al tope del padre
    static void TopStrip(GameObject go, float height, float padL = 0, float padR = 0)
    {
        var rt = RT(go);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(padL, -height);
        rt.offsetMax = new Vector2(-padR, 0);
    }

    // Línea horizontal a 'fromTop' px desde el tope
    static void HLine(string name, Transform p, float fromTop, float thickness)
    {
        var go = GO(name, p);
        var rt = RT(go);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, -(fromTop + thickness));
        rt.offsetMax = new Vector2(0, -fromTop);
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);
    }

    // Panel de contenido con VLG para apilar filas (solo para hijos de ContentArea)
    static GameObject ContentPanel(string name, Transform p)
    {
        var go = GO(name, p);
        Stretch(go, 0, 0, 0, 0);
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        return go;
    }

    static void Img(GameObject go, Color tint, Sprite sprite = null)
    {
        var img = go.AddComponent<Image>();
        img.color  = tint;
        img.sprite = sprite;
        img.type   = Image.Type.Simple;
    }

    static TextMeshProUGUI TXT(GameObject go, string text, float size,
        TextAlignmentOptions align, FontStyles style)
    {
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = size;
        tmp.color              = (Color)C_TEXT;
        tmp.fontStyle          = style;
        tmp.alignment          = align;
        tmp.outlineWidth       = OUTLINE_W;
        tmp.outlineColor       = C_OUTLINE;
        tmp.enableWordWrapping = false;
        if (s_font != null) tmp.font = s_font;
        return tmp;
    }
}
#endif
