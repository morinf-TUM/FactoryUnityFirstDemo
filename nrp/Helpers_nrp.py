import numpy as np
from copy import deepcopy
from typing import Any, Dict, List
import operator
import sys




def create_logger(s = 'my.log'):
    logger = open(s, 'w')
    return logger
    



def normalize_policy_input(output_range: range, input_range: range, input):
    output_range_diff = output_range.stop - output_range.start
    input_range_diff  = input_range.stop  - input_range.start
    return output_range.start + ((output_range_diff) / (input_range_diff)) * (input - input_range.start)





def map_from_normalized_to_natural_range(input_range: range, ni):
    """
    Converts observations from [-1, 1] into Unity range
    """
    input_range_diff  = input_range.stop  - input_range.start
    return input_range.start + input_range_diff * ni





def revert_normalized_policy_input(x_range: range, y_range: range, z_range: range, v_range: range, val):   
    """
    Converts observations from [-1, 1] into Unity range
    """
    x  = map_from_normalized_to_natural_range(x_range, val[0])
    y  = map_from_normalized_to_natural_range(y_range, val[1])
    z  = map_from_normalized_to_natural_range(z_range, val[2])
    vx = map_from_normalized_to_natural_range(v_range, val[3])
    vy = map_from_normalized_to_natural_range(v_range, val[4])
    vz = map_from_normalized_to_natural_range(v_range, val[5])
    return np.array([x, y, z, vx, vy, vz])








