using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public class IKManager : MonoBehaviour
{
    public Joint m_root;
    public Joint m_end;                 //end effector
    public GameObject m_target;
    public float m_threshold = 0.05f;
    public float m_rate = 5.0f;
    public float m_steps = 2;


    float slopeCalc(Joint _joint)       //calculate the min
    {
        float deltaTheta = 0.05f;
        float dist1 = getDistance(m_end.transform.position, m_target.transform.position);

        _joint.Rotate(deltaTheta);

        float dist2 = getDistance(m_end.transform.position, m_target.transform.position);

        _joint.Rotate(-deltaTheta);

        return (dist2 - dist1) / deltaTheta;
    }


    float getDistance(Vector3 _point1, Vector3 _point2)
    {
        return Vector3.Distance(_point1, _point2);
    }


    private void Update()           //once per frame
    {
        for(int i = 0; i < m_steps; ++i)
        {
            if (getDistance(m_end.transform.position, m_target.transform.position) > m_threshold)
            {
                Joint current = m_root;
                while(current != null)
                {
                    float slope = slopeCalc(current);
                    current.Rotate(-slope * m_rate);
                    current = current.GetChild();
                }
            
            }
        }
        
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}
