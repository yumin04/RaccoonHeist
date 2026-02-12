using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public GameObject pauseMenuUI;        
    public GameObject resumeButton;       
    public GameObject pausePanel;
    public GameObject controlsPanel;
    
    [Header("References")]
    public MainMenuManager MainMenuManager;         
    public GameObject pauseMenuCanvas;

    public ScrollRect controlsScrollRect;
    
    [SerializeField] private GameObject mapImage;
    private bool isMapOpen = false;

    void Start()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;
        Physics.autoSimulation = true;
        GameIsPaused = false;

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (mapImage != null)
            mapImage.SetActive(false);

        // Ensure Wwise starts unpaused
        AkSoundEngine.SetState("PauseState", "Unpaused");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
                Resume();
            else
                Pause();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (!isMapOpen)
                OpenMap();
            else
                CloseMap();
        }
    }

    private void OpenMap()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        if (mapImage != null) mapImage.SetActive(true);
        isMapOpen = true;

        Time.timeScale = 0f;
        Physics.autoSimulation = false;
        GameIsPaused = true;

        // 🔊 WWISE: Enter Pause
        AkSoundEngine.SetState("PauseState", "Paused");
    }

    private void CloseMap()
    {
        if (mapImage != null) mapImage.SetActive(false);
        isMapOpen = false;

        if ((pauseMenuUI == null || !pauseMenuUI.activeSelf) &&
            (controlsPanel == null || !controlsPanel.activeSelf))
        {
            Time.timeScale = 1f;
            Physics.autoSimulation = true;
            GameIsPaused = false;

            // 🔊 WWISE: Leave Pause
            AkSoundEngine.SetState("PauseState", "Unpaused");
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        Time.timeScale = 1f;
        Physics.autoSimulation = true;
        GameIsPaused = false;

        if (mapImage != null) mapImage.SetActive(false);
        isMapOpen = false;

        EventSystem.current.SetSelectedGameObject(null);

        // 🔊 WWISE: Leave Pause
        AkSoundEngine.SetState("PauseState", "Unpaused");
    }

    void Pause()
    {
        EnsurePauseCanvasActive();

        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        Physics.autoSimulation = false;
        GameIsPaused = true;

        EventSystem.current.SetSelectedGameObject(null);
        if (resumeButton != null)
            EventSystem.current.SetSelectedGameObject(resumeButton);

        // 🔊 WWISE: Enter Pause
        AkSoundEngine.SetState("PauseState", "Paused");
    }

    public void Controls()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);

        if (controlsScrollRect != null)
        {
            controlsScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void BackButton()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void LoadMenu()
    {
        // Hide the pause menu panel (but not the entire canvas)
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        // --- 🔊 WWISE: Reset Pause State ---
        AkSoundEngine.SetState("PauseState", "Unpaused");

        // --- 🔊 WWISE: Switch to "None" BEFORE reload ---
        AkSoundEngine.SetState("MusicState", "None");

        // --- 🔊 STOP ALL SOUND (critical fix) ---
        AkSoundEngine.StopAll();

        // Reset internal pause state
        ResetPauseState();

        // --- 🔁 RELOAD CURRENT SCENE (FULL RESET) ---
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    public void ResetPauseState()
    {
        GameIsPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (mapImage != null) mapImage.SetActive(false);
        isMapOpen = false;

        Time.timeScale = 1f;
        Physics.autoSimulation = true;
        EventSystem.current.SetSelectedGameObject(null);

        // 🔊 WWISE: Leave Pause (safety)
        AkSoundEngine.SetState("PauseState", "Unpaused");
    }

    private void EnsurePauseCanvasActive()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);
            return;
        }

        if (pauseMenuUI == null) return;

        Transform t = pauseMenuUI.transform;
        Transform top = t;
        while (t.parent != null)
        {
            t = t.parent;
            top = t;
        }

        if (top != null)
            top.gameObject.SetActive(true);
    }
}
