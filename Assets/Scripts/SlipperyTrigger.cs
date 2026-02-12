using UnityEngine;
using UnityEngine.Events;

public class SlipperyTrigger : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onPlayerEnter;
    public UnityEvent onNPCEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[SlipperyTrigger] Player entered.");
            onPlayerEnter?.Invoke();
        }

        if (other.CompareTag("NPC"))
        {
            Debug.Log("[SlipperyTrigger] NPC entered.");
            onNPCEnter?.Invoke();
        }
    }
}