using UnityEngine;
using UnityEngine.Events;

public class WindowEventTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("Delay")]
    public float delay = 0f; // delay before the event fires

    [Header("Events")]
    public UnityEvent onPlayerEnter;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;

        hasTriggered = true;
        StartCoroutine(TriggerWithDelay());
    }

    private System.Collections.IEnumerator TriggerWithDelay()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        onPlayerEnter.Invoke();
    }
}