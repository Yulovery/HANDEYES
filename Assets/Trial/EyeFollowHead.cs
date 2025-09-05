using UnityEngine;

public class EyeFollowHead : MonoBehaviour
{
    [Header("Assign the XR head reference (a transform that tracks the HMD center eye)")]
    public Transform headRef;

    [Tooltip("If true, eye sticks to head pose; set false when grabbed.")]
    public bool followHead = true;

    [Tooltip("Local offset from head (set on Start from current pose).")]
    public Vector3 localPosOffset;
    public Quaternion localRotOffset = Quaternion.identity;

    void Start()
    {
        if (headRef != null)
        {
            // Record initial relative offset so the eye sits where you placed it
            var headInv = Quaternion.Inverse(headRef.rotation);
            localPosOffset = headInv * (transform.position - headRef.position);
            localRotOffset = headInv * transform.rotation;
        }
    }

    void LateUpdate()
    {
        if (!followHead || headRef == null) return;

        transform.position = headRef.position + headRef.rotation * localPosOffset;
        transform.rotation = headRef.rotation * localRotOffset;
    }
}