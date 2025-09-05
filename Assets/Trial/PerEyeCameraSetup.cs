using UnityEngine;

public class PerEyeCameraSetup : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Camera leftEyeCam;
    public Camera rightEyeCam;

    [Tooltip("Any legacy/placeholder HMD camera to disable (e.g., XR Origin Main Camera).")]
    public Camera hmdCenterCameraToDisable;

    void Awake()
    {
        if (leftEyeCam == null || rightEyeCam == null)
        {
            Debug.LogError("[PerEyeCameraSetup] Assign both left and right eye Camera references.");
            enabled = false;
            return;
        }

        // Ensure they are not tagged as MainCamera
        if (leftEyeCam.CompareTag("MainCamera")) leftEyeCam.tag = "Untagged";
        if (rightEyeCam.CompareTag("MainCamera")) rightEyeCam.tag = "Untagged";

        // Built-in RP per-eye assignment:
        leftEyeCam.stereoTargetEye  = StereoTargetEyeMask.Left;
        rightEyeCam.stereoTargetEye = StereoTargetEyeMask.Right;

        // Disable any other stereo camera (especially XR Origin Main Camera)
        if (hmdCenterCameraToDisable != null)
            hmdCenterCameraToDisable.enabled = false;

        foreach (var cam in FindObjectsOfType<Camera>())
        {
            if (cam == leftEyeCam || cam == rightEyeCam) continue;
            // Prevent any other camera from rendering to the HMD
            if (cam.stereoTargetEye != StereoTargetEyeMask.None)
                cam.enabled = false;
        }

        // Sanity
        Debug.Log("[PerEyeCameraSetup] Left->Left eye, Right->Right eye. Other stereo cameras disabled.");
    }
}