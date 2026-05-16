#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

// Genera toda la jerarquía de la escena SlotsScreen desde el menú
// Eldoria → Setup Slots Scene. Ejecutar con la escena SlotsScreen abierta.
public static class SlotsSceneSetup
{
    // ── Dimensiones de referencia (1920×1080) ─────────────────────────────
    const float CARD_W  = 270f;
    const float CARD_H  = 420f;
    const float COL_W   = 270f;
    const float COL_H   = 490f;   // card + espacio + botón acción
    const float ACT_W   = 252f;   // ancho botón CONTINUAR / NUEVA PARTIDA
    const float ACT_H   =  65f;
    const float GBTN_W  = 240f;   // botones globales
    const float GBTN_H  =  65f;
    const float ROW_GAP =  28f;   // separación entre tarjetas

    [MenuItem("Eldoria/Setup Slots Scene")]
    public static void Execute()
    {
        // ── Activos ───────────────────────────────────────────────────────
        var font    = Load<TMP_FontAsset>("Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
        var sTitle  = Load<Sprite>("Assets/UI/Sprites/NewGame/NewGameContainerTittle.png");
        var sEmN    = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyNormal.png");
        var sEmH    = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyHover.png");
        var sEmP    = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyPress.png");
        var sFilN   = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledNormal.png");
        var sFilH   = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledHover.png");
        var sFilP   = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledPress.png");
        var sBtnN   = Load<Sprite>("Assets/UI/Sprites/NewGame/ButonNewPartyNormal.png");
        var sBtnH   = Load<Sprite>("Assets/UI/Sprites/NewGame/ButonNewPartyHover.png");
        var sBtnP   = Load<Sprite>("Assets/UI/Sprites/NewGame/ButonNewPartyPress.png");
        var sCont   = Load<Sprite>("Assets/UI/Sprites/Containers/Container1.png");
        var bgClip  = Load<VideoClip>("Assets/UI/Sprites/NewGame/BgSlots.mp4");

        // ── Limpiar Canvas existente ──────────────────────────────────────
        var existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null)
        {
            while (existingCanvas.transform.childCount > 0)
                Object.DestroyImmediate(existingCanvas.transform.GetChild(0).gameObject);
        }

        // ── Canvas ────────────────────────────────────────────────────────
        var canvasGo = existingCanvas ?? new GameObject("Canvas");
        var canvas   = canvasGo.GetComponent<Canvas>() ?? canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if (!canvasGo.TryGetComponent<CanvasScaler>(out var scaler))
            scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        if (!canvasGo.GetComponent<GraphicRaycaster>())
            canvasGo.AddComponent<GraphicRaycaster>();

        // ── SlotsScreenManager ────────────────────────────────────────────
        if (!canvasGo.TryGetComponent<SlotsScreenManager>(out var mgr))
            mgr = canvasGo.AddComponent<SlotsScreenManager>();

        // ── Cámara principal ──────────────────────────────────────────────
        var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        if (!camGo.TryGetComponent<Camera>(out var cam))
            cam = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic    = true;
        cam.depth           = -1;
        if (!camGo.GetComponent<AudioListener>())
            camGo.AddComponent<AudioListener>();

        // ── Video Player ──────────────────────────────────────────────────
        var vpGo = GameObject.Find("VideoPlayer") ?? new GameObject("VideoPlayer");
        if (!vpGo.TryGetComponent<VideoPlayer>(out var vp))
            vp = vpGo.AddComponent<VideoPlayer>();
        vp.clip           = bgClip;
        vp.renderMode     = VideoRenderMode.CameraFarPlane;
        vp.targetCamera   = cam;
        vp.isLooping      = true;
        vp.playOnAwake    = true;
        vp.audioOutputMode = VideoAudioOutputMode.None;

        // ── Título ────────────────────────────────────────────────────────
        var titleGo = MakeImage(canvasGo.transform, "TitleBar", sTitle);
        SetRT(titleGo, 0, 440, 920, 110);

        // ── Fila de slots ─────────────────────────────────────────────────
        float rowW = COL_W * 4 + ROW_GAP * 3;
        var rowGo  = MakeEmpty(canvasGo.transform, "SlotsRow");
        SetRT(rowGo, 0, 30, rowW, COL_H);
        var hLG = rowGo.AddComponent<HorizontalLayoutGroup>();
        hLG.childAlignment        = TextAnchor.MiddleCenter;
        hLG.spacing               = ROW_GAP;
        hLG.childForceExpandWidth  = false;
        hLG.childForceExpandHeight = false;
        hLG.childControlWidth      = false;
        hLG.childControlHeight     = false;

        // ── 4 tarjetas ────────────────────────────────────────────────────
        var slotUIs = new SlotsScreenManager.SlotUI[4];
        for (int i = 0; i < 4; i++)
            slotUIs[i] = BuildSlot(rowGo.transform, i, font,
                sEmN, sEmH, sEmP, sFilN, sFilH, sFilP, sBtnN, sBtnH, sBtnP);

        // ── Botones globales ──────────────────────────────────────────────
        var gbRow = MakeEmpty(canvasGo.transform, "GlobalButtons");
        SetRT(gbRow, 0, -435, 860, GBTN_H);
        var gLG = gbRow.AddComponent<HorizontalLayoutGroup>();
        gLG.childAlignment        = TextAnchor.MiddleCenter;
        gLG.spacing               = 40;
        gLG.childForceExpandWidth  = false;
        gLG.childForceExpandHeight = false;
        gLG.childControlWidth      = false;
        gLG.childControlHeight     = false;

        var backGo   = MakeButton(gbRow.transform, "BackButton",   "VOLVER",      sBtnN, sBtnH, sBtnP, font, GBTN_W, GBTN_H);
        var deleteGo = MakeButton(gbRow.transform, "DeleteButton",  "BORRAR",      sBtnN, sBtnH, sBtnP, font, GBTN_W, GBTN_H);
        var selectGo = MakeButton(gbRow.transform, "SelectButton",  "SELECCIONAR", sBtnN, sBtnH, sBtnP, font, 280,    GBTN_H);

        // ── Panel de confirmación borrar ──────────────────────────────────
        var cpGo  = MakeEmpty(canvasGo.transform, "DeleteConfirmPanel");
        SetRT(cpGo, 0, 0, 680, 320);
        var cpImg = cpGo.AddComponent<Image>();
        cpImg.sprite = sCont;
        cpImg.type   = Image.Type.Sliced;

        var cpTxt = MakeTMP(cpGo.transform, "ConfirmText",
            "¿Borrar esta partida?\nEsta acción no se puede deshacer.", font, 22);
        SetRT(cpTxt.gameObject, 0, 55, 580, 120);
        cpTxt.alignment = TextAlignmentOptions.Center;
        cpTxt.color     = Color.white;

        var cpYes = MakeButton(cpGo.transform, "ConfirmYes", "SÍ",  sBtnN, sBtnH, sBtnP, font, 180, 65);
        var cpNo  = MakeButton(cpGo.transform, "ConfirmNo",  "NO",  sBtnN, sBtnH, sBtnP, font, 180, 65);
        SetRT(cpYes, -110, -100, 180, 65);
        SetRT(cpNo,   110, -100, 180, 65);

        cpGo.SetActive(false);

        // ── Cablear SlotsScreenManager ────────────────────────────────────
        var so = new SerializedObject(mgr);

        var slotsProp = so.FindProperty("slots");
        slotsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            var e = slotsProp.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("selectionHighlight").objectReferenceValue = slotUIs[i].selectionHighlight;
            e.FindPropertyRelative("emptyState")        .objectReferenceValue = slotUIs[i].emptyState;
            e.FindPropertyRelative("occupiedState")     .objectReferenceValue = slotUIs[i].occupiedState;
            e.FindPropertyRelative("slotNumberText")    .objectReferenceValue = slotUIs[i].slotNumberText;
            e.FindPropertyRelative("zoneText")          .objectReferenceValue = slotUIs[i].zoneText;
            e.FindPropertyRelative("playTimeText")      .objectReferenceValue = slotUIs[i].playTimeText;
            e.FindPropertyRelative("cardButton")        .objectReferenceValue = slotUIs[i].cardButton;
            e.FindPropertyRelative("newGameButton")     .objectReferenceValue = slotUIs[i].newGameButton;
            e.FindPropertyRelative("continueButton")    .objectReferenceValue = slotUIs[i].continueButton;
        }

        so.FindProperty("backButton")        .objectReferenceValue = backGo.GetComponent<Button>();
        so.FindProperty("deleteButton")      .objectReferenceValue = deleteGo.GetComponent<Button>();
        so.FindProperty("selectButton")      .objectReferenceValue = selectGo.GetComponent<Button>();
        so.FindProperty("deleteConfirmPanel").objectReferenceValue = cpGo;
        so.FindProperty("confirmDeleteYes")  .objectReferenceValue = cpYes.GetComponent<Button>();
        so.FindProperty("confirmDeleteNo")   .objectReferenceValue = cpNo.GetComponent<Button>();
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SlotsSceneSetup] Escena generada. Guarda con Ctrl+S.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Construye una columna de slot (tarjeta + overlay + estados)
    // ─────────────────────────────────────────────────────────────────────
    static SlotsScreenManager.SlotUI BuildSlot(
        Transform parent, int idx, TMP_FontAsset font,
        Sprite sEmN, Sprite sEmH, Sprite sEmP,
        Sprite sFilN, Sprite sFilH, Sprite sFilP,
        Sprite sBtnN, Sprite sBtnH, Sprite sBtnP)
    {
        var ui  = new SlotsScreenManager.SlotUI();
        int num = idx + 1;

        // Raíz del slot (solo layout, sin imagen)
        var col = MakeEmpty(parent, $"Slot{num}");
        col.GetComponent<RectTransform>().sizeDelta = new Vector2(COL_W, COL_H);

        // Posición local de la tarjeta dentro de la columna
        // La tarjeta ocupa la parte superior; el botón asoma por debajo
        float cardY = (COL_H - CARD_H) * 0.5f - 5f;  // ~30

        // ── CardButton (invisible, captura clics para selección) ──────────
        var cbGo  = MakeEmpty(col.transform, "CardButton");
        SetRT(cbGo, 0, cardY, CARD_W, CARD_H);
        var cbImg = cbGo.AddComponent<Image>();
        cbImg.color          = Color.clear;
        cbImg.raycastTarget  = true;
        var cb               = cbGo.AddComponent<Button>();
        cb.targetGraphic     = cbImg;
        cb.transition        = Selectable.Transition.None;
        ui.cardButton        = cb;

        // ── SelectionHighlight (overlay azul, desactivado por defecto) ────
        var hl = MakeImage(col.transform, "SelectionHighlight", sFilH);
        SetRT(hl, 0, cardY, CARD_W + 8, CARD_H + 8);
        var hlImg = hl.GetComponent<Image>();
        hlImg.color         = new Color(0.25f, 0.45f, 1f, 0.7f);
        hlImg.raycastTarget = false;
        hlImg.type          = Image.Type.Simple;
        hl.SetActive(false);
        ui.selectionHighlight = hl;

        // ── EmptyState ────────────────────────────────────────────────────
        var emp = MakeEmpty(col.transform, "EmptyState");
        SetRT(emp, 0, cardY, CARD_W, CARD_H);
        ui.emptyState = emp;

        //   Fondo de tarjeta vacía (no recibe raycasts para que pasen al CardButton)
        var empBg = MakeImage(emp.transform, "CardBg", sEmN);
        Stretch(empBg);
        empBg.GetComponent<Image>().raycastTarget = false;

        //   Signo "?"
        var qTMP = MakeTMP(emp.transform, "QuestionMark", "?", font, 110);
        SetRT(qTMP.gameObject, 0, 55, 200, 180);
        qTMP.alignment      = TextAlignmentOptions.Center;
        qTMP.color          = new Color(0.6f, 0.42f, 0.1f, 0.55f);
        qTMP.raycastTarget  = false;

        //   Número de slot
        var numTMP = MakeTMP(emp.transform, "SlotNumber", num.ToString(), font, 26);
        SetRT(numTMP.gameObject, 0, -150, 200, 50);
        numTMP.alignment     = TextAlignmentOptions.Center;
        numTMP.color         = new Color(0.9f, 0.85f, 0.65f, 1f);
        numTMP.raycastTarget = false;
        ui.slotNumberText    = numTMP;

        //   Botón NUEVA PARTIDA (sobresale por debajo de la tarjeta)
        var ngGo = MakeButton(emp.transform, "NewGameButton",
            "NUEVA PARTIDA", sBtnN, sBtnH, sBtnP, font, ACT_W, ACT_H);
        SetRT(ngGo, 0, -(CARD_H * 0.5f + ACT_H * 0.5f + 6), ACT_W, ACT_H);
        ui.newGameButton = ngGo.GetComponent<Button>();

        // ── OccupiedState (oculto por defecto) ────────────────────────────
        var occ = MakeEmpty(col.transform, "OccupiedState");
        SetRT(occ, 0, cardY, CARD_W, CARD_H);
        occ.SetActive(false);
        ui.occupiedState = occ;

        //   Fondo de tarjeta con partida
        var occBg = MakeImage(occ.transform, "CardBg", sFilN);
        Stretch(occBg);
        occBg.GetComponent<Image>().raycastTarget = false;

        //   Imagen de personaje (placeholder dorado semitransparente)
        var charGo = MakeImage(occ.transform, "CharacterImage", null);
        SetRT(charGo, 0, 52, 200, 260);
        charGo.GetComponent<Image>().color         = new Color(0.75f, 0.55f, 0.15f, 0.28f);
        charGo.GetComponent<Image>().raycastTarget = false;

        //   Zona
        var zoneTMP = MakeTMP(occ.transform, "ZoneText", "Inicio", font, 20);
        SetRT(zoneTMP.gameObject, 0, -138, 248, 40);
        zoneTMP.alignment     = TextAlignmentOptions.Center;
        zoneTMP.color         = Color.white;
        zoneTMP.raycastTarget = false;
        ui.zoneText           = zoneTMP;

        //   Tiempo
        var timeTMP = MakeTMP(occ.transform, "TimeText", "00:00:00", font, 20);
        SetRT(timeTMP.gameObject, 0, -178, 200, 40);
        timeTMP.alignment     = TextAlignmentOptions.Center;
        timeTMP.color         = new Color(0.9f, 0.85f, 0.45f, 1f);
        timeTMP.raycastTarget = false;
        ui.playTimeText       = timeTMP;

        //   Botón CONTINUAR
        var cntGo = MakeButton(occ.transform, "ContinueButton",
            "CONTINUAR", sBtnN, sBtnH, sBtnP, font, ACT_W, ACT_H);
        SetRT(cntGo, 0, -(CARD_H * 0.5f + ACT_H * 0.5f + 6), ACT_W, ACT_H);
        ui.continueButton = cntGo.GetComponent<Button>();

        return ui;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static T Load<T>(string path) where T : Object =>
        AssetDatabase.LoadAssetAtPath<T>(path);

    static GameObject MakeEmpty(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject MakeImage(Transform parent, string name, Sprite sprite)
    {
        var go  = MakeEmpty(parent, name);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        return go;
    }

    static GameObject MakeButton(Transform parent, string name, string label,
        Sprite normal, Sprite hover, Sprite press,
        TMP_FontAsset font, float w, float h)
    {
        var go  = MakeImage(parent, name, normal);
        SetRT(go, 0, 0, w, h);
        var img = go.GetComponent<Image>();
        img.type = Image.Type.Simple;
        var btn  = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.SpriteSwap;
        var ss = btn.spriteState;
        ss.highlightedSprite = hover;
        ss.pressedSprite     = press;
        btn.spriteState      = ss;

        var lbl = MakeTMP(go.transform, "Label", label, font, 19);
        Stretch(lbl.gameObject);
        lbl.alignment    = TextAlignmentOptions.Center;
        lbl.color        = Color.white;
        lbl.raycastTarget = false;

        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        TMP_FontAsset font, float size)
    {
        var go  = MakeEmpty(parent, name);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.font     = font;
        tmp.fontSize = size;
        tmp.color    = Color.white;
        return tmp;
    }

    // RectTransform con anchor central
    static void SetRT(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // Stretch para rellenar el padre
    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
#endif
