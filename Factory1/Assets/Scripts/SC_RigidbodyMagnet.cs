using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_RigidbodyMagnet : MonoBehaviour
{
    public float magnetForce = 100;

    List<Rigidbody> caughtRigidbodies = new List<Rigidbody>();
    Transform magnetPoint;

    private void Start()
    {
    
       magnetPoint = GetComponent<Transform>();
    
    }

    private void FixedUpdate()
    {
        foreach (Rigidbody bodie in caughtRigidbodies)
        {
            bodie.AddForce((magnetPoint.position - bodie.position) * magnetForce * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball")) caughtRigidbodies.Add(other.GetComponent<Rigidbody>());
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball")) caughtRigidbodies.Remove(other.GetComponent<Rigidbody>());
    }
}