using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject quitConfirmPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button quitYesButton;
    [SerializeField] private Button quitNoButton;

    [Header("Menu Music")]
    [SerializeField] private AudioClip menuMusic;

    void Start()
    {
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);

        if (playButton)    playButton   .onClick.AddListener(OnPlay);
        if (optionsButton) optionsButton.onClick.AddListener(OnOptions);
        if (quitButton)    quitButton   .onClick.AddListener(OnQuit);
        if (quitYesButton) quitYesButton.onClick.AddListener(OnQuitConfirm);
        if (quitNoButton)  quitNoButton .onClick.AddListener(OnQuitCancel);

        // Only MainMenu starts the music; other scenes stop or replace it
        AudioManager.Instance?.PlayMusic(menuMusic);
    }

    private void OnPlay()    => SceneFader.Instance.LoadScene(EldoriaSceneNames.SlotsScreen);
    private void OnOptions() => SceneFader.Instance.LoadScene(EldoriaSceneNames.Settings);

    private void OnQuit()       => quitConfirmPanel.SetActive(true);
    private void OnQuitCancel() => quitConfirmPanel.SetActive(false);

    private void OnQuitConfirm()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
