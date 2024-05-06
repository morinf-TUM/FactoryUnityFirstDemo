using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrientation : MonoBehaviour
{

    public float xAngle, yAngle, zAngle;
    public GameObject Camera;

    // Start is called before the first frame update
    void Start()
    {
        Camera.transform.Rotate(xAngle, yAngle, zAngle, Space.Self);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
