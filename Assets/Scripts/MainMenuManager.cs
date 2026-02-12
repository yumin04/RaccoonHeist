using UnityEngine;
using UnityEngine.EventSystems;
using AK.Wwise;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu UI")]
    public GameObject mainMenuCanvas;
    public GameObject startButton;

    [Header("Wwise")]
    public AK.Wwise.State GameplayState;

    public AK.Wwise.Event musicEvent;    // <— assign the SAME event that starts the music transition

    public PauseMenu pauseMenu;

    private bool hasStartedGameplay = false;

    void Awake()
    {
        LoadMainMenu();
        uint playingId = musicEvent.Post(gameObject, 
            (uint)(AkCallbackType.AK_MusicSyncUserCue), 
            MusicCallback);

        Debug.Log("Posted music event with ID = " + playingId);
    }

    public void LoadMainMenu()
    {
        mainMenuCanvas.SetActive(true);

        Time.timeScale = 0f;
        Physics.autoSimulation = false;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(startButton);
    }

    public void ShowMainMenu()
    {
        LoadMainMenu();
    }

    // --------------------------------------------------------------------
    // PLAYER PRESSES START — ONLY change music + wait for cue
    // --------------------------------------------------------------------
    public void StartGame()
    {
        Debug.Log("Start button pressed — changing Wwise state and posting event with callback.");

        // Apply Wwise State
        GameplayState?.SetValue();
        
    }
    
    private void MusicCallback(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            AkMusicSyncCallbackInfo cueInfo = (AkMusicSyncCallbackInfo)in_info;
            Debug.Log("🎵 USER CUE reached! Cue Name: " + cueInfo.userCueName);
            RunGameplayStartLogic();
        }
    }


    // --------------------------------------------------------------------
    // REAL GAME START LOGIC (only called after cue)
    // --------------------------------------------------------------------
    private void RunGameplayStartLogic()
    {
        // Hide menu
        mainMenuCanvas.SetActive(false);
        PauseMenu.GameIsPaused = false;

        // Resume gameplay
        Time.timeScale = 1f;
        Physics.autoSimulation = true;

        if (pauseMenu != null)
            pauseMenu.ResetPauseState();

        Debug.Log("Gameplay officially started.");
    }

    public void QuitGame()
    {
        Debug.Log("Quit button pressed!");
        Application.Quit();
    }
}
