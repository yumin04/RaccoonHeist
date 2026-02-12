using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    public GameObject startButton;
    // Called when Start Game button is clicked
    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(startButton);
    }
    
    public void StartGame()
    {
        SceneManager.LoadScene("Main scene");
    }

    // Called when Quit button is clicked
    public void QuitGame()
    {
        Debug.Log("Quit button pressed!"); // For testing in the editor
        Application.Quit(); // Actually quits the application (works in builds, not in editor)
    }
}
