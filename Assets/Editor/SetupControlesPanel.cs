// Menú: Eldoria/Setup Controles Panel
// Reconstruye el ControlesPanel clonando el Scroll View funcional de GraficosPanel
// y creando las 10 filas de rebind con GameObjectUtility.SetParentAndAlign.
// Patrón idéntico al usado en SettingsContentSetup.cs para garantizar que
// layers, fuentes, colores y ScrollRect queden correctamente configurados.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupControlesPanel
{
    struct ActionDef
    {
        public string  id;
        public string  label;
        public KeyCode defaultKey;
        public ActionDef(string id, string label, KeyCode key)
        { this.id = id; this.label = label; this.defaultKey = key; }
    }

    static readonly ActionDef[] Actions =
    {
        new ActionDef("MoveLeft",  "Mover izquierda", KeyCode.LeftArrow),
        new ActionDef("MoveRight", "Mover derecha",   KeyCode.RightArrow),
        new ActionDef("Jump",      "Saltar",           KeyCode.Z),
        new ActionDef("Run",       "Correr (toggle)",  KeyCode.LeftShift),
        new ActionDef("Dash",      "Dash",             KeyCode.C),
        new ActionDef("Float",     "Flotar",           KeyCode.F),
        new ActionDef("Attack",    "Atacar",           KeyCode.X),
        new ActionDef("Interact",  "Interactuar",      KeyCode.E),
        new ActionDef("Teleport",  "Teleporte",        KeyCode.V),
        new ActionDef("MapOpen",   "Abrir mapa",       KeyCode.M),
    };

    [MenuItem("Eldoria/Setup Controles Panel")]
    static void Execute()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Eldoria] Sal del Play Mode antes de ejecutar Setup Controles Panel.");
            return;
        }

        // ── 1. Cargar Settings si no está abierta ─────────────────────────────
        const string SettingsPath = "Assets/Scenes/Settings.unity";
        bool openedSettings = false;
        UnityEngine.SceneManagement.Scene settingsScene = default;

        var controlesPanel = FindInactive("ControlesPanel");
        if (controlesPanel == null)
        {
            settingsScene  = EditorSceneManager.OpenScene(SettingsPath, OpenSceneMode.Additive);
            openedSettings = true;
            controlesPanel = FindInactive("ControlesPanel");
        }
        if (controlesPanel == null) { Debug.LogError("[Eldoria] ControlesPanel no encontrado."); return; }

        // ── 2. Encontrar GraficosPanel (fuente del Scroll View funcional) ─────
        var graficosPanel = FindInactive("GraficosPanel");
        if (graficosPanel == null) { Debug.LogError("[Eldoria] GraficosPanel no encontrado."); return; }

        var gScrollViewTF = graficosPanel.transform.Find("Scroll View");
        if (gScrollViewTF == null) { Debug.LogError("[Eldoria] GraficosPanel/Scroll View no encontrado."); return; }

        // ── 3. Cargar sprites de tecla ─────────────────────────────────────────
        Sprite normalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/UI/Sprites/KeysConfig/KeysConfigNormal.png");
        Sprite pressSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/UI/Sprites/KeysConfig/KeysConfigPresss.png");

        if (normalSprite == null)
            Debug.LogWarning("[Eldoria] KeysConfigNormal.png no encontrado.");

        // ── 4. Reemplazar Scroll View en ControlesPanel ────────────────────────
        Undo.RegisterFullObjectHierarchyUndo(controlesPanel, "Setup Controles Panel");

        // Eliminar Scroll View viejo (mantenemos el Tittle)
        var oldSV = controlesPanel.transform.Find("Scroll View");
        if (oldSV != null) Object.DestroyImmediate(oldSV.gameObject);

        // Clonar el Scroll View funcional de GraficosPanel
        var svClone = (GameObject)Object.Instantiate(gScrollViewTF.gameObject);
        svClone.name = "Scroll View";
        GameObjectUtility.SetParentAndAlign(svClone, controlesPanel);

        // ── 5. Limpiar el Content clonado y ajustar padding ───────────────────
        var contentTF = svClone.transform.Find("Viewport/Content");
        if (contentTF == null) { Debug.LogError("[Eldoria] Viewport/Content no encontrado en ScrollView clonado."); return; }

        ClearChildren(contentTF.gameObject);

        // Reducir padding superior (GraficosPanel tiene 335px extra para su Tittle)
        var vlg = contentTF.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
            vlg.padding = new RectOffset(vlg.padding.left, vlg.padding.right, 10, 10);

        // Asegurar ContentSizeFitter
        var csf = contentTF.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = contentTF.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── 6. Colores y fuente ────────────────────────────────────────────────
        var lblColor = new Color(0.9098f, 0.8706f, 0.7843f, 1f);  // crema cálido
        var keyColor = Color.white;                                 // blanco — visible sobre el sprite de tecla

        TMP_FontAsset font = null;
        var guids = AssetDatabase.FindAssets("Perfect DOS VGA 437 Win SDF t:TMP_FontAsset");
        if (guids.Length > 0)
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));

        // ── 7. Construir las 10 filas ──────────────────────────────────────────
        var buttons   = new Button[Actions.Length];
        var keyLabels = new TMP_Text[Actions.Length];

        for (int i = 0; i < Actions.Length; i++)
        {
            var a = Actions[i];

            // —— Row ——————————————————————————————————————————————————————————
            var rowGO = new GameObject("Row_" + a.id, typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(rowGO, contentTF.gameObject);

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 12;
            hlg.childForceExpandWidth  = false;  // solo el Lbl (flexibleWidth=1) se expande; botón queda a la derecha
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = false;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.padding = new RectOffset(20, 24, 5, 5);

            rowGO.AddComponent<LayoutElement>().minHeight = 100;

            // —— Lbl (flexible: ocupa el espacio sobrante) ————————————————————
            var lblGO = new GameObject("Lbl", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(lblGO, rowGO);

            // flexibleWidth=1 → se estira; RebindBtn con flexibleWidth=0 queda fijo a la derecha
            var lblLE = lblGO.AddComponent<LayoutElement>();
            lblLE.minWidth      = 200;
            lblLE.flexibleWidth = 1;

            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = a.label;
            lbl.fontSize  = 35;
            lbl.color     = lblColor;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) lbl.font = font;

            // —— RebindBtn (ancho fijo 240px, anclado a la derecha) ————————————
            var btnGO = new GameObject("RebindBtn", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(btnGO, rowGO);

            var btnLE = btnGO.AddComponent<LayoutElement>();
            btnLE.minWidth      = 240;
            btnLE.preferredWidth = 240;
            btnLE.flexibleWidth = 0;

            var btnImg = btnGO.AddComponent<Image>();
            if (normalSprite != null)
            {
                btnImg.sprite = normalSprite;
                btnImg.type   = Image.Type.Simple;
                btnImg.preserveAspect = false;
                btnImg.color  = Color.white;
            }
            else
            {
                btnImg.color = new Color(0.18f, 0.18f, 0.25f, 0.92f);
            }

            var btn = btnGO.AddComponent<Button>();
            if (normalSprite != null && pressSprite != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                btn.spriteState = new SpriteState
                {
                    highlightedSprite = pressSprite,
                    pressedSprite     = pressSprite,
                    selectedSprite    = normalSprite,
                    disabledSprite    = normalSprite,
                };
            }

            // —— KeyLbl (texto de la tecla, sobre el sprite) ——————————————————
            var keyGO = new GameObject("KeyLbl", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(keyGO, btnGO);

            var keyText = keyGO.AddComponent<TextMeshProUGUI>();
            keyText.text      = KeyRebindUI.FriendlyName(a.defaultKey);
            keyText.fontSize  = 34;
            keyText.color     = keyColor;
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.fontStyle = FontStyles.Bold;
            if (font != null) keyText.font = font;

            buttons[i]   = btn;
            keyLabels[i] = keyText;
        }

        // ── 8. Añadir/reemplazar KeyRebindUI en ControlesPanel ────────────────
        var oldKR = controlesPanel.GetComponent<KeyRebindUI>();
        if (oldKR != null) Object.DestroyImmediate(oldKR);

        var rebindUI = controlesPanel.AddComponent<KeyRebindUI>();
        var so = new SerializedObject(rebindUI);
        so.FindProperty("keyNormalSprite").objectReferenceValue    = normalSprite;
        so.FindProperty("keyListeningSprite").objectReferenceValue = pressSprite;

        var entriesProp = so.FindProperty("entries");
        entriesProp.arraySize = Actions.Length;
        for (int i = 0; i < Actions.Length; i++)
        {
            var ep = entriesProp.GetArrayElementAtIndex(i);
            ep.FindPropertyRelative("actionId").stringValue              = Actions[i].id;
            ep.FindPropertyRelative("defaultKey").intValue               = (int)Actions[i].defaultKey;
            ep.FindPropertyRelative("rebindButton").objectReferenceValue = buttons[i];
            ep.FindPropertyRelative("keyLabel").objectReferenceValue     = keyLabels[i];
        }
        so.ApplyModifiedProperties();

        // ── 9. Forzar recalculo de layout y guardar ───────────────────────────
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            controlesPanel.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>()
            ?? (RectTransform)contentTF);

        EditorUtility.SetDirty(controlesPanel);
        var scene = controlesPanel.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (openedSettings) EditorSceneManager.CloseScene(settingsScene, false);

        Debug.Log($"[Eldoria] ControlesPanel listo: {Actions.Length} acciones. Scroll View clonado de GraficosPanel. Escena guardada.");
    }

    static void ClearChildren(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
    }

    static GameObject FindInactive(string name)
    {
        for (int s = 0; s < EditorSceneManager.sceneCount; s++)
        {
            var sc = EditorSceneManager.GetSceneAt(s);
            if (!sc.isLoaded) continue;
            foreach (var root in sc.GetRootGameObjects())
            {
                var found = FindInChildren(root.transform, name);
                if (found != null) return found;
            }
        }
        return null;
    }

    static GameObject FindInChildren(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var found = FindInChildren(t.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
