import numpy as np
from nrp_core import *
from nrp_core.data.nrp_json import *
from nrp_core.data.nrp_protobuf import *
from nrp_protobuf.nrpgenericproto_pb2 import ArrayFloat
import traceback


@EngineDataPack(keyword='actions', id=DataPackIdentifier('actions', 'policy'))
@TransceiverFunction("unity")
def actions_to_unity(actions):
    
    x = NrpGenericProtoArrayFloatDataPack('action', "unity")
        
    try:
        for k in actions.data.keys(): 
        
            if ( not ( actions.data[k] is None ) and ( len(actions.data[k])>0 )  ):
                for i in range(len(actions.data[k])):
                    x.data.array.extend(actions.data[k][i])
    except:
        print("WARNING: error creating protobuf datapack.")
        print(traceback.format_exc())
    
    
    print('DataPack x successfully set:')
    print(x)
    print('\n')

    return [x]
