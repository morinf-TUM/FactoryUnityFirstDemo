#!/usr/bin/env python3

import argparse
import json
import numpy as np
from readchar import readkey, key
from copy import deepcopy
from typing import List
import sys
import signal
import torch as th

from stable_baselines3 import PPO, DDPG, A2C
from stable_baselines3.common.noise import NormalActionNoise, OrnsteinUhlenbeckActionNoise

from nrp_vec_env import NrpVecEnv



USE_PPO  = False
USE_DDPG = True
USE_A2C  = False



def parse_arguments():
    """Parses command line arguments"""

    parser = argparse.ArgumentParser()
    parser.add_argument("-m", "--mode",          type=str, default="infer", choices=["infer", "learn", "test"])
    parser.add_argument("-a", "--num_agents",    type=int, default=1)
    parser.add_argument("-n", "--num_nrp_cores", type=int, default=1)

    args = parser.parse_args()

    return args



def prepare_configs(num_agents: int, num_nrp_cores: int, mode: int) -> List[str]:
    """
    Prepares configuration files for all requested instances of NRP Core.
    The configs are based on the template configuration file.

    :param num_agents: Number of agents per instance of NRP Core
    :param num_nrp_cores: Total number of parallel NRP Cores
    :returns: A list of generated configuration file names
    """
    
    # Load configuration template
    template_file = open("simulation_config_template_v2.json")
    template = json.load(template_file)
    template_file.close()

    
    # Generate actual configs based on the template
    configs = []
    port = 50060
    for i in range(num_nrp_cores):
        config = deepcopy(template)
        config["EngineConfigs"][0]["EngineProcStartParams"] = [f"--port {port} --num_agents {num_agents} --mode {mode}"]
        config["EngineConfigs"][0]["ServerAddress"] = f"localhost:{port}"
        port += 1
        config_name = f"simulation_config{i}_v2.json"
        configs.append(config_name)
        with open(config_name, "w") as outfile:
            json.dump(config, outfile)

    return configs



def prepare_logs(num_nrp_cores: int) -> List[str]:
    """
    Generates unique log file names for all requested instances of NRP Core.

    :param num_nrp_cores: Total number of parallel NRP Cores
    :returns: A list of generated log file names
    """
    log_names = []
    for i in range(num_nrp_cores):
        log_names.append(f"log{i}.log")
    return log_names



def prepare_addresses(num_nrp_cores: int) -> List[str]:
    """
    Generates addressed for all requested instances of NRP Core.

    :param num_nrp_cores: Total number of parallel NRP Cores
    :returns: A list of generated addresses (strings)
    """
    starting_port = 50050
    nrp_addresses = []
    for port in range(starting_port, starting_port + num_nrp_cores):
        nrp_addresses.append(f"localhost:{port}")
    return nrp_addresses



def prepare_env(num_agents: int, num_nrp_cores: int, mode: int, algo: int) -> NrpVecEnv:
    configs       = prepare_configs(num_agents, num_nrp_cores, mode)
    logs          = prepare_logs(num_nrp_cores)
    nrp_addresses = prepare_addresses(num_nrp_cores)
    return NrpVecEnv(num_agents,
                     num_nrp_cores,
                     algo,
                     config_files=configs,
                     log_output_files=logs,
                     nrp_server_addresses=nrp_addresses)




def run_episode(model, env):
    obs = env.reset()

    is_done = [False]
    if USE_PPO:
        while not np.all(is_done):
            action, _states = model.predict(obs, deterministic=True)
            obs, rewards, is_done = env.step(action)
            env.render("human")
    elif USE_DDPG:
        while not np.all(is_done):
            action, _states = model.predict(obs)
            print(action)
            obs, rewards, is_done, infos = env.step(action)
            env.render("human")




def run_episode_test(model, env, num_agents):
    obs = env.reset()

    is_done = [False]*num_agents
    while not np.all(is_done):
        action, _states = model.predict(obs, deterministic=False)
        obs, rewards, is_done, infos = env.step(action)
        env.render("human")




def infer(model_filename, env):
    

    if USE_PPO:
        model = PPO.load(model_filename, env=env)
    elif USE_DDPG:
        model = DDPG.load(model_filename, env=env)
    
    vec_env = model.get_env()
    while True:
        print("SPACE to run an episode, Q to quit")
        k = readkey()

        if k == key.SPACE:
            run_episode(model, vec_env)
        if k == "q":
            break

    env.close()



    
def test(model_filename, env, num_agents, N=100):
    if USE_PPO:
        model = PPO.load(model_filename, env=env)
    elif USE_DDPG:
        model = DDPG.load(model_filename, env=env)
    vec_env = model.get_env()

    percentage_success = 0.0
    success = 0
    n = 0
    n2 = 0

    print("num_agents = ", num_agents)

    while (n<N):
        n = n + num_agents
        run_episode_test(model, vec_env, num_agents)
        has_landed = env.has_landed
        n2 = (has_landed == True).sum() # counting the new landings
        if n2 > 0:
            success = success + n2
        percentage_success = 100 * success / n
        print("For this run, n = ",n," and n2 = ", n2)
        
    print("Percentage of successful completion of the task for ",n," tries: ", percentage_success,"%")
    env.close()




def learn(model_filename, env):
    
    if USE_PPO:
        #steps per update per agent per core:
        spu = 5

        policy_kwargs = dict( net_arch=dict(pi=[32, 64, 32], vf=[512,256,128]) ,
                         activation_fn = th.nn.modules.activation.ReLU,
                         full_std = False,
                         use_expln = True,
                         log_std_init = -1,
                         share_features_extractor = True,
                         #feature_extraction="mlp", 
                         #squash_output=True
                         )
    
        model = PPO('MlpPolicy', env, 
                                verbose=0, 
                                learning_rate = 0.0007, 
                                gamma = 0.95, 
                                use_sde = True, 
                                n_steps = spu, # Number of nrp-core steps to run per update for each core
                                batch_size = spu * env.num_agents * env.num_nrp_cores, 
                               #normalize_advantage = False, 
                                n_epochs = 12,
                                clip_range = 0.1, 
                                tensorboard_log="../tblog/",
                                policy_kwargs=policy_kwargs
                                )


    
    #TODO Implement A2C:

        #model = A2C('MlpPolicy', env, verbose=0, learning_rate = 0.001, gamma = 1.0, 
    #                        use_sde = True, 
    #                        gae_lambda = 0.8, 
    #                        tensorboard_log="../tblog/",
    #                        policy_kwargs=policy_kwargs
    #                        )
        #print(model.policy)
     


    elif USE_DDPG:
        n_actions = env.action_space.shape[-1]
    
        actnoise = NormalActionNoise(mean=np.zeros(n_actions), sigma=0.4 * np.ones(n_actions))
        #actnoise = OrnsteinUhlenbeckActionNoise(mean=np.zeros(n_actions), sigma=0.05 * np.ones(n_actions))
    
        policy_kwargs = dict(activation_fn = th.nn.modules.activation.ReLU, #.Tanh
                             net_arch=dict(pi=[32, 32], qf=[32,32])
                            )
    
        model = DDPG('MlpPolicy', env, 
                                  verbose=0, 
                                  learning_rate = 0.00005, 
                                  gamma = 0.95, 
                                  action_noise = actnoise,
                                  buffer_size=1000000, 
                                  batch_size = 256,
                                  learning_starts =8000,
                                  tau = 0.005,
                                  #policy_kwargs = policy_kwargs,
                                  tensorboard_log="../tblog/",
                                  train_freq=(100, 'step'),
                                  gradient_steps = 50
                                  )
    
    print(model.policy)
    print('\n \n \n \n \n')

    model.learn(total_timesteps=int(3000000))
    model.save(model_filename)





def main():
    env = None
    # Custom signal handler for sigint
    def signal_handler(sig, frame):
        env.close()
        sys.exit(0)

    signal.signal(signal.SIGINT, signal_handler)

    if USE_PPO:
        model_filename = "ppo_vec"
        algo = 1
    elif USE_DDPG:
        model_filename = "ddpg_vec"
        algo = 2

    args = parse_arguments()
    num_agents = args.num_agents
    if args.mode == "learn":
        mode = 1
    elif args.mode == "infer":
        mode = 2
    elif args.mode == "test":
        mode = 3
    else:
        mode = 2
        print("WARNING, execution mode unreadable, set to 'test' by default.\n\n\n")
        

    env = prepare_env(num_agents, args.num_nrp_cores, mode, algo)
    #check_env(env)
    if args.mode == "infer":
        infer(model_filename, env)
    elif args.mode == "test":
        test(model_filename, env, num_agents)
    else:
        learn(model_filename, env)





if __name__ == '__main__':
    main()

# EOF
