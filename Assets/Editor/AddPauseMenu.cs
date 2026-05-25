using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Crea la jerarquía completa del menú de pausa en MainMenu.unity.
// El Canvas queda serializado en la escena → editable desde el Inspector de Unity.
// Para ajustar tamaños: MainMenu → Hierarchy → PauseMenuManager → Overlay → Container.
public class AddPauseMenu
{
    static readonly Color Gold = new Color(0.95f, 0.82f, 0.45f, 1f);
    static readonly Color Red  = new Color(0.90f, 0.22f, 0.15f, 1f);

    [MenuItem("Eldoria/Add Pause Menu")]
    static void Run()
    {
        string returnPath = EditorSceneManager.GetActiveScene().path;
        var mainMenu = EditorSceneManager.OpenScene(
            "Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        // Limpia instancia previa
        var existing = GameObject.Find("PauseMenuManager");
        if (existing != null) Object.DestroyImmediate(existing);

        // Assets
        var containerSpr = L<Sprite>("Assets/UI/Sprites/Pause/PauseContainer.png");
        var titleSpr     = L<Sprite>("Assets/UI/Sprites/Pause/PauseTitle.png");
        var btnNormal    = L<Sprite>("Assets/UI/Sprites/Buttons/normalButton.png");
        var btnHover     = L<Sprite>("Assets/UI/Sprites/Buttons/hoverButton.png");
        var btnPress     = L<Sprite>("Assets/UI/Sprites/Buttons/pressButton.png");
        var font         = L<TMP_FontAsset>("Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");

        // ── Root: PauseMenuManager con Canvas ────────────────────────────────
        var root = new GameObject("PauseMenuManager");
        var pm   = root.AddComponent<PauseMenuManager>();

        var canvas          = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler                 = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // ── Overlay oscuro fullscreen ─────────────────────────────────────────
        var overlay    = UI("Overlay", root.transform);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.65f);
        Stretch(overlay);

        // ── Container (PauseContainer.png) ───────────────────────────────────
        // Para cambiar tamaño: selecciona "Container" en Hierarchy → Inspector → Rect Transform
        var container    = UI("Container", overlay.transform);
        var containerImg = container.AddComponent<Image>();
        containerImg.sprite         = containerSpr;
        containerImg.type           = Image.Type.Simple;
        containerImg.preserveAspect = false;
        var cRT = Rt(container);
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta        = new Vector2(340, 600);
        cRT.anchoredPosition = Vector2.zero;

        // ── Banner título (PauseTitle.png) ────────────────────────────────────
        // Para cambiar tamaño: selecciona "Title" en Hierarchy → Inspector → Rect Transform
        var title    = UI("Title", container.transform);
        var titleImg = title.AddComponent<Image>();
        titleImg.sprite         = titleSpr;
        titleImg.type           = Image.Type.Simple;
        titleImg.preserveAspect = false;
        var tRT = Rt(title);
        tRT.anchorMin        = tRT.anchorMax = new Vector2(0.5f, 1f);
        tRT.pivot            = new Vector2(0.5f, 0.5f);
        tRT.sizeDelta        = new Vector2(420, 100);
        tRT.anchoredPosition = new Vector2(0f, 6f);

        var titleTextGO = UI("TitleText", title.transform);
        var tt = titleTextGO.AddComponent<TextMeshProUGUI>();
        tt.text = "PAUSA"; tt.fontSize = 32; tt.font = font;
        tt.alignment = TextAlignmentOptions.Center; tt.color = Gold;
        Stretch(titleTextGO);

        // ── Panel de botones principales ──────────────────────────────────────
        // Ajusta posición/espaciado en Inspector → VerticalLayoutGroup
        var btnGroup = UI("ButtonsGroup", container.transform);
        VLG(btnGroup, 22, new RectOffset(25, 25, 0, 0));
        var bRT = Rt(btnGroup);
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(0f, 55f);
        bRT.offsetMax = new Vector2(0f, -110f);

        var goCont   = Btn(btnGroup.transform, "Btn_Continuar", "CONTINUAR", Gold, btnNormal, btnHover, btnPress, font);
        var goAjust  = Btn(btnGroup.transform, "Btn_Ajustes",   "AJUSTES",   Gold, btnNormal, btnHover, btnPress, font);
        var goSalir  = Btn(btnGroup.transform, "Btn_Salir",     "SALIR",     Red,  btnNormal, btnHover, btnPress, font);

        // ── Panel de confirmación (SALIR) ─────────────────────────────────────
        var confirmGroup = UI("ConfirmGroup", container.transform);
        VLG(confirmGroup, 18, new RectOffset(25, 25, 0, 0));
        var cfRT = Rt(confirmGroup);
        cfRT.anchorMin = Vector2.zero; cfRT.anchorMax = Vector2.one;
        cfRT.offsetMin = new Vector2(0f, 55f);
        cfRT.offsetMax = new Vector2(0f, -110f);

        var msgGO = UI("ConfirmMsg", confirmGroup.transform);
        var msg   = msgGO.AddComponent<TextMeshProUGUI>();
        msg.text = "¿SALIR AL MENÚ\nPRINCIPAL?";
        msg.fontSize = 20; msg.font = font;
        msg.alignment = TextAlignmentOptions.Center; msg.color = Gold;
        var msgLE = msgGO.AddComponent<LayoutElement>();
        msgLE.preferredHeight = 80; msgLE.minHeight = 80;

        var goConfirm  = Btn(confirmGroup.transform, "Btn_Confirmar", "CONFIRMAR", Red,  btnNormal, btnHover, btnPress, font);
        var goCancelar = Btn(confirmGroup.transform, "Btn_Cancelar",  "CANCELAR",  Gold, btnNormal, btnHover, btnPress, font);

        confirmGroup.SetActive(false);
        overlay.SetActive(false);

        // ── Cablear referencias en PauseMenuManager ────────────────────────────
        var so = new SerializedObject(pm);
        so.FindProperty("overlay").objectReferenceValue      = overlay;
        so.FindProperty("buttonsGroup").objectReferenceValue = btnGroup;
        so.FindProperty("confirmGroup").objectReferenceValue = confirmGroup;
        so.ApplyModifiedProperties();

        // ── Cablear onClick de botones ─────────────────────────────────────────
        Wire(goCont,    pm.OnContinuar);
        Wire(goAjust,   pm.OnAjustes);
        Wire(goSalir,   pm.OnSalir);
        Wire(goConfirm, pm.OnConfirmarSalir);
        Wire(goCancelar,pm.OnCancelarSalir);

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(mainMenu);
        EditorSceneManager.SaveScene(mainMenu);
        Debug.Log("Eldoria: PauseMenuManager creado en MainMenu. Abre MainMenu.unity para editarlo.");

        if (!string.IsNullOrEmpty(returnPath))
            EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static T L<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path);

    static GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform Rt(GameObject go) => go.GetComponent<RectTransform>();

    static void Stretch(GameObject go)
    {
        var rt = Rt(go);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void VLG(GameObject go, float spacing, RectOffset padding)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing              = spacing;
        v.childAlignment       = TextAnchor.MiddleCenter;
        v.childControlWidth    = true;
        v.childControlHeight   = true;
        v.childForceExpandWidth  = true;
        v.childForceExpandHeight = false;
        v.padding = padding;
    }

    static GameObject Btn(Transform parent, string name, string label, Color textColor,
                          Sprite normal, Sprite hover, Sprite press, TMP_FontAsset font)
    {
        var go  = UI(name, parent);
        var img = go.AddComponent<Image>();
        img.sprite = normal; img.type = Image.Type.Simple;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.SpriteSwap;
        btn.spriteState   = new SpriteState
        {
            highlightedSprite = hover,
            pressedSprite     = press,
            selectedSprite    = normal
        };

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 70; le.minHeight = 70;

        var tGO = UI("Text", go.transform);
        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 22; tmp.font = font;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = textColor;
        Stretch(tGO);

        return go;
    }

    static void Wire(GameObject go, UnityEngine.Events.UnityAction action)
    {
        var btn = go.GetComponent<Button>();
        if (btn != null)
            UnityEventTools.AddPersistentListener(btn.onClick, action);
    }
}
