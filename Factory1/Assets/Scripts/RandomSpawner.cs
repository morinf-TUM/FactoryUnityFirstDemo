using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
    public GameObject[] googleObjects;

    public void spawnObj(int counter){
        for (int i = 0; i < counter; i++)
        {
            Vector3 randomSpawnPosition = new Vector3(Random.Range((float)-6.2, (float)-5.8), (float)1.3, Random.Range((float)11, (float)12));
            Instantiate(googleObjects[i], randomSpawnPosition, Quaternion.identity);
        }
    }
}
