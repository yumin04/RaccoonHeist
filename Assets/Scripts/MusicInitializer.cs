using UnityEngine;

public class MusicInitializer : MonoBehaviour
{
    void Start()
    {
        AkSoundEngine.SetState("MusicState", "Menu");
    }
}