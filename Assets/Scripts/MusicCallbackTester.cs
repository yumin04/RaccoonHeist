using UnityEngine;

public class MusicCallbackTester : MonoBehaviour
{
    public AK.Wwise.Event musicEvent;  // assign your music start event in the Inspector

    private void Start()
    {
        // Post the event with callback flags
        uint playingId = musicEvent.Post(gameObject, 
            (uint)(AkCallbackType.AK_MusicSyncUserCue), 
            MusicCallback);

        Debug.Log("Posted music event with ID = " + playingId);
    }

    // This function is triggered when a USER CUE is hit in Wwise
    private void MusicCallback(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            AkMusicSyncCallbackInfo cueInfo = (AkMusicSyncCallbackInfo)in_info;
            Debug.Log("🎵 USER CUE reached! Cue Name: " + cueInfo.userCueName);
        }
    }
}