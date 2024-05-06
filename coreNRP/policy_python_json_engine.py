import time
#import cv2
import numpy as np
#import tensorflow.compat.v1 as tf
from nrp_core.engines.python_json import EngineScript
from stable_baselines3 import PPO, DDPG

#tf.disable_v2_behavior()


class Script(EngineScript):

    def __init__(self) -> None:
        super().__init__()
        self.algo = 2 #1=PPO; 2=DDPG
        self.model = None
        self.model_filename = None
        env = None

        if (self.algo == 1):
            self.model_filename = "../nrp/ppo_vec"
            self.model = PPO.load(self.model_filename, env=env)
        elif (self.algo == 2):
            self.model_filename = "../nrp/ddpg_vec"
            self.model = DDPG.load(self.model_filename, env=env)
        print("\nPolicy network loaded. \nInitialization of Python engine over.\n")
        print(self.model_filename)
        

    

    def initialize(self):
        print ("\nInitialize instrution received by the Python engine")
        self._registerDataPack("actions")
        self._registerDataPack("observation")
        self._setDataPack("actions", {"data": None})

        print("Datapacks registered and set.")
        print('\n')




    def runLoop(self, timestep_ns):
        
        time_stamp = time.time_ns()
        #print("ENTERING RUNLOOP")
        
        try:
            received_data = self._getDataPack("observation") 
        except:
            print("[policy runloop] _getDatapacks failed.")
        

        if not received_data:
            print("[policy runloop] _getDatapacks returned an empty datapack, or sommething went wrong.\n")       
        else:
            obs = received_data["obs"] 
            obs = np.array(obs)
            obs = obs.reshape(20,6)

            actions, _ = self.model.predict(obs)
           
            actions = actions.reshape(20,3)
            
            # Setting the datapack
            self._setDataPack("actions", {"data": actions.tolist()}) 
            


    def shutdown(self):
        
        print("Factory1 Policy Python JSON Engine is shutting down")