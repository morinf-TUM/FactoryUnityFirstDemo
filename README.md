
## Setting up the environment

We recommend using Python 3.8.10 and stable-baselines 2.1.0
Install SB3, then overwrite the few SB3 files inside the installation directories with their modified version inside the SB3 folder of this repo

SB3 can be installed within the virtual environment folders, e.g., 
~/miniconda3/envs/unity1/lib/python3.8/site-packages/stable_baselines 

When running the experiments below, you may find that some libraries need to be upgraded or downgraded. We recommend that you proceed library by library.



## Getting started and learning a policy

First, get in the nrp directory:
cd nrp

To launch the script to proceed and learn a policy:
 ./master_script_vectorized.py --mode learn -a 64
 
 
 --mode can be set to test (gives a percentage of success) or infer (just uses the policy)

Manually set the corresponding parameters in "masterscript" to use one type of algorithm or another, for example:
USE_PPO  = False
USE_DDPG = True
USE_A2C  = False




To start tensorboard:
Open terminal in folder tblog and type: 

tensorboard --logdir ./ &

Then, in a browser tab:
http://localhost:6006/#timeseries



## Implementing the policy in an NRP simulation

First, get in the coreNRP directory:
cd coreNRP

To launch the an NRP-core simulation of the task using a previously-learned policy:
NRPCoreSim -c simulation_config.json 
 
 

 Set the parameter self.algo manually inside the PYthon engine to use either PPO or DDPG
 self.algo = 1 for PPO
 self.algo = 2 for DDPG
 
 

## Read about NRP:

- [ ] [Bitbucket public repo for the NRP](https://bitbucket.org/hbpneurorobotics/neurorobotics-platform/src/master/)
- [ ] [Bitbucket public repositories](https://bitbucket.org/hbpneurorobotics/workspace/repositories/)
- [ ] [Documentation](https://neurorobotics.net/Documentation/latest/)
- [ ] [Neurorobotics.net](https://neurorobotics.net/)




