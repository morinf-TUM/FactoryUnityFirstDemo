using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera[] cameras; // Array to hold the cameras
    private int currentCameraIndex = 0; // Index of the currently active camera

    void Start()
    {
        // Enable the first camera and disable others
        SwitchCamera(currentCameraIndex);
    }

    void Update()
    {
        // Check for input to switch cameras
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchCamera(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchCamera(1);
        }
        // Add more conditions for additional cameras (e.g., KeyCode.Alpha3 for third camera)
    }

    void SwitchCamera(int newIndex)
    {
        // Disable the current camera
        cameras[currentCameraIndex].enabled = false;

        // Enable the new camera
        cameras[newIndex].enabled = true;

        // Update the index of the current camera
        currentCameraIndex = newIndex;
    }
}