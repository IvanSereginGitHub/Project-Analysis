using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System;
[System.Serializable]
public class ObjectTypeClass
{
  public TargetObjectType objectType;
  public int objID;
}
public enum TargetObjectType
{
  ObjectID,
  Player,
  SpawnedObject,
  MainCamera,
  NormalCamera,
  BackgroundCamera,
  AllCameras,
  CurrentObject
}

[System.Serializable]
public class ReactorTriggersClass
{
  public bool autoFade = true;
  public float fadeMultiplier = 100f;
  public Vector4 affectAxis = new Vector4(1, 1, 0, 0);

  public float low_threshold = 0;
  public float high_threshold = 1;

  public Vector4 defaultVector = new Vector4(1, 1, 0, 0);
  public Vector4 finalVector = new Vector4(1, 1, 0, 0);
  Vector4 vectorToChange = new Vector4(0, 0, 0, 0);

  Vector4 prevVector;

  public string pathToVariable = "";

  public List<Tuple<GameObject, object, dynamic>> selectedObjects = new List<Tuple<GameObject, object, dynamic>>();

  object valueSource;
  dynamic entryToApply;
  Type typeToConvert;

  public float ChangeVectorVal(float targetVal, float prevVal, int axisIndex)
  {
    if (affectAxis[axisIndex] <= 0)
    {
      return prevVal;
    }

    if (targetVal >= low_threshold && targetVal <= high_threshold)
    {
      return Mathf.Lerp(defaultVector[axisIndex], finalVector[axisIndex], targetVal) * affectAxis[axisIndex];
    }

    if (autoFade)
    {
      return Mathf.Lerp(prevVal, defaultVector[axisIndex], fadeMultiplier * Time.deltaTime);
    }
    return prevVal;
  }

  public void ApplyValuesToVector(Vector4 values)
  {
    vectorToChange = new Vector4(
      ChangeVectorVal(values.x, vectorToChange.x, 0),
      ChangeVectorVal(values.y, vectorToChange.y, 1),
      ChangeVectorVal(values.z, vectorToChange.z, 2),
      ChangeVectorVal(values.w, vectorToChange.w, 3)
    );
  }

  public void ApplyValuesToVector(float value)
  {
    vectorToChange = new Vector4(
      ChangeVectorVal(value, vectorToChange.x, 0),
      ChangeVectorVal(value, vectorToChange.y, 1),
      ChangeVectorVal(value, vectorToChange.z, 2),
      ChangeVectorVal(value, vectorToChange.w, 3)
    );
  }
  public void InitializePath(GameObject targetComponent, GameObject gameObject, ObjectTypeClass objectTypeClass)
  {
    if (pathToVariable != "")
    {
      List<GameObject> tempList = new List<GameObject>();
      if (targetComponent != null)
        tempList = new List<GameObject> { targetComponent };
      else
      {
        tempList = BeatReactorTrigger.AddObjectsToList(objectTypeClass.objectType, objectTypeClass.objID, gameObject);
      }

      tempList.ForEach((obj) =>
      {
        var tmp = ProjectManager.FromStringToOrigin(obj, pathToVariable, out dynamic newSrc);
        if (newSrc is not null)
          selectedObjects.Add(new Tuple<GameObject, object, dynamic>(obj, newSrc, tmp.Item2));
        typeToConvert = tmp.Item1.GetType();
      });
    }
  }

  public void ApplyVectorToComponent(bool addValue = false)
  {
    for (int i = 0; i < selectedObjects.Count; i++)
    {
      ProjectManager.FromStringToSetValue(selectedObjects[i].Item2, selectedObjects[i].Item3, vectorToChange, typeToConvert, addValue);
    }
  }

  public void ResetVectors()
  {
    vectorToChange = defaultVector;
    prevVector = vectorToChange;
  }
}

//Currently very obsolete
public class BeatReactorTrigger : MonoBehaviour
{

  public static List<GameObject> AddObjectsToList(TargetObjectType objectType, int objID, GameObject currentObj = null)
  {
    List<GameObject> objects = new List<GameObject>();
    switch (objectType)
    {
      case TargetObjectType.NormalCamera:
        objects.Add(GameObject.FindWithTag("NormalCamera"));
        break;
      case TargetObjectType.BackgroundCamera:
        objects.Add(GameObject.FindWithTag("BackgroundCamera"));
        break;
      case TargetObjectType.CurrentObject:
        objects.Add(currentObj);
        break;
      case TargetObjectType.MainCamera:
        objects.Add(Camera.main.gameObject);
        break;
    }
    return objects;
  }
  public ObjectTypeClass objectTypeClass;
  public enum SpectrumValueMethod
  {
    Highest,
    Average,
    Summ
  }
  [SerializeField]
  GameObject targetComponent;
  public SpectrumValueMethod spectrumCalcMethod;
  public int spectrumIndex = 0;
  public int spectrumCount = 1;
  public bool addToCurrentValue;
  float spectrumVal;
  float[] samples = new float[1024];
  List<float> allSpectrumValues;
  [SerializeField]
  AudioSource audSource;
  public ReactorTriggersClass reactorTriggerClass = new ReactorTriggersClass();
  float[] tempArr;

  [SerializeField]
  AudioManager manager = null;
  void Start()
  {
    reactorTriggerClass.ResetVectors();

    Initialize();
  }

  public static float[] GetSpectrumDataArr(AudioSource audSource, float[] samples)
  {
    audSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
    return samples;
  }

  void FixedUpdate()
  {
    ExecuteTrigger();
  }

  public void IncrementSpectrumValues(int index, int count)
  {
    spectrumIndex += index;
    while (spectrumIndex >= 1024)
    {
      spectrumIndex -= 1024;
    }

    spectrumCount += count;
    while (spectrumIndex + spectrumCount > 1024)
    {
      spectrumCount = 1024 - spectrumIndex;
    }
  }

  public void ExecuteTrigger()
  {
    allSpectrumValues = new List<float>();

    while ((spectrumIndex + spectrumCount) > 1024)
    {
      spectrumCount--;
    }

    tempArr = GetSpectrumDataArr();

    for (int i = 0; i < spectrumCount; i++)
    {
      allSpectrumValues.Add(tempArr[spectrumIndex + i]);
    }

    switch (spectrumCalcMethod)
    {
      case SpectrumValueMethod.Average:
        spectrumVal = 0;
        allSpectrumValues.ForEach
        (
            delegate (float val)
            {
              spectrumVal += val;
            }
        );
        spectrumVal /= allSpectrumValues.Count;
        break;
      case SpectrumValueMethod.Highest:
        spectrumVal = allSpectrumValues.Max(x => x);
        break;
      case SpectrumValueMethod.Summ:
        spectrumVal = allSpectrumValues.Sum(x => x);
        break;
    }

    reactorTriggerClass.ApplyValuesToVector(spectrumVal);
    reactorTriggerClass.ApplyVectorToComponent(addToCurrentValue);
  }

  public float GetSpectrumDataAt(int index)
  {
    return samples[index];
  }

  float[] GetSpectrumDataArr()
  {
    return manager == null ? GetSpectrumDataArr(audSource, samples) : manager.GetSpectrumDataArr();
  }

  public void StopExecutingTrigger()
  {
    reactorTriggerClass.selectedObjects.Clear();
  }

  public void Initialize()
  {
    if (audSource == null)
      audSource = FindObjectOfType<AudioSource>();

    reactorTriggerClass.InitializePath(targetComponent, gameObject, objectTypeClass);
    reactorTriggerClass.ResetVectors();
  }
}
