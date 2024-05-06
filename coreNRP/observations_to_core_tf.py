from nrp_core import *
from nrp_core.data.nrp_json import *
from nrp_core.data.nrp_protobuf import *


@EngineDataPack(keyword='observation', id=DataPackIdentifier('observation', 'unity'))
@EngineDataPack(keyword='reward',      id=DataPackIdentifier('reward',      'unity'))
@EngineDataPack(keyword='is_done',     id=DataPackIdentifier('is_done',     'unity'))
@EngineDataPack(keyword='has_landed',  id=DataPackIdentifier('has_landed',  'unity'))
@PreprocessingFunction("unity")
def observations_to_core(observation, reward, is_done, has_landed):

    return [observation, reward, is_done, has_landed]