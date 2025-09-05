using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class EyeballRecallManager : MonoBehaviour
{
    [Header("Eyeballs (root transforms that move)")]
    public Transform leftEyeball;     // Eyeball root (has Rigidbody + XRGrabInteractable)
    public Transform rightEyeball;

    [Header("Controller Attach Points")]
    public Transform leftHandAttach;  // Where the eyeball should hover near left hand
    public Transform rightHandAttach; // Where the eyeball should hover near right hand

    [Header("Input Actions (press & hold to summon/hover)")]
    public InputActionReference recallLeftAction;   // e.g., Left Hand - Primary Button (Press)
    public InputActionReference recallRightAction;  // e.g., Right Hand - Primary Button (Press)

    [Header("Flight & Hover Tuning")]
    [Tooltip("Seconds to fly from current position to hand on press.")]
    [Range(0.01f, 1.0f)]
    public float flyDuration = 0.20f;

    [Tooltip("How quickly the eyeball follows while the button is held (higher = snappier).")]
    [Range(1f, 30f)]
    public float hoverFollowLerp = 15f;

    [Tooltip("Local position offset relative to the hand attach (per hand).")]
    public Vector3 leftLocalPosOffset = new Vector3(0f, 0f, 0.10f);
    public Vector3 rightLocalPosOffset = new Vector3(0f, 0f, 0.10f);

    [Tooltip("Local rotation offset relative to the hand attach (per hand).")]
    public Vector3 leftLocalEulerOffset = new Vector3(0f, 0f, 0f);
    public Vector3 rightLocalEulerOffset = new Vector3(0f, 0f, 0f);

    [Header("Physics/Grab Options")]
    [Tooltip("Disable grabbing while being recalled/hovering (prevents tug-of-war).")]
    public bool disableGrabWhileHeld = true;

    [Tooltip("Make Rigidbody kinematic while hovering (recommended).")]
    public bool kinematicWhileHeld = true;

    // Internals
    Rigidbody leftRB, rightRB;
    XRGrabInteractable leftGrab, rightGrab;
    Coroutine leftRoutine, rightRoutine;
    bool leftHeld, rightHeld;

    void Awake()
    {
        Cache(leftEyeball, ref leftRB, ref leftGrab);
        Cache(rightEyeball, ref rightRB, ref rightGrab);
    }

    void OnEnable()
    {
        if (recallLeftAction != null)
        {
            recallLeftAction.action.performed += OnLeftPerformed;
            recallLeftAction.action.canceled  += OnLeftCanceled;
            recallLeftAction.action.Enable();
        }

        if (recallRightAction != null)
        {
            recallRightAction.action.performed += OnRightPerformed;
            recallRightAction.action.canceled  += OnRightCanceled;
            recallRightAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (recallLeftAction != null)
        {
            recallLeftAction.action.performed -= OnLeftPerformed;
            recallLeftAction.action.canceled  -= OnLeftCanceled;
            recallLeftAction.action.Disable();
        }
        if (recallRightAction != null)
        {
            recallRightAction.action.performed -= OnRightPerformed;
            recallRightAction.action.canceled  -= OnRightCanceled;
            recallRightAction.action.Disable();
        }
    }

    void Cache(Transform root, ref Rigidbody rb, ref XRGrabInteractable grab)
    {
        if (!root) return;
        rb   = root.GetComponent<Rigidbody>();
        grab = root.GetComponent<XRGrabInteractable>();
    }

    // -------- Left Hand --------
    void OnLeftPerformed(InputAction.CallbackContext ctx)
    {
        if (!leftEyeball || !leftHandAttach) return;
        leftHeld = true;
        if (leftRoutine != null) StopCoroutine(leftRoutine);
        leftRoutine = StartCoroutine(RecallAndHover(
            left: true,
            eyeball: leftEyeball,
            rb: leftRB,
            grab: leftGrab,
            handAttach: leftHandAttach,
            localPos: leftLocalPosOffset,
            localRotEuler: leftLocalEulerOffset
        ));
    }

    void OnLeftCanceled(InputAction.CallbackContext ctx)
    {
        leftHeld = false;
        // Routine will exit hover and restore physics/grab
    }

    // -------- Right Hand --------
    void OnRightPerformed(InputAction.CallbackContext ctx)
    {
        if (!rightEyeball || !rightHandAttach) return;
        rightHeld = true;
        if (rightRoutine != null) StopCoroutine(rightRoutine);
        rightRoutine = StartCoroutine(RecallAndHover(
            left: false,
            eyeball: rightEyeball,
            rb: rightRB,
            grab: rightGrab,
            handAttach: rightHandAttach,
            localPos: rightLocalPosOffset,
            localRotEuler: rightLocalEulerOffset
        ));
    }

    void OnRightCanceled(InputAction.CallbackContext ctx)
    {
        rightHeld = false;
    }

    IEnumerator RecallAndHover(
        bool left,
        Transform eyeball,
        Rigidbody rb,
        XRGrabInteractable grab,
        Transform handAttach,
        Vector3 localPos,
        Vector3 localRotEuler)
    {
        // If an EyeFollowHead script exists, disable follow while we take control
        var follow = eyeball.GetComponent<EyeFollowHead>();
        if (follow) follow.followHead = false;

        // Temporarily disable grabbing while held
        if (disableGrabWhileHeld && grab) grab.enabled = false;

        // Prep physics
        bool prevKinematic = rb ? rb.isKinematic : false;
        if (rb && kinematicWhileHeld) rb.isKinematic = true;

        // Flight target (compute world-space pose from hand attach + offsets)
        Quaternion rotOffset = Quaternion.Euler(localRotEuler);

        // ---- Fly-in ----
        float t = 0f;
        Vector3 startPos = eyeball.position;
        Quaternion startRot = eyeball.rotation;

        while (t < flyDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / flyDuration);

            Vector3 targetPos = handAttach.TransformPoint(localPos);
            Quaternion targetRot = handAttach.rotation * rotOffset;

            eyeball.position = Vector3.Lerp(startPos, targetPos, EaseOutCubic(u));
            eyeball.rotation = Quaternion.Slerp(startRot, targetRot, EaseOutCubic(u));
            yield return null;
        }

        // Snap to exact
        eyeball.position = handAttach.TransformPoint(localPos);
        eyeball.rotation = handAttach.rotation * rotOffset;

        // ---- Hover follow while held ----
        while (left ? leftHeld : rightHeld)
        {
            Vector3 targetPos = handAttach.TransformPoint(localPos);
            Quaternion targetRot = handAttach.rotation * rotOffset;

            // Smooth follow
            eyeball.position = Vector3.Lerp(eyeball.position, targetPos, 1f - Mathf.Exp(-hoverFollowLerp * Time.deltaTime));
            eyeball.rotation = Quaternion.Slerp(eyeball.rotation, targetRot, 1f - Mathf.Exp(-hoverFollowLerp * Time.deltaTime));

            yield return null;
        }

        // ---- Release: restore physics & grabbing ----
        if (rb) rb.isKinematic = prevKinematic;
        if (disableGrabWhileHeld && grab) grab.enabled = true;
    }

    static float EaseOutCubic(float x)
    {
        // nice quick ease for the fly-in
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
