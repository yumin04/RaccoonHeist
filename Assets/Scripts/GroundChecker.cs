using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [Header("References")]
    public Transform groundCheckPoint;
    public LayerMask groundMask;

    [Header("Settings")]
    public float groundRayLength = 0.3f;
    public float startHeight = 0.25f;
    public float raySpacing = 0.25f;
    public int rayCount = 4;

    [Header("State (Read-Only)")]
    public bool isGrounded;
    public GameObject currentGround;
    public string currentSurfaceTag;

    void Update()
    {
        PerformGroundCheck();
    }

    public void PerformGroundCheck()
    {
        if (groundCheckPoint == null) return;

        bool hitSomething = false;
        GameObject hitObject = null;

        Vector3 basePos = groundCheckPoint.position;
        Vector3 right = transform.right;

        // Cast multiple rays left to right
        for (int i = 0; i < rayCount; i++)
        {
            float offsetAmount = (i - (rayCount - 1) / 2f) * raySpacing;
            Vector3 origin = basePos + right * offsetAmount + Vector3.up * startHeight;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayLength, groundMask))
            {
                Debug.DrawRay(origin, Vector3.down * groundRayLength, Color.green);
                hitSomething = true;
                hitObject = hit.collider.gameObject;
                break; // stop on first valid hit
            }
            else
            {
                Debug.DrawRay(origin, Vector3.down * groundRayLength, Color.red);
            }
        }

        isGrounded = hitSomething;
        currentGround = hitSomething ? hitObject : null;
        currentSurfaceTag = hitSomething ? hitObject.tag : "";
    }

    // ---------- NEW ----------
    /// <summary>
    /// Returns true when the player is grounded AND the object under the player's feet
    /// is the same root GameObject as the supplied object.
    /// This handles child colliders by comparing root transforms.
    /// </summary>
    public bool IsGroundedOnObject(GameObject obj)
    {
        if (!isGrounded || currentGround == null || obj == null) return false;

        // Compare root transforms so children of the same prefab/structure count as the same object
        return currentGround.transform.root == obj.transform.root;
    }
    // --------------------------

    public bool IsOnSurface(string tag)
    {
        return isGrounded && currentSurfaceTag == tag;
    }

    public bool IsOnTagFromArray(string[] tags)
    {
        if (!isGrounded || currentGround == null) return false;
        foreach (string tag in tags)
        {
            if (currentGround.CompareTag(tag))
                return true;
        }
        return false;
    }
    
    public bool IsOnLayer(LayerMask layerMask)
    {
        if (!isGrounded || currentGround == null) return false;
        return ((1 << currentGround.layer) & layerMask) != 0;
    }
    
    /// <summary>
    /// Returns the layer of the object the player is currently grounded on.
    /// Returns -1 if not grounded.
    /// </summary>
    public int GetGroundLayer()
    {
        if (!isGrounded || currentGround == null) return -1;
        return currentGround.layer;
    }


}
