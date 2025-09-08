using UnityEngine;

public class CopyYRotation : MonoBehaviour
{
    [Tooltip("The object whose Y rotation we want to follow")]
    public Transform target;

    void Update()
    {

        // Get this object's current rotation
        Vector3 currentEuler = transform.eulerAngles;

        // Get target's rotation
        Vector3 targetEuler = target.eulerAngles;

        // Only copy the Y rotation
        transform.rotation = Quaternion.Euler(currentEuler.x, targetEuler.y, currentEuler.z);
    }
}