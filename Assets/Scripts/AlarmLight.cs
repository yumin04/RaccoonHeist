using UnityEngine;

public class AlarmLight : MonoBehaviour
{
    public Transform lightLeft;
    public Transform lightRight;

    void Update()
    {
        if (AlarmManager.Instance == null || !AlarmManager.Instance.alarmActive)
            return;

        float speed = AlarmManager.Instance.rotationSpeed;
        float rotation = speed * Time.unscaledDeltaTime; // sync-safe

        // Rotate both lights identically
        lightLeft.Rotate(Vector3.up, rotation, Space.Self);
        lightRight.Rotate(Vector3.up, rotation, Space.Self);
    }
}