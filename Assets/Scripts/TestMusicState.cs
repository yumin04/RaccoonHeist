using UnityEngine;
using AK.Wwise;

public class TestMusicState : MonoBehaviour
{
    void Start()
    {
        Debug.Log("TestMusicState is running!");
        
        if (!AkSoundEngine.IsInitialized())
            Debug.Log("Wwise not initialized yet");
    }

    public string stateGroupName = "MusicState"; // Your Wwise State Group
    public string stateName = "Menu";           // The state to switch to

    void Update()
    {
        
        if (!AkSoundEngine.IsInitialized())
            Debug.Log("Wwise not initialized yet");

        // Press 1 to set this state
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AkSoundEngine.SetState(stateGroupName, stateName);
            Debug.Log($"Set state {stateName} in group {stateGroupName}");
        }

        // Press 2 to set another state for testing
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AkSoundEngine.SetState(stateGroupName, "PinkRaccoon"); // replace with your state
            Debug.Log("Set state PinkRaccoon");
        }

        // Press 3 for another example state
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AkSoundEngine.SetState(stateGroupName, "Chase"); // replace with your state
            Debug.Log("Set state Chase");
        }
    }
}