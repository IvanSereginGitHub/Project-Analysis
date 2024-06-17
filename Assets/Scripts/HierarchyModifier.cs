using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HierarchyModifier : MonoBehaviour
{
  public int hierarchyIndex;
  int siblingIndex;
  void LateUpdate()
  {
    if(hierarchyIndex < 0)
      hierarchyIndex = transform.parent.childCount + hierarchyIndex;
    hierarchyIndex = Mathf.Clamp(hierarchyIndex, 0, transform.parent.childCount);

    siblingIndex = transform.GetSiblingIndex();

    if (hierarchyIndex != siblingIndex)
    {
      transform.SetSiblingIndex(hierarchyIndex);
    }
  }
}
