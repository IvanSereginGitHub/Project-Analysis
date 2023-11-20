using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyTrigger : MonoBehaviour
{
    public float destroyAfter;
    public void Start()
    {
        Destroy(gameObject, destroyAfter);
    }
}
