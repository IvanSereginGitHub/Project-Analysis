using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformChanger : MonoBehaviour
{
    public enum TransformPartEnum
    {
        None,
        Position,
        LocalPosition,
        Rotation,
        LocalRotation,
        Scale,
        SizeDelta
    }
    public Transform transformToChange = null;
    public TransformPartEnum transformPartToChange;

    public Vector3 changeVector;

    void Start()
    {
        if (transformToChange == null)
        {
            transformToChange = transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (transformPartToChange)
        {
            case TransformPartEnum.Position:
                transformToChange.position += changeVector * Time.deltaTime;
                break;
            case TransformPartEnum.LocalPosition:
                transformToChange.localPosition += changeVector * Time.deltaTime;
                break;
            case TransformPartEnum.Rotation:
                transformToChange.eulerAngles += changeVector * Time.deltaTime;
                break;
            case TransformPartEnum.LocalRotation:
                transformToChange.localEulerAngles += changeVector * Time.deltaTime;
                break;
            case TransformPartEnum.Scale:
                transformToChange.localScale += changeVector * Time.deltaTime;
                break;
        }
    }
}
