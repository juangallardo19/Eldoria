using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Observer — escucha onValueChanged del Slider para actualizar fillAmount
// en lugar del comportamiento por defecto que estira el RectTransform del Fill.
// Efecto: la imagen de relleno se revela progresivamente (efecto "before/after")
// sin deformarse ni salirse de los bordes.
// Nota: la configuración del RectTransform se aplica un frame después del Awake
// para que el DrivenRectTransformTracker del Slider libere el lock antes.
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
            Debug.LogWarning($"[SliderFillReveal] No se encontró Image en el Fill de '{name}'.", this);
            return;
        }

        // Desconectar fillRect aquí para que el Slider no siga conduciendo el Fill.
        _slider.fillRect = null;

        // Esperar un frame: el DrivenRectTransformTracker libera el lock después
        // de que todos los Awake/OnEnable del frame actual terminen.
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
