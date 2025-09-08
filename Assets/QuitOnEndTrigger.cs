using UnityEngine;

public class QuitOnEndTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object we collided with is the EndObject
        if (other.gameObject.name == "EndObject")
        {
            Debug.Log("EndObject touched. Quitting game...");

            Application.Quit();

#if UNITY_EDITOR
            // This makes it stop Play Mode inside the Unity Editor
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}