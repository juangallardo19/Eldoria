using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Sanctuary of Ara — interactive blue flame with flicker and rest prompt.
// Pattern State: Idle (flicker only) / Near (prompt visible + intensified particles).
// Press E near it: saves checkpoint (scene + position) and restores lives.
public class SanctuaryFlame : MonoBehaviour
{
    public static event System.Action OnRested;

    [Header("References")]
    public ParticleSystem flameParticles;
    public SpriteRenderer glowRenderer;
    public TextMeshPro promptText;

    [Header("Flicker")]
    [SerializeField] private float flickerSpeed     = 3.5f;
    [SerializeField] private float flickerAmplitude = 0.28f;
    [SerializeField] private float baseAlpha        = 0.55f;

    [Header("Proximity")]
    [SerializeField] private float interactRadius = 10f;

    [Header("Rest")]
    [SerializeField] private string restPrompt     = "Descansar [E]";
    [SerializeField] private string restingMessage = "Descansando...";

    private Transform _player;
    private bool      _playerNear;
    private bool      _resting;
    private float     _nearTimer;
    private const float InteractHoldDuration  = 0.4f;  // seconds player must be near before E works
    private const float EMISSION_NEAR         = 38f;
    private const float EMISSION_IDLE         = 20f;
    private const float EMISSION_RESTING      = 80f;
    private const float SECONDARY_FLICKER_MULT = 0.4f; // harmonic at 2.17× speed, 40% amplitude

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        if (promptText != null)
        {
#if UNITY_EDITOR
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
            var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
            if (font != null) promptText.font = font;
            promptText.fontSize  = 8.0f;
            promptText.fontStyle = TMPro.FontStyles.Bold;
            promptText.text = restPrompt;
            promptText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (glowRenderer != null)
        {
            float alpha = baseAlpha
                + Mathf.Sin(Time.time * flickerSpeed)          * flickerAmplitude
                + Mathf.Sin(Time.time * flickerSpeed * 2.17f)  * flickerAmplitude * SECONDARY_FLICKER_MULT;
            var c = glowRenderer.color;
            c.a = Mathf.Clamp01(alpha);
            glowRenderer.color = c;
        }

        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
        if (_player == null) return;

        // Horizontal-only proximity check: tolerant of height differences
        bool near = Mathf.Abs(_player.position.x - transform.position.x) <= interactRadius;

        if (near != _playerNear)
        {
            _playerNear = near;
            _nearTimer  = 0f;
            if (promptText != null && !_resting)
                promptText.gameObject.SetActive(near);

            if (flameParticles != null)
            {
                var emission = flameParticles.emission;
                emission.rateOverTime = near ? EMISSION_NEAR : EMISSION_IDLE;
            }
        }

        if (_playerNear) _nearTimer += Time.deltaTime;

        if (_playerNear && !_resting && _nearTimer >= InteractHoldDuration && Input.GetKeyDown(KeyCode.E))
            StartCoroutine(Rest());
    }

    private IEnumerator Rest()
    {
        _resting = true;

        string sceneName = SceneManager.GetActiveScene().name;
        float  sx        = _player.position.x;
        float  sy        = _player.position.y;
        PlayerPrefs.SetString(EldoriaPrefsKeys.SanctuaryScene, sceneName);
        PlayerPrefs.SetFloat(EldoriaPrefsKeys.SanctuaryX, sx);
        PlayerPrefs.SetFloat(EldoriaPrefsKeys.SanctuaryY, sy);
        PlayerPrefs.Save();

        if (SaveManager.ActiveSlot >= 0 && SaveManager.Instance != null)
        {
            var sdata = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            if (sdata != null)
            {
                sdata.sanctuaryScene = sceneName;
                sdata.sanctuaryX     = sx;
                sdata.sanctuaryY     = sy;
                SaveManager.Instance.Save(SaveManager.ActiveSlot, sdata);
            }
        }

        CrystalRespawnManager.RestoreLivesGlobal();
        OnRested?.Invoke();

        if (promptText != null)
        {
            promptText.text = restingMessage;
            promptText.gameObject.SetActive(true);
        }

        if (flameParticles != null)
        {
            var emission = flameParticles.emission;
            emission.rateOverTime = EMISSION_RESTING;
        }

        yield return new WaitForSeconds(2f);

        if (flameParticles != null)
        {
            var emission = flameParticles.emission;
            emission.rateOverTime = _playerNear ? EMISSION_NEAR : EMISSION_IDLE;
        }

        if (promptText != null)
        {
            promptText.text = restPrompt;
            promptText.gameObject.SetActive(_playerNear);
        }

        _resting = false;
    }
}
