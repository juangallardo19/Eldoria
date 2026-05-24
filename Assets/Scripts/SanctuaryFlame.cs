using UnityEngine;
using TMPro;

// Santuario de Ara — llama azul interactiva con parpadeo y prompt de descanso.
// Patrón: State — Idle (solo parpadeo) / Near (prompt visible + partículas intensificadas).
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
    [SerializeField] private float interactRadius = 6f;

    private Transform _player;
    private bool _playerNear;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Parpadeo — doble frecuencia para efecto orgánico
        if (glowRenderer != null)
        {
            float alpha = baseAlpha
                + Mathf.Sin(Time.time * flickerSpeed)          * flickerAmplitude
                + Mathf.Sin(Time.time * flickerSpeed * 2.17f)  * flickerAmplitude * 0.4f;
            var c = glowRenderer.color;
            c.a = Mathf.Clamp01(alpha);
            glowRenderer.color = c;
        }

        if (_player == null) return;

        bool near = Vector2.Distance(transform.position, _player.position) <= interactRadius;
        if (near == _playerNear) return;

        _playerNear = near;

        if (promptText != null)
            promptText.gameObject.SetActive(near);

        // Intensificar emisión de partículas al estar cerca
        if (flameParticles != null)
        {
            var emission = flameParticles.emission;
            emission.rateOverTime = near ? 38f : 20f;
        }
    }
}
