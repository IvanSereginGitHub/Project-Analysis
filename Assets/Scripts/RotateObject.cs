using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public float anglePerSecond;

    void FixedUpdate()
    {
        transform.localEulerAngles += new Vector3(0, 0, anglePerSecond * Time.deltaTime);
    }
}
