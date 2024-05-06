using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointUR5 : MonoBehaviour
{

    public JointUR5 m_child;

    public JointUR5 GetChild()
    {
        return m_child;
    }

    public void Rotate(float _angle)
    {

        transform.Rotate(eulers: Vector3.forward * _angle);
       
        
    }

}
