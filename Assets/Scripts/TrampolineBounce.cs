using UnityEngine;

public class TrampolineBounce : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceHeight = 10f; // upward force applied

    private void OnTriggerEnter(Collider other)
    {
        // 1️⃣ Handle CharacterController-based player
        var controller = other.GetComponent<CharacterController>();
        if (controller != null)
        {
            var player = controller.GetComponent<PlayerMovementCC>();
            if (player != null)
            {
                //player.ApplyExternalVelocity(Vector3.up * bounceHeight);
                return; // done
            }
        }

        // 2️⃣ Handle Rigidbody-based objects
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            Vector3 lv = rb.linearVelocity;   // use modern API
            lv.y = bounceHeight;              // set vertical velocity
            rb.linearVelocity = lv;           // apply
        }
    }

    // Optional: visualize trampoline in scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}