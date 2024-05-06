using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GUIHelper : MonoBehaviour
{
    //private string textLabel = "numCollisions";
    public string toDisplay ="Not started";
    public string toDisplayRew ="None";
    public string toDisplayLinvel ="None";
    private GameController _gc;
    private TextMeshProUGUI numcol;
    private TextMeshProUGUI rew;
    private TextMeshProUGUI linv;
    int num;

    
    /*
    void OnGUI()
    {
        GUI.Box(new Rect(10,10,100,30), toDisplay);
    }
    */

    void Awake()
    {
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _gc = FindObjectOfType<GameController>();
        numcol = GameObject.Find("numCollisions").GetComponent<TextMeshProUGUI>();
        rew = GameObject.Find("Rewards").GetComponent<TextMeshProUGUI>();
        linv = GameObject.Find("LinVel").GetComponent<TextMeshProUGUI>();
        num = _gc.GetDroneCount();
    }

    // Update is called once per frame
    void Update()
    {
        try 
        {
            int effnum = 4; // number of agents
            float[] disprew = _gc.rewards;
            string[] strrew = new string[effnum];
            for (int j=0; j<effnum; j++)    
            {
                strrew[j] = disprew[j].ToString("0.000");
            }
            toDisplayRew = string.Join("; ", strrew);
            rew.text = toDisplayRew;
        } catch {
            rew.text = "No reward yet.";
        }


        try 
        {
            int effnum = 24; // num;  // num for all agents
            float displinvel = 0f;
            string[] strlinvel = new string[effnum];
            for (int j=0; j<effnum; j++)
            {
                displinvel = _gc._droneControllers[j].GetLinearVelocity();
                strlinvel[j] = displinvel.ToString("0.000");
            }
            toDisplayLinvel = string.Join("; ", strlinvel);
            linv.text = toDisplayLinvel;
        } catch {
            linv.text = "No velocities yet.";
        }


    }
}
