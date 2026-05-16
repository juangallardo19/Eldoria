using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Control de selección estilo "< Opción >" con botones izquierda/derecha.
/// Reemplaza al TMP_Dropdown para opciones simples con pocos valores.
public class SelectionControl : MonoBehaviour
{
    [SerializeField] private Button    leftButton;
    [SerializeField] private Button    rightButton;
    [SerializeField] private TMP_Text  valueLabel;

    private List<string> _options = new List<string>();
    private int          _index   = 0;

    public int value
    {
        get => _index;
        set => SetIndex(value, notify: false);
    }

    public event System.Action<int> onValueChanged;

    void Awake()
    {
        leftButton ?.onClick.AddListener(Previous);
        rightButton?.onClick.AddListener(Next);
    }

    public void SetOptions(List<string> options)
    {
        _options = options ?? new List<string>();
        UpdateLabel();
    }

    private void Previous()
    {
        int next = _index - 1;
        if (next < 0) next = _options.Count - 1;
        SetIndex(next, notify: true);
    }

    private void Next()
    {
        int next = (_index + 1) % Mathf.Max(1, _options.Count);
        SetIndex(next, notify: true);
    }

    private void SetIndex(int idx, bool notify)
    {
        _index = Mathf.Clamp(idx, 0, Mathf.Max(0, _options.Count - 1));
        UpdateLabel();
        if (notify) onValueChanged?.Invoke(_index);
    }

    private void UpdateLabel()
    {
        if (valueLabel == null) return;
        valueLabel.text = _options.Count > 0 ? _options[_index] : "—";
    }
}
