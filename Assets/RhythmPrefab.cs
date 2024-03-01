using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhythmPrefab : MonoBehaviour
{
    bool startedMoving = false;

    GameObject target;

    float speed = 0f;


    public void StartMoving(GameObject target, float speed)
    {
        this.target = target;
        this.speed = speed;
        startedMoving = true;
    }
    // Update is called once per frame
    void Update()
    {
        if (!startedMoving)
            return;
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
    }
}
