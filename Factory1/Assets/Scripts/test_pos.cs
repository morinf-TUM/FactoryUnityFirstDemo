using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test_pos : MonoBehaviour
{
    // Start is called before the first frame update
    // Drone Position variables
    float drone_posx,drone_posy,drone_posz;
    // Drone Rotation Variables
    float drone_rotx,drone_roty,drone_rotz;
    // Rotor angles
    float rotor0_angle, rotor1_angle, rotor2_angle, rotor3_angle;
    // Rotor angular velocities
    float rotor0_ang_vel, rotor1_ang_vel, rotor2_ang_vel, rotor3_ang_vel;
    // Child objects
    Transform rotor3, rotor2, rotor1, rotor0, baselink;
    public Rigidbody rb;
    void Start()
    {
        rotor3 = gameObject.transform.GetChild(0);
        rotor2 = gameObject.transform.GetChild(1);
        rotor1 = gameObject.transform.GetChild(2);
        rotor0 = gameObject.transform.GetChild(3);
        baselink = gameObject.transform.GetChild(4);
        rb = gameObject.GetComponent<Rigidbody>();
        rb.WakeUp();
        drone_posy = 0.1f;
        // Rotor initial angles
        rotor0_angle = 0;
        rotor1_angle = 120;
        rotor2_angle = 0;
        rotor3_angle = 0;
        rotor0_ang_vel = 100;
        rotor1_ang_vel = 100;
        rotor2_ang_vel = 100;
        rotor3_ang_vel = 100;

    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(rb.name);
        Debug.Log(rb);
        // rb.AddForce(transform.up * 1);
        rotor0_angle = rotor0_angle + rotor0_ang_vel * Time.deltaTime;
        rotor1_angle = rotor1_angle + rotor1_ang_vel * Time.deltaTime;
        rotor2_angle = rotor2_angle + rotor2_ang_vel * Time.deltaTime;
        rotor3_angle = rotor3_angle + rotor3_ang_vel * Time.deltaTime;
        drone_posy +=0.001f;
        gameObject.transform.position = new Vector3(drone_posx, drone_posy, drone_posz);
        gameObject.transform.rotation = Quaternion.Euler(drone_rotx, drone_roty,drone_rotz);
        rotor0.transform.rotation = Quaternion.Euler(0, rotor0_angle,0);
        rotor1.transform.rotation = Quaternion.Euler(0, rotor1_angle,0);
        rotor2.transform.rotation = Quaternion.Euler(0, rotor2_angle,0);
        rotor3.transform.rotation = Quaternion.Euler(0, rotor3_angle,0);

        
    }

    void OnCollisionStay(Collision collision) {
        Debug.Log("Impulse " + collision.impulse);
        Debug.Log("Contact Count " + collision.contactCount);

       // int counter = 0 ;
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal * 100, Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f), 10f);
            Debug.Log("----------------");
            // Debug.Log(contact.point - my_rb.worldCenterOfMass);
            Debug.Log("Contact normal " + contact.normal);
            Debug.Log("Contact point " + contact.point);
            // counter++;
            // Debug.Log(contact.impulse);
        }

    }



}

