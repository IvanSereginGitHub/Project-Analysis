using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundToCamera : MonoBehaviour
{
  [SerializeField]
  Camera camera;
  [SerializeField]
  List<GameObject> confinementList;
  [SerializeField]
  bool isBound;
  void LateUpdate()
  {
    if (isBound)
    {
      Vector3 higherCorner = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.transform.position.z));
      Vector3 lowerCorner = camera.ScreenToWorldPoint(new Vector3(0, 0, camera.transform.position.z));
      confinementList.ForEach((obj) =>
      {
        if (obj.TryGetComponent<Renderer>(out Renderer spr))
        {
          Vector3 viewPos = obj.transform.position;

          float objectWidth = spr.bounds.extents.x;
          float objectHeight = spr.bounds.extents.y;

          viewPos.x = Mathf.Clamp(viewPos.x, lowerCorner.x + objectWidth, higherCorner.x - objectWidth);
          viewPos.y = Mathf.Clamp(viewPos.y, lowerCorner.y + objectHeight, higherCorner.y - objectHeight);
          obj.transform.position = viewPos;
        }
      });
    }
  }
}
