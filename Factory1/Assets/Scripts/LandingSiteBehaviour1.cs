using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandingSiteBehaviour1 : MonoBehaviour
{
    public int LandingSiteID;

    private GameController gc;


    public void Start()
    {
        gc = GameObject.FindObjectOfType<GameController>();
    }
    

    public void OnTriggerEnter (Collider other)
    {
        DroneController targ = other.transform.GetComponent<DroneController>();
        int x = gc.landingTargetsID[targ.ID];
        Debug.Log("[LandingSiteBehaviour1] Trigger on site " + LandingSiteID);
        if (x == LandingSiteID) 
        {
            gc.landed[targ.ID] = true;
            Debug.Log("[LandingSiteBehaviour1] Drone " + targ.ID + " correctly landed on site " + x );
        }
        else
        {
            Debug.Log("[LandingSiteBehaviour1] Drone " + targ.ID + " landed on wrong site (" + 
                       LandingSiteID + ") instead of " + x + " with coordinates " + gc._targetPositions[targ.ID] );
        }
        
                
    }
}