using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class StereoFromThisCamera : MonoBehaviour
{
    public Camera.StereoscopicEye eye = Camera.StereoscopicEye.Left;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // IMPORTANT: tell XR to stop overriding this camera with HMD pose
#if UNITY_2019_4_OR_NEWER
        XRDevice.DisableAutoXRCameraTracking(cam, true);
#endif
        // Also ensure this camera is assigned to the correct eye only
        cam.stereoTargetEye = (eye == Camera.StereoscopicEye.Left)
            ? StereoTargetEyeMask.Left : StereoTargetEyeMask.Right;
    }

    void OnPreCull()
    {
        // Force XR to use THIS camera's view+projection for the target eye
        cam.SetStereoViewMatrix(eye, cam.worldToCameraMatrix);
        cam.SetStereoProjectionMatrix(eye, cam.projectionMatrix);
    }

    void OnDisable()
    {
        // Clean up
        cam.ResetStereoViewMatrices();
        cam.ResetStereoProjectionMatrices();
#if UNITY_2019_4_OR_NEWER
        XRDevice.DisableAutoXRCameraTracking(cam, false);
#endif
    }
}