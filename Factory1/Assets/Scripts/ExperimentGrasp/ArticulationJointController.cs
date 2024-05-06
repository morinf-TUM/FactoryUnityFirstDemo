using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RotationDirection { None = 0, Positive = 1, Negative = -1 };

public class ArticulationJointController : MonoBehaviour
{
    public RotationDirection rotationState = RotationDirection.None;
    public float speed = 0;
    float target = 60;

    private ArticulationBody articulation;


    // LIFE CYCLE

    void Start()
    {
        articulation = GetComponent<ArticulationBody>();
    }

    void Update() {
        ArticulationDrive drive = articulation.xDrive;
        //drive.target = target;
        //drive.targetVelocity = 1;
        articulation.xDrive = drive;
    }

    void FixedUpdate() 
    {
        if (rotationState != RotationDirection.None) {
            float rotationChange = (float)rotationState * speed * Time.fixedDeltaTime;
            float rotationGoal = CurrentPrimaryAxisRotation() + rotationChange;
            RotateTo(rotationGoal);
        }


    }


    // MOVEMENT HELPERS

    float CurrentPrimaryAxisRotation()
    {
        float currentRotationRads = articulation.jointPosition[0];
        float currentRotation = Mathf.Rad2Deg * currentRotationRads;
        return currentRotation;
    }

    public void RotateTo(float primaryAxisRotation)
    {
        /*
        ArticulationDrive drive = articulation.xDrive;
        drive.target = primaryAxisRotation;
        drive.targetVelocity = speed;
        articulation.xDrive = drive;
        */
        target = primaryAxisRotation;
    }



}
