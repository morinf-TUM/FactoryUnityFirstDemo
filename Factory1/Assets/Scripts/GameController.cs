using System;
using System.Collections.Generic;
using UnityEngine;
using NrpGenericProto;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

public class GameController : MonoBehaviour
{
    public  GameObject             dronePrefab;
    //public  GameObject             gameController;
    private int                    numDrones;
    private List<GameObject>      _drones;
    public List<DroneController> _droneControllers;
    private static List<Vector3>  _spawnPositions = new List<Vector3>();  // Elements are added in SpawnDrones
    private static Vector3[]      _spawnPosition; 
    public bool multipleSpawnPositions = true;
    public int numSpawnPositions;
    
    public List<Vector3>  _dronePositions = new List<Vector3>();  // Elements are added in SpawnDrones

    private GameObject _target;  // for single target
    private Vector3    _targetPosition;  
    public List<Vector3>  _targetPositions = new List<Vector3>(); // for multiple targets, same size as _droneControllers
    //public List<int>        targetIDs = new List<int>(); // for multiple targets, same size as _droneControllers

    public List<Vector3>  landingTargets = new List<Vector3>(); // for multiple targets, of length numLandingTargets
    public List<int>      landingTargetsID = new List<int>(); // for multiple targets, same size as _droneControllers
    private int      numLandingTargets;
    private float[]  initial_distance;
    private int _maxCollisions;
    public List<bool> landed = new List<bool>(); 
    public float[] rewards;
    private float  rewardK = (float) 0.5;

    //private List<Vector3> _waypoints = new List<Vector3>();

    public bool isPaused = false;
    public bool pauseNextFrame = false;

    public float[] x_bounds = {-7.5f, 7.5f};
    public float[] y_bounds = {0.0f, 6.75f};
    public float[] z_bounds = {-19.725f,  19.0f};




    void Awake()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("LandingSite");
        Debug.Log("[GameController] [Awake] Landing targets found: " + targets);
        numLandingTargets = 0;
        Transform targetCenter;
        foreach (GameObject target in targets)
        {
            targetCenter = target.transform.Find("Center");//Find child through transform hierarchy
            landingTargets.Add(targetCenter.transform.position);
            target.transform.Find("Cylinder").GetComponent<LandingSiteBehaviour1>().LandingSiteID = numLandingTargets;
            numLandingTargets++;
            Debug.Log("[GameController] [Awake] Landing target " + numLandingTargets + " added");
        }
        Debug.Log("[GameController] [Awake] Landing targets are " + landingTargets);
    }




    void Start()
    {
        //field1 = this.transform.parent.AddComponent<FieldFromCollisions>();
        Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);

        Debug.Log("[GameController] GameController has started.");
        
        // Define the target position common to all drones
        _target = GameObject.Find("Target");
        _targetPosition = _target.transform.position;
        
        foreach(GameObject drone in _drones)
        {
            int n = UnityEngine.Random.Range(0,numLandingTargets);
            landingTargetsID.Add(n);                  // int
            _targetPositions.Add(landingTargets[n]);  // Vector3
            landed.Add(false);
        }
    }




    void FixedUpdate()
    {
        int k = 0;
        foreach(var controller in _droneControllers)
        {
            _dronePositions[k] = controller.GetPosition();
            k+=1;
        }

    }



    void Update()
    {
        HandleInput();
    }



    public void SpawnDrones(int numDrones2)
    {
        numDrones = numDrones2;
        Debug.Log("[GameControllerWithField] [SpawnDrones] " + numDrones + " drones preparing to spawn.");

        // Initialize arrays that are depending on numDrones
        rewards = new float[numDrones];
        for (int i=0; i<numDrones; i++)
        {
            rewards[i] = 0f;
        }

        int k = 0;
        _spawnPosition = new Vector3[numDrones];
        _spawnPositions.Add(new Vector3( 0f,  3f,  0f));

        if (multipleSpawnPositions == true)
        {
            // Adding multiple spawn positions to the one above:
            //_spawnPositions.Add(new Vector3( 0f,  3f,  5f));
            _spawnPositions.Add(new Vector3(  2f,   3f,  -6f));
            _spawnPositions.Add(new Vector3( -2f,   3f,  -6f));
            _spawnPositions.Add(new Vector3(  2f,   3f,   5f));
            _spawnPositions.Add(new Vector3( -2f,   3f,   5f));
        }
        numSpawnPositions = _spawnPositions.Count;

        // Spawn drones using the defined drone prefab
        Debug.Log("[GameController] [SpawnDrones] " + numDrones + " drones are spawning.");
        _drones = new List<GameObject>();
        for(int i = 0; i < numDrones; i++)
        {
            _spawnPosition[i] = _spawnPositions[UnityEngine.Random.Range(0,_spawnPositions.Count)];
            _drones.Add(Instantiate(dronePrefab, _spawnPosition[i], Quaternion.identity));
            _dronePositions.Add(_spawnPosition[i]);
            _drones[i].GetComponent<DroneController>().ID = i;
            _drones[i].GetComponent<DroneController>().gc = this;
        }


        // Retrieve controller scripts of all drones and define initial distance to target
        _droneControllers = new List<DroneController>();
        initial_distance = new float[numDrones];

        k = 0;
        foreach(var drone in _drones)
        {
            _droneControllers.Add(drone.GetComponent<DroneController>());
            initial_distance[k] = Vector3.Distance(_spawnPosition[k], _targetPosition);

            k=k+1;
        }
        
        Debug.Log("[GameControl] At the end of spawndrones, _droneControllers is " + _droneControllers); // + " and indexBuffer.count is " + n.ToString());
        Debug.Log("[GameControl] _droneControllers is comprised of " + _droneControllers.Count + " elements.");
    }


    public int GetDroneCount()
    {
        return numDrones;
    }



    public void SetActions(RepeatedField<float> values)
    {    
        for(int i = 0; i < _droneControllers.Count; i++)
        {
            // _droneControllers[i].SetAction(new Vector3(values[i*3 + 0], (float) 0.0, values[i*3 + 1]));
            _droneControllers[i].SetAction(new Vector3(values[i*3 + 0], values[i*3 + 1], values[i*3 + 2]));
        }
        Debug.Log("[GameController] [SetActions] Actions set at fixedTime " + Time.fixedTime + " s");
    }



    public void ResetAllDrones()
    {
        int k = 0;
        int n;
        foreach(var controller in _droneControllers)
        {
            ResetDrone(controller.ID);
            /*controller.Reset(_spawnPositions[UnityEngine.Random.Range(0,_spawnPositions.Count)]);
            n = UnityEngine.Random.Range(0,numLandingTargets);
            landingTargetsID[controller.ID] = n;                  // int
            _targetPositions[controller.ID] = landingTargets[n];  // Vector3
                      landed[controller.ID] = false;*/
            if (controller.ID == 0)
            {
                Debug.Log("[GameController] drone k=0 =target is no " + landingTargetsID[controller.ID] );
                Debug.Log("[GameController] drone k=0 prevPosi = " + _targetPositions[controller.ID] );
            }
        }
    }



    public void ResetDrone(int i)
    {
        _droneControllers[i].Reset(_spawnPositions[UnityEngine.Random.Range(0,_spawnPositions.Count)]);
        int n = UnityEngine.Random.Range(0,numLandingTargets);
        landingTargetsID[i] = n;                  // int
        _targetPositions[i] = landingTargets[n];  // Vector3
                  landed[i] = false;
    }




    public NrpGenericProto.ArrayFloat GetObservations()
    {
        NrpGenericProto.ArrayFloat positionProto = new NrpGenericProto.ArrayFloat();
        foreach(var controller in _droneControllers)
        {
            Vector3 positionVector = controller.GetPosition();

            positionProto.Array.Add(positionVector[0]);
            positionProto.Array.Add(positionVector[1]);
            positionProto.Array.Add(positionVector[2]);

            Vector3 velocityVector = controller.GetVelocity();

            // Passing identity of target
            switch(landingTargetsID[controller.ID]) 
            {
                case 0:
                positionProto.Array.Add(1f);
                positionProto.Array.Add(0f);
                positionProto.Array.Add(0f);
                break;
                case 1:
                positionProto.Array.Add(0f);
                positionProto.Array.Add(1f);
                positionProto.Array.Add(0f);
                break;
                default:
                positionProto.Array.Add(0f);
                positionProto.Array.Add(0f);
                positionProto.Array.Add(1f);
                break;
            }
        }

        return positionProto;
    }



    public (NrpGenericProto.ArrayFloat, NrpGenericProto.ArrayBool, NrpGenericProto.ArrayBool) GetRewardsAndDones()
    {
        NrpGenericProto.ArrayFloat rewardProto   = new NrpGenericProto.ArrayFloat();
        NrpGenericProto.ArrayBool isDoneProto    = new NrpGenericProto.ArrayBool();
        NrpGenericProto.ArrayBool hasLandedProto = new NrpGenericProto.ArrayBool();
        
        Vector3 fieldGrad   = Vector3.zero;

        int k = 0;
        foreach(var controller in _droneControllers)
        {
            Vector3 position     = controller.GetPosition();
            Vector3 prevPosition = controller.GetPrevPosition(); 
            if (k==0)
            {
                Debug.Log("[GameController] drone k=0 position = " + position);
                Debug.Log("[GameController] drone k=0 prevPosi = " + prevPosition);
                Debug.Log("[GameController] drone k=0 localLoc = " + controller.transform.Find("LandingLocus").transform.localPosition);
            }
            
            float distance     = Vector3.Distance(position,     _targetPositions[k]);
            float prevDistance = Vector3.Distance(prevPosition, _targetPositions[k]);
            bool isDone = false;
            float reward = 0f;

            //Debug.Log("Checking: k = " + k + " and controller.ID is " + controller.ID);

            try{
                rewards[k] = rewardK * (prevDistance - distance);
                // If the target is reached
                if(distance<0.0005f)
                {
                    rewards[k] += 5f;
                    isDone = true;
                }

                if(landed[k])
                {
                    rewards[k] += 1.5f + 
                                    5f * (float) Math.Exp(-( Math.Pow(_targetPositions[k].x-position.x, 2.00) +
                                    Math.Pow(_targetPositions[k].z-position.z, 2.00) ) / (2*Math.Pow(0.4,2.00)));
                    isDone = true;
                    Debug.Log("[GameController] Drone (k,ID) = (" + k + "," + controller.ID + ") has landed.");
                }

                // If the drone collided with anything
                if(controller.IsColliding())
                {
                    if (isDone==false)
                    {
                        rewards[k] = -1f ;
                        Debug.Log("[GameController] Drone (k,ID) = (" + k + "," + controller.ID + ") has crashed.");
                    }
                    isDone = true; 
                }

                
            } catch {
                if (k==0) Debug.Log("Check that you are not using a private variable in a callback.");
                else Debug.Log("*****   WARNING: ERROR IN REWARD COMPUTATION!   *****");
            }

            if (k==0)
            {
                Debug.Log("[GameController] drone k=0 reward = " + reward);
                Debug.Log("[GameController] drone k=0 isDone = " + isDone);
                Debug.Log("[GameController] drone k=0 landed = " + landed);
            }

            rewardProto.Array.Add(rewards[k]);
            //rewardProto.Array.Add(reward);
            isDoneProto.Array.Add(isDone);
            hasLandedProto.Array.Add(landed);

            k += 1;
        }

        return (rewardProto, isDoneProto, hasLandedProto);
    }




    void HandleInput()
    {
        if (pauseNextFrame == true) 
        {
            isPaused = true;
            Time.timeScale = 0f; 
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
            if (isPaused == false) Time.timeScale = 1f;
            else Time.timeScale = 0f; 
            if (pauseNextFrame == true) pauseNextFrame = false;
        }

        
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isPaused = false;
            Time.timeScale = 1f;
            pauseNextFrame = true;
        }
    }


    void myprint()
    {
        
    }
}


// EOF
