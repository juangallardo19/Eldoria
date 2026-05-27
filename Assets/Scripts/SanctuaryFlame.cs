using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Santuario de Ara — llama azul interactiva con parpadeo y prompt de descanso.
// Patrón State: Idle (solo parpadeo) / Near (prompt visible + partículas intensificadas).
// Al presionar E cerca: guarda checkpoint (escena + posición) y restaura vidas.
public class SanctuaryFlame : MonoBehaviour
{
    [Header("Referencias")]
    public ParticleSystem flameParticles;
    public SpriteRenderer glowRenderer;
    public TextMeshPro promptText;

    [Header("Parpadeo")]
    [SerializeField] private float flickerSpeed     = 3.5f;
    [SerializeField] private float flickerAmplitude = 0.28f;
    [SerializeField] private float baseAlpha        = 0.55f;

    [Header("Proximidad")]
    [SerializeField] private float interactRadius = 10f;

    [Header("Descanso")]
    [SerializeField] private string restPrompt    = "Descansar [E]";
    [SerializeField] private string restingMessage = "Descansando...";

    private Transform _player;
    private bool      _playerNear;
    private bool      _resting;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        if (promptText != null)
        {
            // Fuente, tamaño y bold se aplican en código para consistencia entre escenas
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
        // Parpadeo orgánico de la llama
        if (glowRenderer != null)
        {
            float alpha = baseAlpha
                + Mathf.Sin(Time.time * flickerSpeed)          * flickerAmplitude
                + Mathf.Sin(Time.time * flickerSpeed * 2.17f)  * flickerAmplitude * 0.4f;
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

        // Detección horizontal: solo distancia X, tolerante a diferencia de altura
        bool near = Mathf.Abs(_player.position.x - transform.position.x) <= interactRadius;

        if (near != _playerNear)
        {
            _playerNear = near;
            if (promptText != null && !_resting)
                promptText.gameObject.SetActive(near);

            if (flameParticles != null)
            {
                var emission = flameParticles.emission;
                emission.rateOverTime = near ? 38f : 20f;
            }
        }

        // Interacción: E para descansar
        if (_playerNear && !_resting && Input.GetKeyDown(KeyCode.E))
            StartCoroutine(Rest());
    }

    private IEnumerator Rest()
    {
        _resting = true;

        // Guardar checkpoint en PlayerPrefs
        string sceneName = SceneManager.GetActiveScene().name;
        float  sx        = _player.position.x;
        float  sy        = _player.position.y;
        PlayerPrefs.SetString("SanctuaryScene", sceneName);
        PlayerPrefs.SetFloat("SanctuaryX", sx);
        PlayerPrefs.SetFloat("SanctuaryY", sy);
        PlayerPrefs.Save();

        // Guardar santuario en el slot activo — respawn al cargar partida
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

        // Restaurar vidas (funciona aunque la escena no tenga CrystalRespawnManager)
        CrystalRespawnManager.RestoreLivesGlobal();

        // Feedback visual
        if (promptText != null)
        {
            promptText.text = restingMessage;
            promptText.gameObject.SetActive(true);
        }

        // Intensificar llama brevemente
        if (flameParticles != null)
        {
            var emission = flameParticles.emission;
            emission.rateOverTime = 80f;
        }

        yield return new WaitForSeconds(2f);

        if (flameParticles != null)
        {
            var emission = flameParticles.emission;
            emission.rateOverTime = _playerNear ? 38f : 20f;
        }

        if (promptText != null)
        {
            promptText.text = restPrompt;
            promptText.gameObject.SetActive(_playerNear);
        }

        _resting = false;
    }
}
