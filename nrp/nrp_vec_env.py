#!/usr/bin/env python3

import numpy as np
from copy import deepcopy
import Helpers_nrp as hlp

from gymnasium import spaces

from stable_baselines3.common.vec_env.base_vec_env import VecEnv, VecEnvStepReturn

from nrp_client import NrpCore
from nrp_protobuf.nrpgenericproto_pb2 import ArrayFloat


x_bounds = [-6, 46]
y_bounds = [-1, 11]
z_bounds = [-6,  6]
v_bounds = [-10,10]
low_bounds_xyz = [-1.0,  -6.0, -1.0]
high_bounds_xyz= [46.0, 11.0,  6.0]
low_bounds  = low_bounds_xyz  + [-1.0,  -1.0, -1.0]
high_bounds = high_bounds_xyz + [ 1.0,   1.0,  1.0]


x_range = range(x_bounds[0], x_bounds[1])
y_range = range(y_bounds[0], y_bounds[1])
z_range = range(z_bounds[0], z_bounds[1])
normalized_range = range(0, 1)

#Initially, v was supposed to represent velocity, the use of which was eventually dropped.
#v has been converted into a "landing target variable"
v_range = range(v_bounds[0], v_bounds[1]) 
v_normalized_range = range(-1, 1)

param_unity_obs_size = 6

#TO ERASE
param_support_len = 1   #
if isinstance(param_support_len, int):
    param_obs_size = param_unity_obs_size * param_support_len
else:
    param_obs_size = np.sum(param_support_len)
param_noise_one_hot = 0.5
oneHotFlag = False
#TO ERASE




def map_range(output_range: range, input_range: range, input):
    output_range_diff = output_range.stop - output_range.start
    input_range_diff  = input_range.stop  - input_range.start
    return output_range.start + ((output_range_diff) / (input_range_diff)) * (input - input_range.start)





def actions_to_unity(val):
    """
    Converts observations from [-1, 1] into Unity range
    """
    x = map_range(v_range, normalized_range, val[0])
    y = 0.0 #map_range(v_range, normalized_range, val[1])
    z = 0.0 #map_range(v_range, normalized_range, val[2])
    return np.array([x, y, z])





def from_unity(val):
    """
    First, converts observations from Unity range into [-1, 1]
    Then, encodes each value into a vector of length support_len
    """
    x  = hlp.normalize_policy_input(normalized_range, x_range, val[0])
    y  = hlp.normalize_policy_input(normalized_range, y_range, val[1])
    z  = hlp.normalize_policy_input(normalized_range, z_range, val[2])

    vx = 0.1*val[3]
    vy = 0.1*val[4]
    vz = 0.1*val[5]

    return np.array([x, y, z, vx, vy, vz])





class NrpVecEnv(VecEnv):
    """
    A thin wrapper class for NRP Python Client, that makes it
    compatible with gym interface
    """

    metadata = {'render.modes': ['human']}

    unity_obs_size = param_unity_obs_size
    
    obs_size = param_obs_size

    has_landed = [False]

    USE_PPO  = 0
    USE_DDPG = 0

    f = hlp.create_logger()


    def __init__(self,
                 num_agents: int,
                 num_nrp_cores: int,
                 algo: int,
                 nrp_server_addresses = ["localhost:50050"],
                 config_files = ["simulation_config0_v2.json"],
                 log_output_files = ["log.txt"]):

        self.num_agents = num_agents
        self.num_nrp_cores = num_nrp_cores

        if (algo == 1):
            self.USE_PPO = 1
        elif (algo == 2):
            self.USE_DDPG = 1


        for i in range(1,num_agents):
            self.has_landed.append(False)

        
        box_obs = spaces.Box(low  = 0.0, 
                             high = 1.0, 
                             shape = (self.obs_size,), dtype=float)
        
        box_actions = spaces.Box(low  =  v_bounds[0], 
                                 high =  v_bounds[1],  
                                 shape=(3,), dtype=float)

        super(NrpVecEnv, self).__init__(num_nrp_cores * num_agents, box_obs, box_actions)
                                        
        
        assert(len(nrp_server_addresses) == num_nrp_cores)
        assert(len(config_files) == num_nrp_cores)
        assert(len(log_output_files) == num_nrp_cores)


        # Create an instance of NRP Core and initialize it
        self.nrp_cores = []
        for i in range(num_nrp_cores):
            nrp_core = NrpCore(nrp_server_addresses[i], config_file=config_files[i], log_output=log_output_files[i])
            try:
                nrp_core.initialize()
            except:
                print("nrp_core.initialize() failing.")
            self.nrp_cores.append(nrp_core)


        # Initialize aggregate buffers
        self.buf_dones  = np.zeros((self.num_envs,), dtype=bool)
        self.buf_landed = np.zeros((self.num_envs,), dtype=bool)
        self.buf_obs   = np.ndarray(shape=(self.num_envs, self.obs_size), dtype=float)
        self.buf_rews  = np.zeros((self.num_envs,), dtype=np.float32)
        print("Environment initialized")





    def step_async(self, actions: np.ndarray) -> None:
        """
        Method inherited from the base class.
        Passes the actions to step_wait() method
        """
        self.actions = actions #1st dim number of agents in env, 2nd dim being action space






    def _get_env_range(self, nrp_core_idx: int):
        return range( nrp_core_idx      * self.num_agents,
                     (nrp_core_idx + 1) * self.num_agents)






    def _prepare_actions_proto(self, nrp_core_idx: int):
        action_proto = ArrayFloat()
        for env_idx in self._get_env_range(nrp_core_idx):
            action_proto.array.extend(self.actions[env_idx]) # 3D action for movement in space

        return action_proto





    def step_wait(self) -> VecEnvStepReturn:
        """
        Method inherited from the base class.
        Runs simulation step in all instances of NRP Core.
        Actions are received through a member, which is set by step_async() method.
        Observations are aggregated from all NRP Cores into a list of arrays.
        """

        threads = [None] * self.num_nrp_cores
        thread_results = [ [] for _ in range(self.num_nrp_cores) ]

        # Run simulation steps for all NRP Cores in separate threads
        for i in range(self.num_nrp_cores):
            self.nrp_cores[i].set_proto_datapack("action", "unity", self._prepare_actions_proto(i))
            threads[i] = self.nrp_cores[i].run_loop(1, run_async=True, response=thread_results[i])


        # Wait for all NRP Cores to end their simulation step
        for i in range(self.num_nrp_cores):
            threads[i].join()


        # Retrieve the observations and aggregate them
        for i in range(self.num_nrp_cores):
            (done, observations) = self.nrp_cores[i].unpack_thread_results(thread_results[i][0])

            for env_idx in range(self.num_agents):

                # Get observations for the current agent (drone). 
                # observations have class <list>, observation has class <numpy.ndarray>, observation[] is ArrayFloat
                try:
                    observation = from_unity(observations[0].array[env_idx * self.unity_obs_size : 
                                                               self.unity_obs_size * (env_idx + 1)])  # ArrayFloat type
                except:
                    print("[Step_wait] Error getting obseervations: likely empty observation received")
                
                try:
                    reward      = observations[1].array[env_idx]  # ArrayFloat type
                except:
                    print("\n")
                    print('Read error. Observations[1] at env_idx = ', str(env_idx),  file = self.f)
                    print('            Observations[1] is: ', observations[1], file = self.f)
                    print("Type of observations is:    ", type(observations)," and observations itself is:", file = self.f)
                    print(observations, file = self.f)

                try:
                    is_done     = observations[2].array[env_idx]  # ArrayBool type
                except:
                    print('Step_wait] is_done error. Observations[2] at env_idx = ', str(env_idx), file = self.f)

                try:
                    landed = observations[3].array[env_idx]       # ArrayBool type
                except:
                    print('Done error. Observations[3] at env_idx = ', str(env_idx), file = self.f)


                # Calculate the global environment (drone) index and update the aggregate buffers
                abs_env_idx = (i * self.num_agents) + env_idx

                self.buf_dones [abs_env_idx]   = is_done
                self.buf_rews  [abs_env_idx]   = reward
                self.buf_obs   [abs_env_idx, 0:self.obs_size] = observation
                self.buf_landed[abs_env_idx]   = landed

                if (env_idx == (self.num_agents-1)): 
                    print('Observations from last drone', file = self.f)
                    print( hlp.revert_normalized_policy_input(x_range, y_range, z_range, v_range, observation), file = self.f )
                    print('Actions for last drone', file = self.f)
                    print(self.actions[env_idx],      file = self.f)
                    print('reward for last drone', file = self.f)
                    print(reward,      file = self.f)
                    print(' \n',       file = self.f)
                    if is_done: 
                        print('The last drone is DONE \n \n \n', file = self.f)
                        
                                
        #print('Rewards')
        #print(self.buf_rews)

        #print('Observations')
        #print( self.buf_obs[:,0:3])

        self.has_landed = self.buf_landed # only fine for one core
        #if self.USE_PPO:
        #    return (deepcopy(self.buf_obs), np.copy(self.buf_rews), np.copy(self.buf_dones))
        #elif self.USE_DDPG:
        #    infos = np.empty(0)
        #    for i in range(abs_env_idx+1):
        #        infos = np.append( infos,[{"landed": self.buf_landed[i]}])
        #    return (deepcopy(self.buf_obs), np.copy(self.buf_rews), np.copy(self.buf_dones), np.copy(infos))

        infos = np.empty(0)
        for i in range(abs_env_idx+1):
            infos = np.append( infos,[{"landed": self.buf_landed[i]}])
        return (deepcopy(self.buf_obs), np.copy(self.buf_rews), np.copy(self.buf_dones), np.copy(infos))


    def env_is_wrapped(self, wrapper_class, indices = None):
        """
        Method inherited from the base class.
        TODO
        """
        print("ENV")
        return [False] * self.num_envs


    def get_attr(self, attr_name: str, indices = None):
        """
        Method inherited from the base class.
        TODO
        """
        print("GET")
        return [False] * self.num_envs

                    
    def set_attr(self, attr_name: str, value, indices = None) -> None:
        """
        Method inherited from the base class.
        TODO
        """
        print("SET")
        pass

    def env_method(self, method_name: str, *method_args, indices = None, **method_kwargs):
        """
        Method inherited from the base class.
        TODO
        """
        print("ENV")
        return [False] * self.num_envs


    def reset(self):
        """
        Method inherited from the base class.
        Resets simulations in all NRP Cores
        """
        for i in range(self.num_nrp_cores):
            self.nrp_cores[i].reset()

        self.buf_obs = np.zeros(shape=(self.num_envs, self.obs_size), dtype=float)

        return deepcopy(self.buf_obs)


    def render(self, mode='human'):
        """
        Method inherited from the base class.
        TODO
        """
        pass


    def close(self):
        """
        Method inherited from the base class.
        Shutdowns all simulations.
        """
        for i in range(self.num_nrp_cores):
            self.nrp_cores[i].shutdown()


    def seed(self, seed = None):
        """
        Sets the random seeds for all environments, based on a given seed.
        Each individual environment will still get its own seed, by incrementing the given seed.
        WARNING: since gym 0.26, those seeds will only be passed to the environment
        at the next reset.

        :param seed: The random seed. May be None for completely random seeding.
        :return: Returns a list containing the seeds for each individual env.
            Note that all list elements may be None, if the env does not return anything when being seeded.
        """
        if seed is None:
            # To ensure that subprocesses have different seeds,
            # we still populate the seed variable when no argument is passed
            seed = np.random.randint(0, 2**32 - 1)

        self._seeds = [seed + idx for idx in range(self.num_envs)]
        return self._seeds

# EOF
