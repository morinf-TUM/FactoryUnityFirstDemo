/*
 * NRP Core - Backend infrastructure to synchronize simulations
 *
 * Copyright 2022-2023 Josip Josifovski, Krzysztof Lebioda
 *
 * Demonstrator of drones learning path in factory settings
 * Copyright 2024 Fabrice O. Morin
 *
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

using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Grpc.Core;
using EngineGrpc;
using Google.Protobuf;
using System.Text;
using System.Threading;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using NrpGenericProto;
using Wrappers;
using UnityEngine.SceneManagement;


/// <summary>
/// Enum for the possible remote proedures calls, new ones to be added if the game should support more
/// </summary>
enum ServiceProcedure { NONE, INITIALIZE, RESET, SHUTDOWN, GET_DATAPACKS, SET_DATAPACKS, RUN_LOOP_STEP }

class CommunicationServiceController : MonoBehaviour
{
    /// <summary>
    /// The currently invoked remote procedure
    /// </summary>
    private ServiceProcedure _invokedProcedure;

    private readonly object _locker = new object();

    /// <summary>
    /// gRPC server responsible for listening to requests and providing responses
    /// </summary>
    private Server _server;

    /// <summary>
    /// The ip address on which the server is registered, usually localhost when both client and server are on the same machine, can be a configurable parameter in future
    /// </summary>
    private string _ipAddress = "localhost";

    private string _engineName = "unity";

    /// <summary>
    /// Port on which the server should be listening, it should be accessable from outside if clients are supposed to connect from different machine, can be a configurable parameter in future
    /// </summary>
    private int _port = 50051;
    private int _numDrones = 2;

    /// <summary>
    /// 
    /// </summary>
    //private GameController _gameController;
    private GameController _gameController;

    /// <summary>
    /// TImeStep controller, responsible for running the game simulation
    /// </summary>
    private TimeStepController _timeStepController;

    private bool[] _needReset;

    /// <summary>
    /// A handle that is used to block the thread which recieves the remote procedure call until the procedure is executed in the main Unity thread (necessary as UnityEngine calls have to run in the main thread)
    /// </summary>
    private AutoResetEvent _stopWaitHandle;

    /// <summary>
    /// The total simulated time so far in nanoseconds
    /// </summary>
    private long _totalSimulatedTime = 0;
    private long _shutdownCounter = 0;
    private int _executionMode;  // 0=learn, 2=infer. 3=test


    /// ===========================Data holders for the requests and responses of the procedures, necessary for syncing the data between the main Untty thread and the server thread
    private InitializeRequest _initializeRequest = null;
    private InitializeReply _initializeReply = null;

    private ResetRequest _resetRequest = null;
    private ResetReply _resetReply = null;

    private ShutdownRequest _shutdownRequest = null;
    private ShutdownReply _shutdownReply = null;

    private RunLoopStepRequest _runLoopStepRequest = null;
    private RunLoopStepReply _runLoopStepReply = null;

    private SetDataPacksRequest _setDataPacksRequest = null;
    private SetDataPacksReply _setDataPacksReply = null;

    private GetDataPacksRequest _getDataPacksRequest = null;
    private GetDataPacksReply _getDataPacksReply = null;
    /// ===============================================================================================================================================================================



    
    
    private void ParseCommandLineArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if(args[i] == "--port")
            {
                int.TryParse(args[i+1], out _port);
            }
            else if(args[i] == "--num_agents")
            {
                int.TryParse(args[i+1], out _numDrones);
            }
            else if(args[i] == "--mode")
            {
                int.TryParse(args[i+1], out _executionMode);
            }
        }
        Debug.Log("[CommServiceController] _executionMode is " + _executionMode);
    }



    private void Awake() //Start()
    {
        ParseCommandLineArgs();

        bool failFlag = true;

        while(failFlag)
        {
            try
            {
                _timeStepController = FindObjectOfType<TimeStepController>();
            }
            catch(Exception ex)
            {
                Debug.LogException(ex, this);
            }

            try 
            {
                _gameController = FindObjectOfType<GameController>();
            }
            catch(Exception ex)
            {
                Debug.LogException(ex, this);
            }
            Debug.Log("[CommControl] _gameController was found: " + _gameController);

            try {
                _gameController.SpawnDrones(_numDrones);
            }
            catch(Exception ex)
            {
                Debug.LogException(ex, this);
            }

            DontDestroyOnLoad(transform.gameObject);
            _stopWaitHandle = new AutoResetEvent(false);
            _invokedProcedure = ServiceProcedure.NONE;
            Debug.Log("[CommControl] Starting gRPC server");
            try{
                StartServer();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
            Debug.Log("[CommControl] gRPC Server successfully started");

            failFlag = false;

        }

        _needReset = new bool[_numDrones];
        for  (int i=0;i<_numDrones;i++)
        {
            _needReset[i]=false;
        }
       
    }



    private void initializeUser(string simulationConfigJson)
    {
        // User-defined additional initialization related to NRP core code should go here
    }

    private void resetUser()
    {
        // User-defined reset code should go here
        _gameController.ResetAllDrones();
    }

    private void shutdownUser()
    {
        // User-defined shutdown code should go here

        // Shutdown in 2s - there are 50 iterations of fixed update per second,
        // so 2 * 50 = 100
        _shutdownCounter = 100;

        Debug.Log("[SHUTDOWN] started");
    }



    private void setDataPacksUser(IList<DataPackMessage> dataPacks)   // from NRP-core into Unity engine 
    {
        NrpGenericProto.ArrayFloat action;

        // Iterating through incoming DataPacks
        foreach (DataPackMessage dataPack in dataPacks)
        {
            //Debug.Log("[setDataPackUser] Trying to find 'action' in datapacks... ");
            if(dataPack.DataPackId.DataPackName == "action")
            {
                //Debug.Log("[setDataPackUser] Trying to unpack 'action'... ");
                dataPack.Data.TryUnpack<NrpGenericProto.ArrayFloat>(out action);
                Debug.Log("[CommServiceController] [setDataPackUser] After unpack, array is:\n" + action.Array + " \n.\n ");
                if (action.Array != null && action.Array.Count > 0) _gameController.SetActions(action.Array);
            }
        }
    }



    private long runLoopUser(long timeStep)
    {
        // User-defined runLoop code should go here
        return (long)_timeStepController.RunGame(_runLoopStepRequest.TimeStep);
    }




    private DataPackMessage createDataPack(string dataPackName, string engineName, Google.Protobuf.IMessage message)
    {
        DataPackMessage dataPack = new DataPackMessage();
        dataPack.DataPackId = new DataPackIdentifier();
        dataPack.DataPackId.DataPackName = dataPackName;
        dataPack.DataPackId.EngineName   = engineName;
        dataPack.Data = Any.Pack(message);
        return dataPack;
    }




    private List<DataPackMessage> getDataPacksUser()  // from the Unity engine to NRP-core
    {
        List<DataPackMessage> dataPacks = new List<DataPackMessage>();

        NrpGenericProto.ArrayFloat positionProto = _gameController.GetObservations();
        dataPacks.Add(createDataPack("observation", _engineName, positionProto));

        (NrpGenericProto.ArrayFloat rewardProto, NrpGenericProto.ArrayBool isDoneProto, 
         NrpGenericProto.ArrayBool hasLandedProto) = _gameController.GetRewardsAndDones();

        dataPacks.Add(createDataPack("reward",     _engineName, rewardProto));
        dataPacks.Add(createDataPack("is_done",    _engineName, isDoneProto));
        dataPacks.Add(createDataPack("has_landed", _engineName, hasLandedProto));

        // Call reset for drones that are "done"
        /*
        for(int i = 0; i < isDoneProto.Array.Count; i++)
        {
            if(isDoneProto.Array[i])
            {
                _gameController.ResetDrone(i);
            }
        }
        */

        for(int i = 0; i < isDoneProto.Array.Count; i++)
        {
            if(isDoneProto.Array[i])
            {
                _needReset[i] = true;
            }
        }
        Debug.Log("[CommServiceController] [getDataPackUSer] dataPacks: " + dataPacks);
        Debug.Log("[CommServiceController] [getDataPackUSer] dataPacks[0]: " + dataPacks[1]);

        return dataPacks;
    }
    



    private void FixedUpdate()
    {

        if(_shutdownCounter > 0)
        {
            Debug.Log("[SHUTDOWN] count: " + _shutdownCounter);
            _shutdownCounter--;
            if(_shutdownCounter == 0)
            {
                StopServer();
                Application.Quit();
            }
        }

        lock(_locker)
        {   
            if(_invokedProcedure != ServiceProcedure.NONE)
            {
                switch(_invokedProcedure)
                {
                    case ServiceProcedure.INITIALIZE:
                        initializeUser(_initializeRequest.Json);
                        _totalSimulatedTime = 0;
                        _initializeReply = new InitializeReply { Json = "1"};
                        Debug.Log("[CommController] Service procedure INITIALIZE completed.\n.\n"); 
                        break;


                    case ServiceProcedure.RESET:
                        resetUser();
                        _totalSimulatedTime = 0;
                        _resetReply = new ResetReply { Json = "1" };
                        Debug.Log("\n[CommController] Service procedure RESET completed.\n.\n"); 
                        break;


                    case ServiceProcedure.SHUTDOWN:
                        shutdownUser();
                        _shutdownReply = new ShutdownReply { Json = "" };
                        break;


                    case ServiceProcedure.RUN_LOOP_STEP:

                        Debug.Log(".\n[CommController] Service procedure RUN_LOOP_STEP received."); 
                        
                        if (_executionMode!=3)
                        {
                            // Call reset for drones that are "done"
                            for(int i = 0; i < _numDrones; i++)
                            {
                                if(_needReset[i])
                                {
                                    _gameController.ResetDrone(i);
                                }
                                _needReset[i] = false;
                            }
                        }
                        

                        long simulatedTimeStep = runLoopUser(_runLoopStepRequest.TimeStep);

                        _totalSimulatedTime += simulatedTimeStep;
                        _runLoopStepReply = new RunLoopStepReply { EngineTime =_totalSimulatedTime };
                        Debug.Log("[CommController] Service procedure RUN_LOOP_STEP completed; exiting.\n.\n"); 
                        break;


                    case ServiceProcedure.SET_DATAPACKS:
                        setDataPacksUser(_setDataPacksRequest.DataPacks);
                        _setDataPacksReply = new SetDataPacksReply();
                        Debug.Log(".\n[CommController] Service procedure SET_Datapacks completed.\n."); 
                        break;


                    case ServiceProcedure.GET_DATAPACKS:
                        List<DataPackMessage> dataPacks = getDataPacksUser();

                        _getDataPacksReply = new GetDataPacksReply();
                        _getDataPacksReply.DataPacks.Add(dataPacks);
                        Debug.Log(".\n[CommController] Service procedure GET_Datapacks completed.\n."); 

                        break;
                }

                _invokedProcedure = ServiceProcedure.NONE;
                _stopWaitHandle.Set();
            }
        }
    }




    public InitializeReply InitializeAsync(InitializeRequest request)
    {
        _initializeRequest = request;
        lock(_locker)
        {
            _invokedProcedure = ServiceProcedure.INITIALIZE;
        }
        _stopWaitHandle.WaitOne();
        return _initializeReply;
    }

    public ResetReply ResetAsync(ResetRequest request)
    {
        _resetRequest = request;
        lock(_locker)
        {
            _invokedProcedure = ServiceProcedure.RESET;
        }
        _stopWaitHandle.WaitOne();
        return _resetReply;
    }

    public ShutdownReply ShutdownAsync(ShutdownRequest request)
    {
        _shutdownRequest = request;
        lock(_locker)
        {
            _invokedProcedure = ServiceProcedure.SHUTDOWN;
        }
        _stopWaitHandle.WaitOne();
        return _shutdownReply;
    }

    public GetDataPacksReply GetDataPacksAsync(GetDataPacksRequest request)
    {
        _getDataPacksRequest = request;
        lock(_locker)
        {
            _invokedProcedure = ServiceProcedure.GET_DATAPACKS;
        }
        _stopWaitHandle.WaitOne();
        return _getDataPacksReply;
    }

    public RunLoopStepReply RunLoopStepAsync(RunLoopStepRequest request)
    {
        _runLoopStepRequest = request;
        lock(_locker)
        {
            _invokedProcedure = ServiceProcedure.RUN_LOOP_STEP;
        }
        _stopWaitHandle.WaitOne();
        return _runLoopStepReply;
    }

    public SetDataPacksReply SetDataPacksAsync(SetDataPacksRequest request)
    {
        _setDataPacksRequest = request;
        lock(_locker)
        {
            _invokedProcedure = ServiceProcedure.SET_DATAPACKS;
        }
        _stopWaitHandle.WaitOne();
        return _setDataPacksReply;
    }

    public void StartServer()
    {
        Debug.Log("XXX Port is: " + _port + " and IP address is: " + _ipAddress);
        _server = new Server
        {
            Services = { EngineGrpcService.BindService(new EngineGrpcServiceImpl(this)) },
            Ports = { new ServerPort(_ipAddress, _port, ServerCredentials.Insecure) }
        };
        _server.Start();
    }

    public void StopServer()
    {
        _server.ShutdownAsync().Wait();
    }

    /// <summary>
    /// An implementation of the service.
    /// If new remote procedure is defined in the proto file, a new Unity classes Service.cs and ServiceGrpc.cd should be generated and imported in the project to replace the existing ones.
    /// Then, the new method should be implemented as the ones below
    /// </summary>
    class EngineGrpcServiceImpl : EngineGrpcService.EngineGrpcServiceBase
    {

        private CommunicationServiceController _communicationServiceControlller;

        public EngineGrpcServiceImpl(CommunicationServiceController communicationServiceControlller)
        {
            _communicationServiceControlller = communicationServiceControlller;
        }

        public override Task<InitializeReply> initialize(InitializeRequest request, ServerCallContext context)
        {
            Debug.Log(".\n[CommController] Initialize request received");
            return Task.FromResult(_communicationServiceControlller.InitializeAsync(request));
        }

        public override Task<ShutdownReply> shutdown(ShutdownRequest request, ServerCallContext context)
        {
            return Task.FromResult(_communicationServiceControlller.ShutdownAsync(request));
        }

        public override Task<ResetReply> reset(ResetRequest request, ServerCallContext context)
        {
        return Task.FromResult(_communicationServiceControlller.ResetAsync(request));
        }

        public override Task<RunLoopStepReply> runLoopStep(RunLoopStepRequest request, ServerCallContext context)
        {
            return Task.FromResult(_communicationServiceControlller.RunLoopStepAsync(request));
        }

        public override Task<SetDataPacksReply> setDataPacks(SetDataPacksRequest request, ServerCallContext context)
        {
            return Task.FromResult(_communicationServiceControlller.SetDataPacksAsync(request));
        }

        public override Task<GetDataPacksReply> getDataPacks(GetDataPacksRequest request, ServerCallContext context)
        {
            return Task.FromResult(_communicationServiceControlller.GetDataPacksAsync(request));
        }
    }
}