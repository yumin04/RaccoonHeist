using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPull : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public Transform pullPos; // Point where player "holds" item (e.g., mouth/hand)
    
    [Header("Settings")]
    public float pickUpRange = 5f;
    [Tooltip("Offset from the player’s center for the OverlapSphere that detects nearby objects.")]
    public Vector3 pickUpSphereOffset = Vector3.zero;
    public float breakForce = 2000f;   // how much force can break the joint
    public float damping = 50f;        // rotational damping to reduce jitter
    public float grabsphereoffset = 1;
    

    private GameObject pulledObj;
    private Rigidbody pulledRb;
    private ConfigurableJoint joint;
    private PlayerMovement playerMovement;
    private Vector3 originalLocalPullPos;


    void Start()
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        originalLocalPullPos = pullPos.localPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (pulledObj == null)
                TryPickUpObject();
            else
                DropObject();
        }
    }

    public void TryPickUpObject()
    {
        Vector3 sphereCenter = player.transform.position + player.transform.TransformVector(pickUpSphereOffset);
        Collider[] nearbyObjects = Physics.OverlapSphere(sphereCenter, pickUpRange);
        foreach (Collider col in nearbyObjects)
        {
            if (!col.CompareTag("canPull")) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (rb == null) continue;

            pulledObj = rb.gameObject;
            pulledRb = rb;

            CreateJoint(col);

            playerMovement.isPulling = true;
            Debug.Log("Started pulling: " + pulledObj.name);
            break;
        }
    }

    private void CreateJoint(Collider col)
{
    
    // Step 1: find the closest grab point on the object
    Vector3 grabPointWorld = col.ClosestPoint(player.transform.position);

    // Step 2: instant aligning pullPos toward that grab point (smooth, optional)
    if (!IsPlayerAboveObject(col))
    {
        AlignPullPosToGrabPointInstant(col.ClosestPoint(player.transform.position));
    }

    // Step 3: create joint on the object
    joint = pulledObj.AddComponent<ConfigurableJoint>();
    Rigidbody playerRb = player.GetComponent<Rigidbody>();
    joint.connectedBody = playerRb;
    joint.autoConfigureConnectedAnchor = false;

    // Step 4: assign anchors using updated positions
    joint.anchor = pulledObj.transform.InverseTransformPoint(grabPointWorld);
    joint.connectedAnchor = player.transform.InverseTransformPoint(pullPos.position);

    // Step 5: set linear limit / motion
    SoftJointLimit softLimit = new SoftJointLimit { limit = 0.05f };
    joint.linearLimit = softLimit;
    joint.xMotion = ConfigurableJointMotion.Limited;
    joint.yMotion = ConfigurableJointMotion.Limited;
    joint.zMotion = ConfigurableJointMotion.Limited;

    // Step 6: allow free rotation for natural swing
    joint.angularXMotion = ConfigurableJointMotion.Free;
    joint.angularYMotion = ConfigurableJointMotion.Free;
    joint.angularZMotion = ConfigurableJointMotion.Free;

    // Step 7: damping setup to reduce jitter
    JointDrive dampedDrive = new JointDrive
    {
        positionSpring = 0f,
        positionDamper = damping,
        maximumForce = Mathf.Infinity
    };
    joint.xDrive = joint.yDrive = joint.zDrive = dampedDrive;

    // Step 8: safety and special cases
    joint.breakForce = breakForce;
    joint.breakTorque = breakForce;
    joint.enableCollision = true;
    
    if (pulledObj.GetComponent<HingeJoint>() != null)
    {
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Limited;

        SoftJointLimit limit = new SoftJointLimit { limit = 2f };
        joint.linearLimit = limit;
    }
}
    
    private void AlignPullPosToGrabPointInstant(Vector3 targetPos, float radius = 0.5f)
    {
        // Keep pull position on a circle around the player’s head, offset forward in X
        float facing = Mathf.Sign(player.transform.localScale.x);
        Vector3 circleCenter = player.transform.position + player.transform.right * grabsphereoffset * facing;
        targetPos.y = pullPos.position.y; // keep same Y level

        Vector3 offset = targetPos - circleCenter;
        if (offset.magnitude > radius)
            targetPos = circleCenter + offset.normalized * radius;

        pullPos.position = targetPos;
    }
    
    bool IsPlayerAboveObject(Collider obj)
    {
        Bounds objBounds = obj.bounds;
        Vector3 playerPos = player.transform.position;

        // Check horizontal overlap
        bool xOverlap = playerPos.x > objBounds.min.x && playerPos.x < objBounds.max.x;
        bool zOverlap = playerPos.z > objBounds.min.z && playerPos.z < objBounds.max.z;

        // Check if player's feet are above the top of the object
        bool above = playerPos.y > objBounds.max.y;

        return xOverlap && zOverlap && above;
    }


    
    private IEnumerator ReturnPullPosToOriginal(float duration = 0.2f)
    {
        Vector3 start = pullPos.localPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            pullPos.localPosition = Vector3.Lerp(start, originalLocalPullPos, t / duration);
            yield return null;
        }

        pullPos.localPosition = originalLocalPullPos;
    }


    public void DropObject()
    {
        if (joint != null)
            Destroy(joint);

        pulledObj = null;
        pulledRb = null;
        joint = null;
        playerMovement.isPulling = false;
        // Smoothly return to original local position
        StartCoroutine(ReturnPullPosToOriginal());
        Debug.Log("Stopped pulling");
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.cyan;

        // Determine facing direction
        float facing = 1f;
        if (player.transform.localScale.x < 0)
            facing = -1f;

        // Match the same offset and radius used in your code
        Vector3 circleCenter = player.transform.position + player.transform.right * grabsphereoffset * facing;
        float radius = 0.5f;

        Gizmos.DrawWireSphere(circleCenter, radius);
        
        Gizmos.color = Color.yellow;
        Vector3 sphereCenter = player.transform.position + player.transform.TransformVector(pickUpSphereOffset);
        Gizmos.DrawWireSphere(sphereCenter, pickUpRange);
    }

    public bool IsPulling() => pulledObj != null;

}