using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
//[ExecuteAlways]
public class DropdownExtension : MonoBehaviour
{
  [SerializeField]
  VariablesObject varObj;
  TMP_Dropdown tmp_drop;
  Dropdown base_drop;
  [SerializeField]
  List<UnityEvent<int>> Events = new List<UnityEvent<int>>();
  [SerializeField]
  List<UnityEvent<string>> TransformedOptionsEvents = new List<UnityEvent<string>>();
  public bool separateEventForEachOption = true;
  void Start()
  {
    if (!gameObject.TryGetComponent(out tmp_drop))
    {
      if (!gameObject.TryGetComponent(out base_drop))
      {
        return;
      }
    }

    // if (varObj != null)
    // {
    //   Events.Clear();
    //   for (int i = 0; i < varObj.variableCallers.Count; i++)
    //   {
    //     Events.Add(new CustomEvent());
    //     Events[i].AddListener(val => { varObj.EnableObject(val); });
    //   }
    // }
  }

  string GetOptionsValue(int val)
  {
    if (tmp_drop != null)
      return tmp_drop.options[val].text;
    else if (base_drop != null)
      return base_drop.options[val].text;
    return "";
  }

  void ExecuteEvents<T>(List<UnityEvent<T>> events, int ind, T val)
  {
    if (Events.Count < 0)
      return;

    if (separateEventForEachOption)
    {
      events[ind].Invoke(val);
      return;
    }
    events.ForEach((x) => x.Invoke(val));
  }

  public void UpdateValues()
  {
    if (tmp_drop != null)
    {
      ExecuteEvents(Events, tmp_drop.value, tmp_drop.value);
      ExecuteEvents(TransformedOptionsEvents, tmp_drop.value, GetOptionsValue(tmp_drop.value));
    }
    else if (base_drop != null)
    {
      ExecuteEvents(Events, base_drop.value, base_drop.value);
      ExecuteEvents(TransformedOptionsEvents, base_drop.value, GetOptionsValue(base_drop.value));
    }
  }
}
