using UnityEngine;
using System;
using System.Reflection;

public class SaveLoadVariable : MonoBehaviour
{
    public GameObject inputGameobject, outputGameobject;

    public string inputVariablePath, outputVariablePath;

    public string customName = "";

    // source -> variable -> type
    Tuple<dynamic, dynamic, Type> inputTuple, outputTuple;

    void Awake()
    {
        PrepareInfo(inputVariablePath, inputGameobject, ref inputTuple);
        PrepareInfo(outputVariablePath, outputGameobject, ref outputTuple);
        LoadVariable();
    }
    void PrepareInfo(string variablePath, GameObject selectedObj, ref Tuple<dynamic, dynamic, Type> tuple)
    {
        var tmp = ProjectManager.FromStringToOrigin(selectedObj, variablePath, out dynamic newSrc);
        tuple = new Tuple<dynamic, dynamic, Type>(newSrc, tmp.Item2, tmp.Item1.GetType());

    }
    [ContextMenu("Save variable")]
    public void SaveVariable()
    {
        PlayerPrefs.SetString(customName.Length > 0 ? customName : this.gameObject.name, inputTuple.Item2.GetValue(inputTuple.Item1).ToString());
    }
    [ContextMenu("Load variable")]
    public void LoadVariable()
    {
        if (PlayerPrefs.HasKey(customName.Length > 0 ? customName : this.gameObject.name))
        {
            ProjectManager.FromStringToSetValue(inputTuple.Item1, inputTuple.Item2, PlayerPrefs.GetString(customName.Length > 0 ? customName : this.gameObject.name), inputTuple.Item3);
            ProjectManager.FromStringToSetValue(outputTuple.Item1, outputTuple.Item2, PlayerPrefs.GetString(customName.Length > 0 ? customName : this.gameObject.name), outputTuple.Item3);
        }
    }
}
