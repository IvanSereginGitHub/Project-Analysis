using System.Collections;
using System.Collections.Generic;
using PathCreation;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 50f;

    public PathCreator pathToFollow;
    // Update is called once per frame
    void Update()
    {
        transform.position += speed * Time.deltaTime * transform.forward;
    }
}
