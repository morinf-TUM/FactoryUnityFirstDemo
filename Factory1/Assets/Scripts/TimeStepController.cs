/*
 * NRP Core - Backend infrastructure to synchronize simulations
 *
 * Copyright 2022-2023 Josip Josifovski, Krzysztof Lebioda
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * This project has received funding from the European Union’s Horizon 2020
 * Framework Programme for Research and Innovation under the Specific Grant
 * Agreement No. 945539 (Human Brain Project SGA3).
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeStepController : MonoBehaviour
{
    private float _fixedTimestep;
    private long  _numIterationsInt;
    private bool _runUpdates;
    private float _simulationTime;
    private int _fixedUpdaterCounter;


    private void Start()
    {
        
    }


    private void Awake()
    {
        Physics.simulationMode = SimulationMode.FixedUpdate; //Script;

        _runUpdates = false;
        _simulationTime = 0.0f;
        _fixedUpdaterCounter = 0;
        _fixedTimestep = Time.fixedDeltaTime;
    }


    private void FixedUpdate()
    {
        _fixedUpdaterCounter += 1; 
        //Debug.Log("[TimeStepController] [FixedUpdate] _fixedUpdaterCounter = " + _fixedUpdaterCounter );
        //Debug.Log("[TimeStepController] [FixedUpdate] Physics.Simulate() done at wall-clock time " + Time.time + 
        //                    " s,    fixedtime " + Time.fixedTime + 
        //                    " s,    simulation time " + _simulationTime + 
        //                    " s, with deltaFixed = " + (_fixedTimestep * 1e3f) + " ms");
        _simulationTime += _fixedTimestep;
    }

    void Update()
    {
        Debug.Log("[TimeStepController] [Update] _fixedUpdaterCounter = " + _fixedUpdaterCounter); 

        if (_numIterationsInt > 0) _numIterationsInt--;

        if (_numIterationsInt == 0) _runUpdates = false;

        _fixedUpdaterCounter = 0;
    }


    public long RunGame(long deltaTimeNs)
    {
        // The requested timestep (deltaTimeNs) should be a multiple of the Unity's fixed timestep
        // Unity tracks the time using floats, which results in floating point arithmetic and rounding errors.
        // That's why we 'cheat' when calculating the simulated time (actualDeltaTimeNs).
        // As long as the requested timestep is a multiple of the fixed timestep, then we simply return
        // the requested timestep, instead of calculating it. The calculated timestep would probably differ slightly
        // from the requested timestep (e.g. 199999980 instead of 200000000). This should keep NRP Core engine
        // synchronization mechanism happy.

        Debug.Log("[TimeStepController] Rungame starting.");

        _runUpdates = true;

        _fixedTimestep    = Time.fixedDeltaTime;
        float numIterationsFloat = (float)deltaTimeNs / (_fixedTimestep * 1e9f);
        _numIterationsInt   = Mathf.RoundToInt(numIterationsFloat);
        float numIterationsDiff  = numIterationsFloat - (float)_numIterationsInt;

        long actualDeltaTimeNs = deltaTimeNs;
        if(Mathf.Abs(numIterationsDiff) > 1e5)
        {
            Debug.LogWarning("[TimeStepController] Requested simulation timestep (" +
                             deltaTimeNs + "ns) is not a multiple of the Unity's fixed timestep (" +
                             (_fixedTimestep * 1e9f) + " ns)");

            actualDeltaTimeNs = _numIterationsInt * (int)(_fixedTimestep * 1e9f);
        }

        Debug.Log("[TimeStepController] Rungame ended.");

        return actualDeltaTimeNs;
    }
}
