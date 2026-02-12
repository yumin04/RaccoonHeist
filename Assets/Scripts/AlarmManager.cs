using UnityEngine;
using System.Collections;

public class AlarmManager : MonoBehaviour
{
    public static AlarmManager Instance;

    [Header("Light Objects")]
    public GameObject[] alarmLights; // assign the rotating light meshes or point lights

    [Header("Alarm Settings")]
    public bool alarmActive = false;
    public float rotationSpeed = 180f;

    [Header("Activation Timing")]
    public float activationDelay = 0.5f; // beat-matching delay

    private Coroutine activationRoutine;

    [Header("Trigger Settings")]
    public bool deactivateOnExit = true;

    private void Awake()
    {
        Instance = this;

        // Ensure lights are OFF at the start
        SetLightsActive(false);
    }

    private void Update()
    {
        // Rotate lights only when the alarm is active
        if (alarmActive)
        {
            foreach (GameObject lightObj in alarmLights)
            {
                if (lightObj != null)
                    lightObj.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Cancel any previous activation if player re-enters
        if (activationRoutine != null)
            StopCoroutine(activationRoutine);

        activationRoutine = StartCoroutine(DelayedAlarmActivation());
    }

    private IEnumerator DelayedAlarmActivation()
    {
        yield return new WaitForSeconds(activationDelay);

        alarmActive = true;
        SetLightsActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!deactivateOnExit) return;

        alarmActive = false;
        SetLightsActive(false);

        if (activationRoutine != null)
            StopCoroutine(activationRoutine);
    }

    // Enables or disables all lights
    private void SetLightsActive(bool state)
    {
        foreach (GameObject lightObj in alarmLights)
        {
            if (lightObj != null)
                lightObj.SetActive(state);
        }
    }
}
