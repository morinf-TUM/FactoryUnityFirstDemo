import numpy as np
from nrp_core import *
from nrp_core.data.nrp_json import *
from nrp_core.data.nrp_protobuf import *


@EngineDataPack(keyword='observation', id=DataPackIdentifier('observation', 'unity'))
@TransceiverFunction("policy")
def observations_to_policy(observation):
    
    observations_JSON_datapack = JsonDataPack("observation", "policy")
    obslen = len(observation.data.array)
    
    if (obslen>0):
        obs=[]

        try:
            for i in range(obslen):
                obs.append(observation.data.array[i]) 

            observations_JSON_datapack.data["obs"] = obs #
        except:
            print("[observations_to_policy] Something went wrong.")

    else:
        print("[observations_to_policy] Empty observation.")

    return [observations_JSON_datapack]
    
    
    