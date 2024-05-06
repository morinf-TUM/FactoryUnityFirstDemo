/*
Created by Youssef Elashry to allow two-way communication between Python3 and Unity to send and receive strings

Feel free to use this in your individual or commercial projects BUT make sure to reference me as: Two-way communication between Python 3 and Unity (C#) - Y. T. Elashry
It would be appreciated if you send me how you have used this in your projects (e.g. Machine Learning) at youssef.elashry@gmail.com

Use at your own risk
Use under the Apache License 2.0

Modified by: 
Youssef Elashry 12/2020 (replaced obsolete functions and improved further - works with Python as well)
Based on older work by Sandra Fang 2016 - Unity3D to MATLAB UDP communication - [url]http://msdn.microsoft.com/de-de/library/bb979228.aspx#ID0E3BAC[/url]
*/

using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;


public class UdpSocket : MonoBehaviour
{
    [HideInInspector] public bool isTxStarted = false;

    [SerializeField] string IP = "127.0.0.1"; // local host
    [SerializeField] int rxPort = 8000; // port to receive data from Python on
    [SerializeField] int txPort = 8001; // port to send data to Python on

    int i = 0; // DELETE THIS: Added to show sending data from Unity to Python via UDP

    // Create necessary UdpClient objects
    UdpClient client;
    IPEndPoint remoteEndPoint;
    Thread receiveThread; // Receiving Thread


    //gameobjects to control (joints)
    public GameObject joint0;
    public GameObject joint1;
    public GameObject joint2;
    public GameObject joint3;
    public GameObject joint4;
    public GameObject joint5;
    public GameObject joint6;

    //script of every joint controller regarding the joints game object
    ArticulationJointController controllerJoint0;
    ArticulationJointController controllerJoint1;
    ArticulationJointController controllerJoint2;
    ArticulationJointController controllerJoint3;
    ArticulationJointController controllerJoint4;
    ArticulationJointController controllerJoint5;
    ArticulationJointController controllerJoint6;

    //wanted joint rotation given by the IK
    float angle0;
    float angle1;
    float angle2;
    float angle3;
    float angle4;
    float angle5;
    float angle6;

    //current joint values regarding the joint limits: -> actual joint rotation in the world
    public double currJointVal0;
    public double currJointVal1;
    public double currJointVal2;
    public double currJointVal3;
    public double currJointVal4;
    public double currJointVal5;
    public double currJointVal6;

    //did the rotation change
    bool changedRotation = false;

    //object spawner
    public GameObject spawner;
    RandomSpawner spawnerSc;
    int objectNumber;
    bool spawnObjetsBo = false;


    void Update(){
        if(changedRotation){
            Rotation(angle0, angle1, angle2, angle3, angle4,angle5,angle6);
        }
        if(spawnObjetsBo){
            SpawnObjects(objectNumber);
        }

        currJointVal1 = eulerValuesY(joint1, currJointVal1);
        currJointVal3 = eulerValuesY(joint3, currJointVal3);
        currJointVal5 = eulerValuesY(joint5, currJointVal5);

        currJointVal0 = eulerValuesZ(joint0, currJointVal0);
        currJointVal2 = eulerValuesZ(joint2, currJointVal2);
        currJointVal4 = eulerValuesZ(joint4, currJointVal4);
        currJointVal6 = eulerValuesZ(joint6, currJointVal6);

        //Debug.Log("current: " + currJointVal0);

    }

    public double eulerValuesZ(GameObject jointCurr, double curr ){
        if(jointCurr.transform.localRotation.eulerAngles.z <= 180f)
        {
            curr = jointCurr.transform.localRotation.eulerAngles.z;
            return curr;
        }
        else
        {
            curr = jointCurr.transform.localRotation.eulerAngles.z - 360f;
            return curr;
        }
        Debug.Log("joint values: " + curr + "current: " + currJointVal0);
    }

    public double eulerValuesY(GameObject jointCurr, double curr ){
        if(jointCurr.transform.localRotation.eulerAngles.z <= 180f)
        {
            curr = jointCurr.transform.localRotation.eulerAngles.y;
            return curr;
        }
        else
        {
            curr = jointCurr.transform.localRotation.eulerAngles.y - 360f;
            return curr;
        }
        //Debug.Log("joint values: " + curr);
    }

    IEnumerator SendDataCoroutine() // DELETE THIS: Added to show sending data from Unity to Python via UDP
    {
        while (true)
        {

            CurrJointValues answer = new CurrJointValues();
            answer.currJointVal0_ = currJointVal0;
            answer.currJointVal1_ = currJointVal1;
            answer.currJointVal2_ = currJointVal2;
            answer.currJointVal3_ = currJointVal3;
            answer.currJointVal4_ = currJointVal4;
            answer.currJointVal5_ = currJointVal5;
            answer.currJointVal6_ = currJointVal6;

            string json = JsonUtility.ToJson(answer);
            Debug.Log("Send Data: " + json);
            SendData(json);


            //SendData("Sent from Unity: " + i.ToString());
            //i++;
            yield return new WaitForSeconds(1f);
        }
    }

    public void SendData(string message) // Use to send data to Python as string
    {
        try
        {   
            Debug.Log("MESSAGE: " + message);
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    public void SendData(byte[] data) // Use to send data to Python as bytes
    {
        try
        {
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    void Awake()
    {

        //get scripts of each joint or component
        controllerJoint0 = (ArticulationJointController) joint0.GetComponent(typeof(ArticulationJointController));
        controllerJoint1 = (ArticulationJointController) joint1.GetComponent(typeof(ArticulationJointController));
        controllerJoint2 = (ArticulationJointController) joint2.GetComponent(typeof(ArticulationJointController));
        controllerJoint3 = (ArticulationJointController) joint3.GetComponent(typeof(ArticulationJointController));
        controllerJoint4 = (ArticulationJointController) joint4.GetComponent(typeof(ArticulationJointController));
        controllerJoint5 = (ArticulationJointController) joint5.GetComponent(typeof(ArticulationJointController));
        controllerJoint6 = (ArticulationJointController) joint6.GetComponent(typeof(ArticulationJointController));

        //get script of the spawner
        spawnerSc = (RandomSpawner) spawner.GetComponent(typeof(RandomSpawner));

        // Create remote endpoint (to Matlab) 
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), txPort);

        // Create local client
        client = new UdpClient(rxPort);

        // local endpoint define (where messages are received)
        // Create a new thread for reception of incoming messages
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        // Initialize (seen in comments window)
        print("UDP Comms Initialised");

        StartCoroutine(SendDataCoroutine()); // DELETE THIS: Added to show sending data from Unity to Python via UDP
    }

    // Receive data, update packets received
    private void ReceiveData()
    {

        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string jsonString = Encoding.UTF8.GetString(data);
                print(">> " + jsonString);

                switch (jsonString[18]){

                    case 'k':
                        var jsonObjectJoints = JsonUtility.FromJson<JointValues>(jsonString);
                        string outputJoints = JsonUtility.ToJson(jsonObjectJoints);
                        if(controllerJoint0 != null){
                            angle0 = float.Parse(jsonObjectJoints.joint0) * Mathf.Rad2Deg;
                            angle1 = float.Parse(jsonObjectJoints.joint1) * Mathf.Rad2Deg;
                            angle2 = float.Parse(jsonObjectJoints.joint2) * Mathf.Rad2Deg;
                            angle3 = float.Parse(jsonObjectJoints.joint3) * Mathf.Rad2Deg;
                            angle4 = float.Parse(jsonObjectJoints.joint4) * Mathf.Rad2Deg;
                            angle5 = float.Parse(jsonObjectJoints.joint5) * Mathf.Rad2Deg;
                            angle6 = float.Parse(jsonObjectJoints.joint6) * Mathf.Rad2Deg;
                            Debug.Log("RotateTo: Joint1: " + angle0 + " Joint2: " + angle1 + " Joint3: " + angle2 + " Joint4: " + angle3 + " Joint5: " + angle4 + " Joint6: " + angle5 + " Joint7: " + angle6);
                            changedRotation = true;
                        }else
                        {
                            Debug.Log("null");
                        }
                        print(outputJoints);
                        break;

                    case 'n':
                        var jsonObjectNum = JsonUtility.FromJson<NumObjects>(jsonString);
                        string outputNum = JsonUtility.ToJson(jsonObjectNum);
                        objectNumber = jsonObjectNum.num_objects;
                        print("Object number: " + outputNum);
                        print(objectNumber);
                        spawnObjetsBo = true;
                        break;

                    case 'c':
                        var jsonObjectCam = JsonUtility.FromJson<CameraAngles>(jsonString);
                        string outputCam = JsonUtility.ToJson(jsonObjectCam);
                        print(outputCam);
                        break;

                    case 'a':
                        Debug.Log("IN ASK");
                        CurrJointValues answer = new CurrJointValues();
                        answer.currJointVal0_ = currJointVal0;
                        answer.currJointVal1_ = currJointVal1;
                        answer.currJointVal2_ = currJointVal2;
                        answer.currJointVal3_ = currJointVal3;
                        answer.currJointVal4_ = currJointVal4;
                        answer.currJointVal5_ = currJointVal5;
                        answer.currJointVal6_ = currJointVal6;


                        string json = JsonUtility.ToJson(answer);
                        Debug.Log("Send Data: " + json);
                        SendData(json);
                       
                        break;

                    default:
                        break;
                }
                

            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

        //call RotateTo() skript of every Joint in the Robot (attached to the different Joints)
        void Rotation(float angle0, float angle1, float angle2, float angle3, float angle4, float angle5, float angle6){
            //Debug.Log("In the function");
        controllerJoint0.RotateTo(angle0);
        controllerJoint1.RotateTo(angle1);
        controllerJoint2.RotateTo(angle2);
        controllerJoint3.RotateTo(angle3);
        controllerJoint4.RotateTo(angle4);
        controllerJoint5.RotateTo(angle5);
        controllerJoint6.RotateTo(angle6);
        changedRotation = false;
    }


    void SpawnObjects(int number){
        Debug.Log("In the function");
        spawnerSc.spawnObj(number);
        spawnObjetsBo = false;
    }

    private void ProcessInput(string input)
    {
        // PROCESS INPUT RECEIVED STRING HERE

        if (!isTxStarted) // First data arrived so tx started
        {
            isTxStarted = true;
        }
    }

    //Prevent crashes - close clients and threads properly!
    void OnDisable()
    {
        if (receiveThread != null)
            receiveThread.Abort();

        client.Close();
    }

}

[System.Serializable]
public class JointValues{
    public string joint0;
    public string joint1;
    public string joint2;
    public string joint3;
    public string joint4;
    public string joint5;
    public string joint6;

}

[System.Serializable]
public class NumObjects{
    public int num_objects;
}

[System.Serializable]
public class CameraAngles{
    public int xAngle;
    public int yAngle;
    public int zAngle;
}

[System.Serializable]
public class CurrJointValues{
    public double currJointVal0_;
    public double currJointVal1_;
    public double currJointVal2_;
    public double currJointVal3_;
    public double currJointVal4_;
    public double currJointVal5_;
    public double currJointVal6_;
    public double currJointVal7_;
}

