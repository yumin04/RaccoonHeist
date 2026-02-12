using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); // kill duplicates
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}


