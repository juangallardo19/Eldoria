// Menú: Eldoria/Setup World Map
// Construye el canvas del mapa con layout de nodos (según sketch del usuario):
//   Tab HUB  : HUB07—HUB01—HUB02—HUB03  con ramas hacia HUB04, HUB05, HUB06
//   Tab MTN  : cuatro filas conectadas que forman el recorrido de las montañas
// Ejecutar con MainMenu abierta → queda DontDestroyOnLoad en runtime.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupWorldMap
{
    // ── Posiciones de nodos (coordenadas de canvas, origen = centro = 0,0) ──
    // HUB (sección regular 190×150; HUB02 = 220×170)
    static readonly (string id, float x, float y, string[] scenes)[] HubNodes =
    {
        ("HUB07", -720f, -140f, new[]{"HV07"}),
        ("HUB01", -420f, -140f, new[]{"HV01_Interior","HV01_Exterior"}),
        ("HUB02",    0f, -140f, new[]{"HV02_PlazaCentral"}),   // más grande
        ("HUB03",  380f, -140f, System.Array.Empty<string>()),  // zona futura
        ("HUB04", -130f,   50f, new[]{"HV04"}),
        ("HUB05", -130f,  230f, new[]{"HV05"}),
        ("HUB06",  130f,   50f, new[]{"HV06"}),
    };

    // Conexiones HUB (índices a HubNodes)
    static readonly (int a, int b)[] HubLines =
    {
        (0,1),(1,2),(2,3),   // camino horizontal
        (2,4),(4,5),(2,6),   // ramas de HUB02
    };

    const float HubW = 190f; const float HubH = 150f;
    const float Hub02W = 220f; const float Hub02H = 170f;

    // MTN (todas 160×110)
    static readonly (string id, float x, float y, string[] scenes)[] MtnNodes =
    {
        // fila superior
        ("MTN04", -560f,  280f, new[]{"MTN04"}),
        ("MTN03", -290f,  280f, new[]{"MTN03"}),
        ("MTN02",  -20f,  280f, new[]{"MTN02"}),
        ("MTN01",  250f,  280f, new[]{"MTN01_Exterior","MTN01_Interior"}),
        // rama vertical de MTN06 (entre filas)
        ("MTN07", -290f,  140f, new[]{"MTN07"}),
        // fila media
        ("MTN12", -760f,  -10f, new[]{"MTN12"}),
        ("MTN05", -560f,  -10f, new[]{"MTN05"}),
        ("MTN06", -290f,  -10f, new[]{"MTN06"}),
        ("MTN08",    0f,  -10f, new[]{"MTN08"}),
        ("MTN09",  280f,  -10f, new[]{"MTN09"}),
        // fila inferior
        ("MTN11",    0f, -245f, new[]{"MTN11"}),
        ("MTN10",  280f, -245f, new[]{"PreMTN10","MTN10"}),
    };

    // Conexiones MTN (índices a MtnNodes array)
    // 0=MTN04  1=MTN03  2=MTN02  3=MTN01
    // 4=MTN07
    // 5=MTN12  6=MTN05  7=MTN06  8=MTN08  9=MTN09
    // 10=MTN11  11=MTN10
    static readonly (int a, int b)[] MtnLines =
    {
        (0,1),(1,2),(2,3),       // fila superior
        (0,6),                   // MTN04 → MTN05 (vertical)
        (5,6),(6,7),(7,8),(8,9), // fila media
        (7,4),                   // MTN06 → MTN07 (vertical arriba)
        (5,10),                  // MTN12 → MTN11 (línea larga diagonal)
        (10,11),                 // MTN11 → MTN10
        (9,11),                  // MTN09 → MTN10 (vertical)
    };

    const float MtnW = 160f; const float MtnH = 110f;

    // ── Colores ─────────────────────────────────────────────────────────────
    static readonly Color BgColor    = new(0f,   0f,   0.04f, 0.88f);
    static readonly Color LineColor  = new(0.7f, 0.7f, 0.7f,  1f);
    static readonly Color TitleColor = new(0.9098f, 0.8706f, 0.7843f, 1f);
    static readonly Color ZoneColor  = new(0.55f,   0.85f,   1f, 1f);
    static readonly Color HintColor  = new(0.45f,   0.45f,   0.45f, 1f);

    [MenuItem("Eldoria/Setup World Map")]
    static void Execute()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Eldoria] Sal del Play Mode antes de ejecutar Setup World Map.");
            return;
        }

        // ── 1. Asegurar que todos los assets estén importados ────────────
        AssetDatabase.Refresh();

        // ── 2. Fuente ────────────────────────────────────────────────────
        TMP_FontAsset font = null;
        var guids = AssetDatabase.FindAssets("Perfect DOS VGA 437 Win SDF t:TMP_FontAsset");
        if (guids.Length > 0)
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));

        // ── 4. Limpiar panel viejo ────────────────────────────────────────
        var old = GameObject.Find("[WorldMapPanel]");
        if (old != null) { Undo.RegisterFullObjectHierarchyUndo(old, "Setup World Map"); Object.DestroyImmediate(old); }

        // ── 5. Raíz ────────────────────────────────────────────────────────
        var root       = new GameObject("[WorldMapPanel]");
        Undo.RegisterCreatedObjectUndo(root, "Setup World Map");
        var controller = root.AddComponent<WorldMapController>();

        // ── 4. Canvas ─────────────────────────────────────────────────────
        var canvasGO = new GameObject("MapCanvas");
        GameObjectUtility.SetParentAndAlign(canvasGO, root);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 5. Fondo oscuro (filtro como PauseMenu) ───────────────────────
        var bg = CreateImage(canvasGO, "Background", BgColor);
        Stretch(bg.GetComponent<RectTransform>());

        // ── 6. Título ─────────────────────────────────────────────────────
        var titleGO = MakeTMP(canvasGO, "Title", "MAPA", font, 58f, TitleColor);
        AC(titleGO, 0f, 475f, 500f, 68f);

        // ── 7. Nombre zona actual ─────────────────────────────────────────
        var zoneNameGO = MakeTMP(canvasGO, "ZoneName", "", font, 30f, ZoneColor);
        AC(zoneNameGO, 0f, 410f, 800f, 44f);

        // ── 8. Hint cerrar ────────────────────────────────────────────────
        var hintGO = MakeTMP(canvasGO, "CloseHint", "[ M ] CERRAR   |   [ Tab ] CAMBIAR SECCION", font, 26f, HintColor);
        AC(hintGO, 0f, -495f, 800f, 38f);

        // ── 10. Contenedor HUB ────────────────────────────────────────────
        var hubGO  = new GameObject("HubContainer");
        GameObjectUtility.SetParentAndAlign(hubGO, canvasGO);
        { var rt = hubGO.GetComponent<RectTransform>(); if (rt == null) rt = hubGO.AddComponent<RectTransform>(); Stretch(rt); }

        BuildHubMap(hubGO, font);

        // ── 11. Contenedor MTN ────────────────────────────────────────────
        var mtnGO  = new GameObject("MtnContainer");
        GameObjectUtility.SetParentAndAlign(mtnGO, canvasGO);
        { var rt = mtnGO.GetComponent<RectTransform>(); if (rt == null) rt = mtnGO.AddComponent<RectTransform>(); Stretch(rt); }

        BuildMtnMap(mtnGO, font);
        mtnGO.SetActive(false);   // empieza oculto

        // ── 12. Desactivar canvas en editor (Awake lo vuelve a desactivar en runtime) ──
        canvasGO.SetActive(false);

        // ── 13. Cablear WorldMapController ────────────────────────────────
        var so = new SerializedObject(controller);
        so.FindProperty("mapCanvas").objectReferenceValue        = canvasGO;
        so.FindProperty("hubContainer").objectReferenceValue     = hubGO;
        so.FindProperty("mtnContainer").objectReferenceValue     = mtnGO;
        so.FindProperty("currentZoneLabel").objectReferenceValue = zoneNameGO.GetComponent<TMP_Text>();
        so.ApplyModifiedProperties();

        // ── 14. Guardar ───────────────────────────────────────────────────
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);

        Debug.Log("[Eldoria] WorldMap creado con layout de nodos. Tab: HUB activo por defecto.");
    }

    // ── Construir mapa HUB ────────────────────────────────────────────────
    static void BuildHubMap(GameObject parent, TMP_FontAsset font)
    {
        // Líneas primero (quedan detrás de las secciones)
        foreach (var (a, b) in HubLines)
        {
            var pa = new Vector2(HubNodes[a].x, HubNodes[a].y);
            var pb = new Vector2(HubNodes[b].x, HubNodes[b].y);
            CreateLine(parent, pa, pb, LineColor, 3f);
        }

        // Secciones
        foreach (var n in HubNodes)
        {
            float w = n.id == "HUB02" ? Hub02W : HubW;
            float h = n.id == "HUB02" ? Hub02H : HubH;
            CreateSection(parent, n.id, n.x, n.y, w, h,
                          HubSpritePath(n.id, "NormalState"),
                          HubSpritePath(n.id, "ActivateState"),
                          n.scenes, font);
        }
    }

    // ── Construir mapa MTN ────────────────────────────────────────────────
    static void BuildMtnMap(GameObject parent, TMP_FontAsset font)
    {
        foreach (var (a, b) in MtnLines)
        {
            var pa = new Vector2(MtnNodes[a].x, MtnNodes[a].y);
            var pb = new Vector2(MtnNodes[b].x, MtnNodes[b].y);
            CreateLine(parent, pa, pb, LineColor, 3f);
        }

        foreach (var n in MtnNodes)
        {
            CreateSection(parent, n.id, n.x, n.y, MtnW, MtnH,
                          MtnSpritePath(n.id, "NormalState"),
                          MtnSpritePath(n.id, "ActivateState"),
                          n.scenes, font);
        }
    }

    // ── Crear un nodo/sección ─────────────────────────────────────────────
    static void CreateSection(GameObject parent, string zoneId,
                              float x, float y, float w, float h,
                              string normalPath, string activePath,
                              string[] scenes, TMP_FontAsset font)
    {
        var go  = new GameObject("Section_" + zoneId);
        GameObjectUtility.SetParentAndAlign(go, parent);
        var img = go.AddComponent<Image>();
        img.preserveAspect = false;

        var sec = go.AddComponent<WorldMapSection>();
        sec.zoneId       = zoneId;
        sec.sceneNames   = scenes;
        sec.normalSprite = Load<Sprite>(normalPath);
        sec.activeSprite = Load<Sprite>(activePath);

        if (sec.normalSprite == null)
            Debug.LogWarning($"[WorldMap] Sprite no encontrado: {normalPath}");
        if (sec.activeSprite == null)
            Debug.LogWarning($"[WorldMap] Sprite no encontrado: {activePath}");

        if (sec.normalSprite != null) img.sprite = sec.normalSprite;
        else img.color = new Color(0.2f, 0.2f, 0.3f, 0.8f); // placeholder visible si falta el sprite

        AC(go, x, y, w, h);

        // Etiqueta del nombre de la zona (debajo de la imagen)
        var lbl = MakeTMP(go, "ZoneLabel", zoneId, font, 22f, new Color(0.8f, 0.8f, 0.8f, 1f));
        AC(lbl, 0f, -(h * 0.5f + 14f), w + 20f, 30f);
    }

    // ── Crear línea de conexión entre dos puntos ──────────────────────────
    static void CreateLine(GameObject parent, Vector2 from, Vector2 to, Color color, float thickness)
    {
        var go  = new GameObject("Line");
        GameObjectUtility.SetParentAndAlign(go, parent);
        go.AddComponent<Image>().color = color;

        var rt    = go.GetComponent<RectTransform>();
        Vector2 d = to - from;
        float len = d.magnitude;
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (from + to) * 0.5f;
        rt.sizeDelta        = new Vector2(len, thickness);
        rt.localRotation    = Quaternion.Euler(0f, 0f, ang);
    }

    // ── UI helpers ────────────────────────────────────────────────────────
    static GameObject CreateImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        GameObjectUtility.SetParentAndAlign(go, parent);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static GameObject MakeTMP(GameObject parent, string name, string text,
                               TMP_FontAsset font, float fontSize, Color color)
    {
        var go  = new GameObject(name);
        GameObjectUtility.SetParentAndAlign(go, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        return go;
    }

    // anchorMin=anchorMax=pivot=center; posición relativa al canvas center
    static void AC(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static T Load<T>(string path) where T : Object =>
        AssetDatabase.LoadAssetAtPath<T>(path);

    static string HubSpritePath(string id, string state) =>
        $"Assets/UI/Mapa/HUB/{id}/{state}.png";

    static string MtnSpritePath(string id, string state) =>
        $"Assets/UI/Mapa/MTN/{id}/{state}.png";
}
#endif
