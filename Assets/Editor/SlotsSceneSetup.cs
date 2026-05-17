#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

// Genera y recablea la jerarquía de la escena SlotsScreen.
// Menú Eldoria → Setup Slots Scene   (reconstruye todo desde cero)
// Menú Eldoria → Wire All Slots References  (solo recablea refs + sprites, sin tocar la jerarquía)
public static class SlotsSceneSetup
{
    const float CARD_W  = 270f;
    const float CARD_H  = 420f;
    const float COL_W   = 270f;
    const float COL_H   = 490f;
    const float GBTN_W  = 240f;
    const float GBTN_H  =  65f;
    const float ROW_GAP =  28f;

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Eldoria/Setup Slots Scene")]
    public static void Execute()
    {
        var font   = Load<TMP_FontAsset>("Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
        var sTitle = Load<Sprite>("Assets/UI/Sprites/NewGame/NewGameContainerTittle.png");
        var sEmN   = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyNormal.png");
        var sEmH   = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyHover.png");
        var sEmP   = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyPress.png");
        var sFilN  = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledNormal.png");
        var sFilH  = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledHover.png");
        var sFilP  = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledPress.png");
        var sBtnN  = Load<Sprite>("Assets/UI/Sprites/NewGame/ButonNewPartyNormal.png");
        var sBtnH  = Load<Sprite>("Assets/UI/Sprites/NewGame/ButonNewPartyHover.png");
        var sBtnP  = Load<Sprite>("Assets/UI/Sprites/NewGame/ButonNewPartyPress.png");
        var sCont  = Load<Sprite>("Assets/UI/Sprites/Containers/Container1.png");
        var bgClip = Load<VideoClip>("Assets/UI/Sprites/NewGame/BgSlots.mp4");
        var amClip = Load<AudioClip>("Assets/UI/Sprites/NewGame/Ambience Cave Sound Effect.mp3");

        // Limpiar Canvas existente
        var existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null)
            while (existingCanvas.transform.childCount > 0)
                Object.DestroyImmediate(existingCanvas.transform.GetChild(0).gameObject);

        var canvasGo = existingCanvas != null ? existingCanvas : new GameObject("Canvas");
        var canvas   = canvasGo.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if (!canvasGo.TryGetComponent<CanvasScaler>(out var scaler))
            scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        if (!canvasGo.GetComponent<GraphicRaycaster>())
            canvasGo.AddComponent<GraphicRaycaster>();

        if (!canvasGo.TryGetComponent<SlotsScreenManager>(out var mgr))
            mgr = canvasGo.AddComponent<SlotsScreenManager>();

        // Cámara
        var camGo = GameObject.Find("Main Camera");
        if (camGo == null) camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        if (!camGo.TryGetComponent<Camera>(out var cam))
            cam = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic    = true;
        cam.depth           = -1;
        if (!camGo.GetComponent<AudioListener>())
            camGo.AddComponent<AudioListener>();

        // VideoPlayer
        var vpGo = GameObject.Find("VideoPlayer");
        if (vpGo == null) vpGo = new GameObject("VideoPlayer");
        if (!vpGo.TryGetComponent<VideoPlayer>(out var vp))
            vp = vpGo.AddComponent<VideoPlayer>();
        vp.clip            = bgClip;
        vp.renderMode      = VideoRenderMode.CameraFarPlane;
        vp.targetCamera    = cam;
        vp.isLooping       = true;
        vp.playOnAwake     = true;
        vp.audioOutputMode = VideoAudioOutputMode.None;

        // Título
        var titleGo = MakeImage(canvasGo.transform, "TitleBar", sTitle);
        SetRT(titleGo, 0, 440, 920, 110);

        // Fila de slots
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

        var slotUIs = new SlotsScreenManager.SlotUI[4];
        for (int i = 0; i < 4; i++)
            slotUIs[i] = BuildSlot(rowGo.transform, i, font,
                sEmN, sEmH, sEmP, sFilN, sFilH, sFilP);

        // Botones globales
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

        // Panel confirmación borrar
        var cpGo = MakeEmpty(canvasGo.transform, "DeleteConfirmPanel");
        SetRT(cpGo, 0, 0, 680, 320);
        var cpImg = cpGo.AddComponent<Image>();
        cpImg.sprite = sCont;
        cpImg.type   = Image.Type.Sliced;

        var cpTxt = MakeTMP(cpGo.transform, "ConfirmText",
            "¿Borrar esta partida?\nEsta acción no se puede deshacer.", font, 22);
        SetRT(cpTxt.gameObject, 0, 55, 580, 120);
        cpTxt.alignment = TextAlignmentOptions.Center;
        cpTxt.color     = Color.white;

        var cpYes = MakeButton(cpGo.transform, "ConfirmYes", "SÍ", sBtnN, sBtnH, sBtnP, font, 180, 65);
        var cpNo  = MakeButton(cpGo.transform, "ConfirmNo",  "NO", sBtnN, sBtnH, sBtnP, font, 180, 65);
        SetRT(cpYes, -110, -100, 180, 65);
        SetRT(cpNo,   110, -100, 180, 65);
        cpGo.SetActive(false);

        // Ambience AudioSource
        var ambGo  = MakeEmpty(canvasGo.transform, "Ambience");
        SetRT(ambGo, 0, 0, 0, 0);
        var ambSrc = ambGo.AddComponent<AudioSource>();
        var ambSo  = new SerializedObject(ambSrc);
        ambSo.FindProperty("m_audioClip").objectReferenceValue = amClip;
        ambSo.ApplyModifiedProperties();
        ambSrc.loop        = true;
        ambSrc.volume      = 0.6f;
        ambSrc.playOnAwake = false;

        // Cablear SlotsScreenManager
        var so = new SerializedObject(mgr);

        var slotsProp = so.FindProperty("slots");
        slotsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            var e = slotsProp.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("cardButton")    .objectReferenceValue = slotUIs[i].cardButton;
            e.FindPropertyRelative("emptyState")    .objectReferenceValue = slotUIs[i].emptyState;
            e.FindPropertyRelative("occupiedState") .objectReferenceValue = slotUIs[i].occupiedState;
            e.FindPropertyRelative("levelText")     .objectReferenceValue = slotUIs[i].levelText;
            e.FindPropertyRelative("zoneText")      .objectReferenceValue = slotUIs[i].zoneText;
            e.FindPropertyRelative("playTimeText")  .objectReferenceValue = slotUIs[i].playTimeText;
        }

        so.FindProperty("backButton")        .objectReferenceValue = backGo.GetComponent<Button>();
        so.FindProperty("deleteButton")      .objectReferenceValue = deleteGo.GetComponent<Button>();
        so.FindProperty("selectButton")      .objectReferenceValue = selectGo.GetComponent<Button>();
        so.FindProperty("selectButtonLabel") .objectReferenceValue =
            selectGo.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        so.FindProperty("deleteConfirmPanel").objectReferenceValue = cpGo;
        so.FindProperty("confirmDeleteYes")  .objectReferenceValue = cpYes.GetComponent<Button>();
        so.FindProperty("confirmDeleteNo")   .objectReferenceValue = cpNo.GetComponent<Button>();
        so.FindProperty("ambienceSource")    .objectReferenceValue = ambSrc;

        AssignSlotSprites(so, sEmN, sEmH, sEmP, sFilN, sFilH, sFilP);
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SlotsSceneSetup] Escena generada. Guarda con Ctrl+S.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Re-cablea referencias + sprites sin reconstruir la escena.
    [MenuItem("Eldoria/Wire All Slots References")]
    public static void WireAllReferences()
    {
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) { Debug.LogError("[WireSlots] No se encontró Canvas."); return; }
        var mgr = canvasGo.GetComponent<SlotsScreenManager>();
        if (mgr == null) { Debug.LogError("[WireSlots] No hay SlotsScreenManager en Canvas."); return; }

        var so        = new SerializedObject(mgr);
        var slotsProp = so.FindProperty("slots");
        slotsProp.arraySize = 4;

        var sEmN  = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyNormal.png");
        var sEmH  = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyHover.png");
        var sEmP  = Load<Sprite>("Assets/UI/Sprites/Slots/SlotEmptyPress.png");
        var sFilN = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledNormal.png");
        var sFilH = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledHover.png");
        var sFilP = Load<Sprite>("Assets/UI/Sprites/Slots/SlotFilledPress.png");

        for (int i = 0; i < 4; i++)
        {
            int    n    = i + 1;
            string root = $"Canvas/SlotsRow/Slot{n}";
            var    e    = slotsProp.GetArrayElementAtIndex(i);

            e.FindPropertyRelative("cardButton")   .objectReferenceValue = FindBtn($"{root}/CardButton");
            e.FindPropertyRelative("emptyState")   .objectReferenceValue = FindGO($"{root}/EmptyState");
            e.FindPropertyRelative("occupiedState").objectReferenceValue = FindGO($"{root}/OccupiedState");
            e.FindPropertyRelative("levelText")    .objectReferenceValue = FindTMP($"{root}/OccupiedState/LevelText");
            e.FindPropertyRelative("zoneText")     .objectReferenceValue = FindTMP($"{root}/OccupiedState/ZoneText");
            e.FindPropertyRelative("playTimeText") .objectReferenceValue = FindTMP($"{root}/OccupiedState/TimeText");

            // ── Configurar CardButton como botón visible con SpriteSwap ──────
            var cbGo = FindGO($"{root}/CardButton");
            if (cbGo != null)
            {
                // Image: sprite empty normal, color blanco, preservar aspecto
                var img   = cbGo.GetComponent<Image>();
                var imgSo = new SerializedObject(img);
                imgSo.FindProperty("m_Sprite")         .objectReferenceValue = sEmN;
                imgSo.FindProperty("m_Color")          .colorValue           = Color.white;
                imgSo.FindProperty("m_PreserveAspect") .boolValue            = true;
                imgSo.ApplyModifiedProperties();

                // Button: transición SpriteSwap con los 3 estados
                var btn   = cbGo.GetComponent<Button>();
                var btnSo = new SerializedObject(btn);
                btnSo.FindProperty("m_Transition").intValue = 3; // SpriteSwap
                var ss = btnSo.FindProperty("m_SpriteState");
                ss.FindPropertyRelative("m_HighlightedSprite").objectReferenceValue = sEmH;
                ss.FindPropertyRelative("m_PressedSprite")    .objectReferenceValue = sEmP;
                ss.FindPropertyRelative("m_SelectedSprite")   .objectReferenceValue = sEmH;
                btnSo.ApplyModifiedProperties();

                // RectTransform: tamaño 350×500 (igual que EmptyState)
                var rt   = cbGo.GetComponent<RectTransform>();
                var rtSo = new SerializedObject(rt);
                rtSo.FindProperty("m_SizeDelta")        .vector2Value = new Vector2(350, 500);
                rtSo.FindProperty("m_AnchoredPosition") .vector2Value = new Vector2(0, 30);
                rtSo.ApplyModifiedProperties();
            }

            // ── Desactivar CardBg dentro de EmptyState (CardButton ES el fondo) ──
            var cardBgGo = FindGO($"{root}/EmptyState/CardBg");
            if (cardBgGo != null) cardBgGo.SetActive(false);

            // ── Desactivar SelectionHighlight (SpriteSwap lo reemplaza) ─────────
            var hlGo = FindGO($"{root}/SelectionHighlight");
            if (hlGo != null) hlGo.SetActive(false);
        }

        so.FindProperty("backButton")        .objectReferenceValue = FindBtn("Canvas/GlobalButtons/BackButton");
        so.FindProperty("deleteButton")      .objectReferenceValue = FindBtn("Canvas/GlobalButtons/DeleteButton");
        so.FindProperty("selectButton")      .objectReferenceValue = FindBtn("Canvas/GlobalButtons/SelectButton");
        so.FindProperty("selectButtonLabel") .objectReferenceValue = FindTMP("Canvas/GlobalButtons/SelectButton/Label");
        so.FindProperty("deleteConfirmPanel").objectReferenceValue = FindGO("Canvas/DeleteConfirmPanel");
        so.FindProperty("confirmDeleteYes")  .objectReferenceValue = FindBtn("Canvas/DeleteConfirmPanel/ConfirmYes");
        so.FindProperty("confirmDeleteNo")   .objectReferenceValue = FindBtn("Canvas/DeleteConfirmPanel/ConfirmNo");

        // Ambience
        var ambienceGo  = FindGO("Canvas/Ambience");
        var ambienceSrc = ambienceGo != null ? ambienceGo.GetComponent<AudioSource>() : null;
        if (ambienceSrc != null)
        {
            var clip = Load<AudioClip>("Assets/UI/Sprites/NewGame/Ambience Cave Sound Effect.mp3");
            if (clip != null)
            {
                var audioSo = new SerializedObject(ambienceSrc);
                audioSo.FindProperty("m_audioClip").objectReferenceValue = clip;
                audioSo.ApplyModifiedProperties();
            }
        }
        else Debug.LogWarning("[WireSlots] No encontrado: Canvas/Ambience con AudioSource.");
        so.FindProperty("ambienceSource").objectReferenceValue = ambienceSrc;

        AssignSlotSprites(so, sEmN, sEmH, sEmP, sFilN, sFilH, sFilP);

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[WireSlots] Referencias y sprites cableados. Guarda con Ctrl+S.");
    }

    static void AssignSlotSprites(SerializedObject so,
        Sprite emN, Sprite emH, Sprite emP,
        Sprite filN, Sprite filH, Sprite filP)
    {
        so.FindProperty("_sprEmptyNormal") .objectReferenceValue = emN;
        so.FindProperty("_sprEmptyHover")  .objectReferenceValue = emH;
        so.FindProperty("_sprEmptyPress")  .objectReferenceValue = emP;
        so.FindProperty("_sprFilledNormal").objectReferenceValue = filN;
        so.FindProperty("_sprFilledHover") .objectReferenceValue = filH;
        so.FindProperty("_sprFilledPress") .objectReferenceValue = filP;
    }

    // ── Helpers de búsqueda ───────────────────────────────────────────────
    static GameObject FindGO(string path)
    {
        int slash = path.IndexOf('/');
        var root  = slash < 0 ? GameObject.Find(path) : GameObject.Find(path.Substring(0, slash));
        if (root == null) { Debug.LogWarning($"[WireSlots] Raíz no encontrada: {path}"); return null; }
        if (slash < 0) return root;
        var t = root.transform.Find(path.Substring(slash + 1));
        if (t == null) Debug.LogWarning($"[WireSlots] No encontrado: {path}");
        return t != null ? t.gameObject : null;
    }
    static Button          FindBtn(string p) { var g = FindGO(p); return g != null ? g.GetComponent<Button>()          : null; }
    static TextMeshProUGUI FindTMP(string p) { var g = FindGO(p); return g != null ? g.GetComponent<TextMeshProUGUI>() : null; }

    // ── BuildSlot ─────────────────────────────────────────────────────────
    // CardButton ES la tarjeta visible (SpriteSwap); EmptyState y OccupiedState
    // contienen solo texto/iconos ENCIMA de la tarjeta, sin CardBg propio.
    static SlotsScreenManager.SlotUI BuildSlot(
        Transform parent, int idx, TMP_FontAsset font,
        Sprite sEmN, Sprite sEmH, Sprite sEmP,
        Sprite sFilN, Sprite sFilH, Sprite sFilP)
    {
        var ui  = new SlotsScreenManager.SlotUI();
        int num = idx + 1;

        var col = MakeEmpty(parent, $"Slot{num}");
        col.GetComponent<RectTransform>().sizeDelta = new Vector2(COL_W, COL_H);

        float cardY = (COL_H - CARD_H) * 0.5f - 5f;

        // ── CardButton — tarjeta visible con SpriteSwap ───────────────────
        // Sibling 0: renderiza primero (detrás del contenido de texto)
        var cbGo = MakeEmpty(col.transform, "CardButton");
        SetRT(cbGo, 0, cardY, CARD_W, CARD_H);
        var cbImg = cbGo.AddComponent<Image>();
        cbImg.sprite         = sEmN;    // sprite inicial (vacío)
        cbImg.color          = Color.white;
        cbImg.preserveAspect = true;
        cbImg.raycastTarget  = true;
        var cb               = cbGo.AddComponent<Button>();
        cb.targetGraphic     = cbImg;
        cb.transition        = Selectable.Transition.SpriteSwap;
        cb.spriteState       = new SpriteState
        {
            highlightedSprite = sEmH,
            pressedSprite     = sEmP,
            selectedSprite    = sEmH,
        };
        var nav = cb.navigation;
        nav.mode       = Navigation.Mode.None;
        cb.navigation  = nav;
        ui.cardButton  = cb;

        // ── EmptyState — texto sobre la tarjeta (sin CardBg propio) ──────
        // Sibling 1: renderiza encima de CardButton
        var emp = MakeEmpty(col.transform, "EmptyState");
        SetRT(emp, 0, cardY, CARD_W, CARD_H);
        ui.emptyState = emp;

        // Signo "?" decorativo sobre la tarjeta vacía
        var qTMP = MakeTMP(emp.transform, "QuestionMark", "?", font, 110);
        SetRT(qTMP.gameObject, 0, 30, 200, 180);
        qTMP.alignment     = TextAlignmentOptions.Center;
        qTMP.color         = new Color(0.9f, 0.75f, 0.2f, 0.5f);
        qTMP.raycastTarget = false;

        // ── OccupiedState — texto sobre la tarjeta llena (desactivado) ───
        // Sibling 2: renderiza encima de CardButton
        var occ = MakeEmpty(col.transform, "OccupiedState");
        SetRT(occ, 0, cardY, CARD_W, CARD_H);
        occ.SetActive(false);
        ui.occupiedState = occ;

        var lvlTMP = MakeTMP(occ.transform, "LevelText", "NIV. 1", font, 22);
        SetRT(lvlTMP.gameObject, 0, -108, 230, 38);
        lvlTMP.alignment     = TextAlignmentOptions.Center;
        lvlTMP.color         = new Color(0.95f, 0.9f, 0.55f, 1f);
        lvlTMP.fontStyle     = FontStyles.Bold;
        lvlTMP.raycastTarget = false;
        ui.levelText         = lvlTMP;

        var zoneTMP = MakeTMP(occ.transform, "ZoneText", "Inicio", font, 20);
        SetRT(zoneTMP.gameObject, 0, -145, 248, 38);
        zoneTMP.alignment     = TextAlignmentOptions.Center;
        zoneTMP.color         = Color.white;
        zoneTMP.raycastTarget = false;
        ui.zoneText           = zoneTMP;

        var timeTMP = MakeTMP(occ.transform, "TimeText", "00:00:00", font, 20);
        SetRT(timeTMP.gameObject, 0, -182, 200, 38);
        timeTMP.alignment     = TextAlignmentOptions.Center;
        timeTMP.color         = new Color(0.9f, 0.85f, 0.45f, 1f);
        timeTMP.raycastTarget = false;
        ui.playTimeText       = timeTMP;

        return ui;
    }

    // ── Helpers de creación ───────────────────────────────────────────────
    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

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
        img.preserveAspect = true;
        var btn            = go.AddComponent<Button>();
        btn.targetGraphic  = img;
        btn.transition     = Selectable.Transition.SpriteSwap;
        btn.spriteState    = new SpriteState { highlightedSprite = hover, pressedSprite = press };

        var lbl = MakeTMP(go.transform, "Label", label, font, 19);
        Stretch(lbl.gameObject);
        lbl.alignment     = TextAlignmentOptions.Center;
        lbl.color         = Color.white;
        lbl.fontStyle     = FontStyles.Bold;
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

    static void SetRT(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    static void Stretch(GameObject go)
    {
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
#endif
