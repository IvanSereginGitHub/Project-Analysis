using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPathFollower : MonoBehaviour
{
    [SerializeField]
    AudioManager audioManager;
    Vector3 pointToMove;
    public float speed = 10f;
    private int currentPointIdx = 0;
    public bool followRotation = true;

    void Update()
    {
        (int nextPointIdx, Vector3 nextPos) = audioManager.GetNextPosition();
        if (currentPointIdx != nextPointIdx)
        {
            currentPointIdx = nextPointIdx;
        }
        float step = speed * Time.deltaTime;
        Vector3 res = Vector3.MoveTowards(transform.position, nextPos, step);
        if (followRotation)
        {
            transform.LookAt(res);
            transform.localEulerAngles += new Vector3(0, 180, 0);
        }
        transform.position = res;
    }
}
