using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Tutorial de dash — aparece tras desbloquear el dash y no desaparece hasta que el jugador presione C.
// Se instancia como GO independiente desde BossObsesion.DefeatedSequence.
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

        // ── Fondo semi-transparente ───────────────────────────────────────────
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

        // ── Texto ─────────────────────────────────────────────────────────────
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "✦ DASH DESBLOQUEADO ✦\nPresiona [ C ] para activarlo";
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(1f, 0.9f, 0.35f);
        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 88f);
        rt.sizeDelta        = new Vector2(600f, 60f);

        // ── Parpadeo del texto ─────────────────────────────────────────────────
        StartCoroutine(BlinkText(tmp));

        // ── Esperar hasta que el jugador presione C ────────────────────────────
        yield return new WaitForSeconds(0.5f);   // breve delay para no leer la E del boss
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
