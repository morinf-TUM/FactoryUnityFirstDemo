import numpy as np
from nrp_core import *
from nrp_core.data.nrp_json import *
from nrp_core.data.nrp_protobuf import *


@EngineDataPack(keyword='actions', id=DataPackIdentifier('actions', 'policy'))
@TransceiverFunction("policy")
def actions_to_unity_engine(actions):
    
    if actions.isEmpty():
        print("WARNING: Empty actions")
        jdp = JsonDataPack('actions','policy')
    else:
        jdp = JsonDataPack('actions','policy')
        for i in actions.data.keys():
            jdp.data[i] = actions.data[i]

    return [jdp]

    #actions_datapack = JsonDataPack("actions", "policy_python_json_engine")
    
    #if not actions.isEmpty():
    #    actions_datapack.data['actions'] = actions.data['actions']

    #return [actions_datapack]
