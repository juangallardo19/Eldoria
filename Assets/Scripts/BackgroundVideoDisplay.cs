using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Strategy — decide en Start() si el video persiste (navegando desde MainMenu)
// o si no hay manager (escena abierta directamente en el editor).
// En ambos casos nunca lanza excepciones; simplemente no muestra nada si no hay video.
[RequireComponent(typeof(RawImage))]
public class BackgroundVideoDisplay : MonoBehaviour
{
    void Start()
    {
        if (BackgroundVideoManager.Instance == null) return;

        VideoPlayer vp = BackgroundVideoManager.Instance.GetComponent<VideoPlayer>();
        if (vp == null || vp.targetTexture == null) return;

        GetComponent<RawImage>().texture = vp.targetTexture;
    }
}
