using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dummy_script : MonoBehaviour
{
    // Start is called before the first frame update

    ArticulationBody articulated_body_rotor0, articulated_body_rotor1, articulated_body_rotor2, articulated_body_rotor3, base_body;
    Vector3 rotor0_ang_vel, rotor1_ang_vel, rotor2_ang_vel,rotor3_ang_vel;
    Transform rotor3, rotor2, rotor1, rotor0, baselink;
    Vector3 position_vector;
    float posx,posy,posz;
    private float movementSpeed = 5f;
    void Start()
    {
        //articulated_body = gameObject.GetComponent<ArticulationBody>();
        rotor0_ang_vel.y = 3000;
        rotor1_ang_vel.y = 3000;
        rotor2_ang_vel.y = 3000;
        rotor3_ang_vel.y = 3000;
        rotor3 = gameObject.transform.GetChild(0);
        rotor2 = gameObject.transform.GetChild(1);
        rotor1 = gameObject.transform.GetChild(2);
        rotor0 = gameObject.transform.GetChild(3);
        baselink = gameObject.transform.GetChild(4);
        articulated_body_rotor0 = rotor0.GetComponent<ArticulationBody>();
        articulated_body_rotor1 = rotor1.GetComponent<ArticulationBody>();
        articulated_body_rotor2 = rotor2.GetComponent<ArticulationBody>();
        articulated_body_rotor3 = rotor3.GetComponent<ArticulationBody>();
        base_body = baselink.GetComponent<ArticulationBody>();
    }

    // Update is called once per frame
    void Update()
    {
        articulated_body_rotor0.angularVelocity = rotor0_ang_vel * Time.deltaTime;
        articulated_body_rotor1.angularVelocity = rotor1_ang_vel * Time.deltaTime;
        articulated_body_rotor2.angularVelocity = rotor2_ang_vel * Time.deltaTime;
        articulated_body_rotor3.angularVelocity = rotor3_ang_vel * Time.deltaTime;
        //articulated_body.AddRelativeForce(Vector3.forward * 1.0f);
        //get the Input from Horizontal axis

        // gameObject.transform.position = new Vector3(0, posy, 0);

        // base_body.angularVelocity = rotor0_ang_vel * Time.deltaTime;
        
        // rotor0.transform.position = new Vector3(0, posy, 0);
        // rotor1.transform.position = new Vector3(0, posy, 0);
        // rotor2.transform.position = new Vector3(0, posy, 0);
        // rotor3.transform.position = new Vector3(0, posy, 0);
        posy += 0.001f;
        Debug.Log(articulated_body_rotor0.name);
        Debug.Log(articulated_body_rotor0.dofCount);
        // Debug.Log(articulated_body_rotor2.name);
        // Debug.Log(articulated_body_rotor3.name);

    }
    
}
