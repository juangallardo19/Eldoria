using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Pattern: Observer — listens to the Slider's onValueChanged to update fillAmount
// instead of the default behaviour that stretches the Fill RectTransform.
// Effect: the fill image is revealed progressively (before/after wipe)
// without distorting or overflowing its borders.
// Note: RectTransform setup runs one frame after Awake so the Slider's
// DrivenRectTransformTracker releases its lock first.
[RequireComponent(typeof(Slider))]
public class SliderFillReveal : MonoBehaviour
{
    private Slider _slider;
    private Image  _fillImage;

    void Awake()
    {
        _slider = GetComponent<Slider>();

        _fillImage = _slider.fillRect != null
            ? _slider.fillRect.GetComponent<Image>()
            : null;

        if (_fillImage == null)
        {
            Debug.LogWarning($"[SliderFillReveal] No Image found on Fill of '{name}'.", this);
            return;
        }

        // Disconnect fillRect so the Slider no longer drives the Fill.
        _slider.fillRect = null;

        // Wait one frame: the DrivenRectTransformTracker releases its lock after
        // all Awake/OnEnable calls of the current frame have completed.
        StartCoroutine(SetupNextFrame());
    }

    private IEnumerator SetupNextFrame()
    {
        yield return null;

        RectTransform rt = _fillImage.rectTransform;
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        _fillImage.maskable   = true;
        _fillImage.type       = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        _slider.onValueChanged.AddListener(OnValueChanged);
        OnValueChanged(_slider.value);
    }

    void OnDestroy()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(float _) =>
        _fillImage.fillAmount = _slider.normalizedValue;
}
