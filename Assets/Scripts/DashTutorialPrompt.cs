using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Dash tutorial — appears after unlocking dash and stays until the player presses C.
// Instantiated as a standalone GO from BossObsesion.DefeatedSequence.
public class DashTutorialPrompt : MonoBehaviour
{
    void Start() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("DashTutCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Semi-transparent background ───────────────────────────────────────
        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin        = new Vector2(0.5f, 0f);
        bgRt.anchorMax        = new Vector2(0.5f, 0f);
        bgRt.pivot            = new Vector2(0.5f, 0f);
        bgRt.anchoredPosition = new Vector2(0f, 80f);
        bgRt.sizeDelta        = new Vector2(620f, 70f);

        // ── Text label ───────────────────────────────────────────────────────
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "✦ DASH DESBLOQUEADO ✦\nPresiona [ C ] para activarlo";
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(1f, 0.9f, 0.35f);
#if UNITY_EDITOR
        var f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
        var f = Resources.Load<TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
        if (f != null) tmp.font = f;
        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 88f);
        rt.sizeDelta        = new Vector2(600f, 60f);

        // ── Text blink ────────────────────────────────────────────────────────
        StartCoroutine(BlinkText(tmp));

        // ── Wait until the player presses C ───────────────────────────────────
        yield return new WaitForSeconds(0.5f);   // brief delay so the boss's E input isn't captured
        while (!Input.GetKeyDown(KeyCode.C))
            yield return null;

        Destroy(canvasGO);
        Destroy(gameObject);
    }

    private IEnumerator BlinkText(TextMeshProUGUI tmp)
    {
        while (tmp != null)
        {
            tmp.color = new Color(1f, 0.9f, 0.35f);
            yield return new WaitForSeconds(0.6f);
            if (tmp == null) break;
            tmp.color = new Color(1f, 0.9f, 0.35f, 0.35f);
            yield return new WaitForSeconds(0.4f);
        }
    }
}
