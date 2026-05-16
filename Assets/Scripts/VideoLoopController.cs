using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoLoopController : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.isLooping  = true;
        videoPlayer.playOnAwake = true;
        videoPlayer.loopPointReached += OnLoopPointReached;
    }

    void Start()
    {
        videoPlayer.Play();
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        vp.time = 0;
        vp.Play();
    }
}
