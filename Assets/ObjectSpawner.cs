using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject objectToSpawn;
    public int spawnAmount = -1;
    public float spawnDelay = 1f;
    public Vector3 spawnPositionOffset;
    Vector3 spawnPos = new Vector3(0, 0, 0);
    public int beatReactorSpectrumIndexOffset = 0;
    public float destroyAfter = 1f;
    float destroyAfterTime = 0f;
    public bool incrementDestroyTime = true;
    int spectrumOffset = 0;
    float timer = 0f;

    void Start()
    {
        destroyAfterTime = destroyAfter;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (spawnAmount == 0)
        {
            return;
        }

        if (timer >= spawnDelay)
        {
            spawnPos += spawnPositionOffset;
            spectrumOffset += beatReactorSpectrumIndexOffset;
            BeatReactorTrigger spawnedObj = Instantiate(objectToSpawn, objectToSpawn.transform.position + spawnPos, Quaternion.identity).GetComponent<BeatReactorTrigger>();
            if (incrementDestroyTime)
            {
                destroyAfterTime += destroyAfter;
            }
            if (destroyAfter != 0f)
            {
                DestroyTrigger destTrig = spawnedObj.gameObject.AddComponent<DestroyTrigger>();
                destTrig.destroyAfter = destroyAfterTime;
            }
            spawnedObj.IncrementSpectrumValues(spectrumOffset, 0);
            spawnAmount--;
            timer = 0f;
        }
    }
}
