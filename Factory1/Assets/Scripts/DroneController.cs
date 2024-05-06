using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    private float     _controlSpeed = 0.5f; //= 0.5f;
    private float     _maxSpeed;
    private float     _maxSpeedCoeff = 1f; //2f; 
    private Rigidbody _body;
    private bool      _is_colliding = false;
    private Vector3   _prevPosition = new Vector3(0, 0, 0);
    public int ID;

    public GameController gc;

    
     public void Awake()
    {
        _body = GetComponent<Rigidbody>();

        
        _maxSpeed = _maxSpeedCoeff * _controlSpeed;
        //_maxSpeed = 1f; //0.00035f;
        //_body.maxLinearVelocity = _maxSpeed;

        // Disable collitions with other drones
    }

    public void Start()
    {
        GameObject[] _drones = GameObject.FindGameObjectsWithTag("Drone");
        foreach (GameObject drone in _drones)
        {
            Physics.IgnoreCollision(drone.GetComponent<Collider>(), GetComponent<Collider>());
        }
        //Debug.Log("[DroneController] [Start] Counter is: " + counter);
    }


    void FixedUpdate()
    {
        if (_body.velocity.magnitude>_maxSpeed)
        {
            _body.velocity = _maxSpeed * _body.velocity.normalized;
            //clipVelocity();
        }
    }


    public void clipVelocity()
    {
        _body.velocity = _maxSpeed * _body.velocity.normalized;
    }


    public void Reset(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        _body.velocity = new Vector3(0, 0, 0);
        _is_colliding = false;
        _prevPosition = spawnPosition;
    }
   


    public void SetAction(Vector3 action)
    {
        _prevPosition = transform.position;//transform.Find("LandingLocus").transform.position;
        _body.velocity = new Vector3(0,0,0); // so that the velocity vector is only defined by the latest choice
        _body.AddForce(_controlSpeed * action, ForceMode.VelocityChange);
    }



    public Vector3 GetPosition()
    {
        return transform.position;
    } 

    public Vector3 GetPrevPosition()
    {
        return _prevPosition;
    }

    public Vector3 GetVelocity()
    {
        return _body.velocity;
    }

    public float GetLinearVelocity()
    {
        return _body.velocity.magnitude;
    }

    void OnCollisionEnter(Collision collision)
    {
        _is_colliding = true;
    }

    public bool IsColliding()
    {
        return _is_colliding;
    }

}

// EOF
