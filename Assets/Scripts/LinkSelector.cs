using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using System;

public class LinkSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
  public bool fixLayoutAnchorsBug;
  private TextMeshProUGUI m_TextMeshPro;
  private Canvas m_Canvas;
  private Camera m_Camera;
  public Color linkColor = Color.white;
  public Color selectColor = Color.yellow;
  public bool addUnderscoreToLinks, autogenerateLinkEventsForWeblinks;
  public List<EventMethodsClass> linkEvents;

  string originalText;
  // Flags
  private bool isHoveringObject;

  private TMP_MeshInfo[] m_cachedMeshInfoVertexData;

  int cachedLinkIndex = -1;

  public void RefreshInfo()
  {
    m_TextMeshPro = gameObject.GetComponent<TextMeshProUGUI>();
    m_Canvas = gameObject.GetComponentInParent<Canvas>();

    // Get a reference to the camera if Canvas Render Mode is not ScreenSpace Overlay.
    if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
      m_Camera = null;
    else
    {
      if (m_Canvas.worldCamera != null)
        m_Camera = m_Canvas.worldCamera;
      else
        m_Camera = Camera.main;
    }
    originalText = m_TextMeshPro.text;
    if (autogenerateLinkEventsForWeblinks)
    {
      int url_count = Regex.Matches(m_TextMeshPro.text, @"((http:\/\/)|(https:\/\/)|(www.))(.*)( |)", RegexOptions.Multiline).Count;
      for (int i = 0; i < url_count; i++)
      {
        //have to do this since index can change
        MatchCollection url_coll = Regex.Matches(m_TextMeshPro.text, @"((http:\/\/)|(https:\/\/)|(www.))(.*)( |)", RegexOptions.Multiline);
        Match t = url_coll[i];
        EventMethodsClass linkEvent = new EventMethodsClass();
        linkEvent.name = $"url_{i}";
        linkEvent.ev.AddListener(() => OpenLink(t.Value));
        linkEvents.Add(linkEvent);
        m_TextMeshPro.text = m_TextMeshPro.text.Insert(t.Index + t.Value.Length, "</link>");
        m_TextMeshPro.text = m_TextMeshPro.text.Insert(t.Index, $"<link=\"{linkEvent.name}\">");
        // string repl = $"<color=#{ColorUtility.ToHtmlStringRGBA(linkColor)}>{(addUnderscoreToLinks ? $"<u>{t.Value}</u>" : t.Value)}</color>";
        // m_TextMeshPro.text = m_TextMeshPro.text.Replace(t.Value, repl);
      }
    }

    MatchCollection coll = Regex.Matches(m_TextMeshPro.text, "(<link=\"(.*)\">(.*)</link>)|(<link=(.*)>(.*)</link>)");

    for (int i = 0; i < coll.Count; i++)
    {
      Match t = coll[i];
      string repl = $"<color=#{ColorUtility.ToHtmlStringRGBA(linkColor)}>{(addUnderscoreToLinks ? $"<u>{t.Value}</u>" : t.Value)}</color>";
      m_TextMeshPro.text = m_TextMeshPro.text.Replace(t.Value, repl);
    }
  }

  void OnEnable()
  {
    // Subscribe to event fired when text object has been regenerated.
    RefreshInfo();
    TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
  }

  void OnDisable()
  {
    // UnSubscribe to event fired when text object has been regenerated.
    if (cachedLinkIndex != -1)
      DeselectLink(cachedLinkIndex);
    TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
    m_TextMeshPro.text = originalText;
  }


  void ON_TEXT_CHANGED(UnityEngine.Object obj)
  {
    if (obj == m_TextMeshPro)
    {
      // Update cached vertex data.
      m_cachedMeshInfoVertexData = m_TextMeshPro.textInfo.CopyMeshInfoVertexData();
    }
  }

  public void OpenLink(string link)
  {
    if (Regex.IsMatch(link, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
      link = "mailto:" + link;
    Application.OpenURL(link);
  }
  public static Vector2 GetUnpivotedLocalPosition(RectTransform tr)
  {
    return new Vector2(tr.localPosition.x + tr.sizeDelta.x * (0.5f - tr.pivot.x), tr.localPosition.y + tr.sizeDelta.y * (0.5f - tr.pivot.y));
  }
  void LateUpdate()
  {
    if (isHoveringObject)
    {
      Vector3 mousePos = Input.mousePosition;
      // Nasty fix for LayoutGroup objects and custom anchors
      if (fixLayoutAnchorsBug)
      {
        mousePos += m_TextMeshPro.transform.TransformPoint(GetUnpivotedLocalPosition(m_TextMeshPro.GetComponent<RectTransform>())) - m_TextMeshPro.transform.position;
      }
      int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, mousePos, m_Camera);
      //SelectCharacter(TMP_TextUtilities.GetCursorIndexFromPosition(m_TextMeshPro, mousePos, m_Camera), Color.red);
      if (linkIndex != -1)
      {
        if (cachedLinkIndex != -1 && linkIndex != -1)
        {
          DeselectLink(cachedLinkIndex);
        }
        SelectLink(linkIndex, selectColor);

        if (Input.GetMouseButtonDown(0))
        {
          string selectedLink = m_TextMeshPro.textInfo.linkInfo[linkIndex].GetLinkID();
          EventMethodsClass found = linkEvents.Find(x => x.name == selectedLink);
          if (found != null)
            found.ev.Invoke();
        }
      }
      else if (cachedLinkIndex != -1)
      {
        DeselectLink(cachedLinkIndex);
      }
    }
    else
    {
      if (cachedLinkIndex != -1)
      {
        DeselectLink(cachedLinkIndex);
      }
    }

  }

  void SelectLink(int linkIndex, Color selectCol)
  {
    if (linkIndex == -1)
    {
      return;
    }

    cachedLinkIndex = linkIndex;
    TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
    for (int i = linkInfo.linkTextfirstCharacterIndex; i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength; i++)
    {
      //Debug.Log(i);
      int meshIndex = m_TextMeshPro.textInfo.characterInfo[i].materialReferenceIndex;

      int vertexIndex = m_TextMeshPro.textInfo.characterInfo[i].vertexIndex;
      if (i != 0 && vertexIndex == 0)
      {
        continue;
      }
      //Debug.Log(i + " " + meshIndex + " " + vertexIndex);
      // Get a reference to the vertex color
      Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[meshIndex].colors32;

      Color32 c = selectCol;
      //m_TextMeshPro.textInfo.characterInfo[i].underlineColor = c;

      vertexColors[vertexIndex + 0] = c;
      vertexColors[vertexIndex + 1] = c;
      vertexColors[vertexIndex + 2] = c;
      vertexColors[vertexIndex + 3] = c;

    }
    //for some reasons the first character in the text is selected as well, need to "fix" it
    //RestoreCachedVertexAttributes(0);
    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
  }

  void UnselectLink(int linkIndex, Color unselectCol)
  {
    if (linkIndex == -1)
    {
      return;
    }

    cachedLinkIndex = linkIndex;
    TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
    for (int i = linkInfo.linkTextfirstCharacterIndex; i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength; i++)
    {
      //Debug.Log(i);
      int meshIndex = m_TextMeshPro.textInfo.characterInfo[i].materialReferenceIndex;

      int vertexIndex = m_TextMeshPro.textInfo.characterInfo[i].vertexIndex;
      if (i != 0 && vertexIndex == 0)
      {
        continue;
      }
      //Debug.Log(i + " " + meshIndex + " " + vertexIndex);
      // Get a reference to the vertex color
      Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[meshIndex].colors32;

      Color32 c = unselectCol;
      //m_TextMeshPro.textInfo.characterInfo[i].underlineColor = c;

      vertexColors[vertexIndex + 0] = c;
      vertexColors[vertexIndex + 1] = c;
      vertexColors[vertexIndex + 2] = c;
      vertexColors[vertexIndex + 3] = c;

    }
    //for some reasons the first character in the text is selected as well, need to "fix" it
    //RestoreCachedVertexAttributes(0);
    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
  }

  void SelectCharacter(int i, Color selectCol)
  {
    int meshIndex = m_TextMeshPro.textInfo.characterInfo[i].materialReferenceIndex;

    int vertexIndex = m_TextMeshPro.textInfo.characterInfo[i].vertexIndex;
    if (i != 0 && vertexIndex == 0)
    {
      return;
    }
    //Debug.Log(i + " " + meshIndex + " " + vertexIndex);
    // Get a reference to the vertex color
    Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[meshIndex].colors32;

    Color32 c = selectCol;
    //m_TextMeshPro.textInfo.characterInfo[i].underlineColor = c;

    vertexColors[vertexIndex + 0] = c;
    vertexColors[vertexIndex + 1] = c;
    vertexColors[vertexIndex + 2] = c;
    vertexColors[vertexIndex + 3] = c;
    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
  }

  void DeselectLink(int linkIndex)
  {
    UnselectLink(linkIndex, linkColor);
    cachedLinkIndex = -1;
  }
  public void OnPointerEnter(PointerEventData eventData)
  {
    isHoveringObject = true;
  }


  public void OnPointerExit(PointerEventData eventData)
  {
    isHoveringObject = false;
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    //Debug.Log("OnPointerUp()");
  }


  void RestoreCachedVertexAttributes(int index)
  {
    if (index == -1 || index > m_TextMeshPro.textInfo.characterCount - 1) return;

    // Get the index of the material / sub text object used by this character.
    int materialIndex = m_TextMeshPro.textInfo.characterInfo[index].materialReferenceIndex;

    // Get the index of the first vertex of the selected character.
    int vertexIndex = m_TextMeshPro.textInfo.characterInfo[index].vertexIndex;

    // Restore Vertices
    // Get a reference to the cached / original vertices.
    Vector3[] src_vertices = m_cachedMeshInfoVertexData[materialIndex].vertices;

    // Get a reference to the vertices that we need to replace.
    Vector3[] dst_vertices = m_TextMeshPro.textInfo.meshInfo[materialIndex].vertices;

    // Restore / Copy vertices from source to destination
    dst_vertices[vertexIndex + 0] = src_vertices[vertexIndex + 0];
    dst_vertices[vertexIndex + 1] = src_vertices[vertexIndex + 1];
    dst_vertices[vertexIndex + 2] = src_vertices[vertexIndex + 2];
    dst_vertices[vertexIndex + 3] = src_vertices[vertexIndex + 3];

    // Restore Vertex Colors
    // Get a reference to the vertex colors we need to replace.
    Color32[] dst_colors = m_TextMeshPro.textInfo.meshInfo[materialIndex].colors32;

    // Get a reference to the cached / original vertex colors.
    Color32[] src_colors = m_cachedMeshInfoVertexData[materialIndex].colors32;

    // Copy the vertex colors from source to destination.
    dst_colors[vertexIndex + 0] = src_colors[vertexIndex + 0];
    dst_colors[vertexIndex + 1] = src_colors[vertexIndex + 1];
    dst_colors[vertexIndex + 2] = src_colors[vertexIndex + 2];
    dst_colors[vertexIndex + 3] = src_colors[vertexIndex + 3];

    // Restore UV0S
    // UVS0
    Vector2[] src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
    Vector2[] dst_uv0s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs0;
    dst_uv0s[vertexIndex + 0] = src_uv0s[vertexIndex + 0];
    dst_uv0s[vertexIndex + 1] = src_uv0s[vertexIndex + 1];
    dst_uv0s[vertexIndex + 2] = src_uv0s[vertexIndex + 2];
    dst_uv0s[vertexIndex + 3] = src_uv0s[vertexIndex + 3];

    // UVS2
    Vector2[] src_uv2s = m_cachedMeshInfoVertexData[materialIndex].uvs2;
    Vector2[] dst_uv2s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs2;
    dst_uv2s[vertexIndex + 0] = src_uv2s[vertexIndex + 0];
    dst_uv2s[vertexIndex + 1] = src_uv2s[vertexIndex + 1];
    dst_uv2s[vertexIndex + 2] = src_uv2s[vertexIndex + 2];
    dst_uv2s[vertexIndex + 3] = src_uv2s[vertexIndex + 3];


    // Restore last vertex attribute as we swapped it as well
    int lastIndex = (src_vertices.Length / 4 - 1) * 4;

    // Vertices
    dst_vertices[lastIndex + 0] = src_vertices[lastIndex + 0];
    dst_vertices[lastIndex + 1] = src_vertices[lastIndex + 1];
    dst_vertices[lastIndex + 2] = src_vertices[lastIndex + 2];
    dst_vertices[lastIndex + 3] = src_vertices[lastIndex + 3];

    // Vertex Colors
    src_colors = m_cachedMeshInfoVertexData[materialIndex].colors32;
    dst_colors = m_TextMeshPro.textInfo.meshInfo[materialIndex].colors32;
    dst_colors[lastIndex + 0] = src_colors[lastIndex + 0];
    dst_colors[lastIndex + 1] = src_colors[lastIndex + 1];
    dst_colors[lastIndex + 2] = src_colors[lastIndex + 2];
    dst_colors[lastIndex + 3] = src_colors[lastIndex + 3];

    // UVS0
    src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
    dst_uv0s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs0;
    dst_uv0s[lastIndex + 0] = src_uv0s[lastIndex + 0];
    dst_uv0s[lastIndex + 1] = src_uv0s[lastIndex + 1];
    dst_uv0s[lastIndex + 2] = src_uv0s[lastIndex + 2];
    dst_uv0s[lastIndex + 3] = src_uv0s[lastIndex + 3];

    // UVS2
    src_uv2s = m_cachedMeshInfoVertexData[materialIndex].uvs2;
    dst_uv2s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs2;
    dst_uv2s[lastIndex + 0] = src_uv2s[lastIndex + 0];
    dst_uv2s[lastIndex + 1] = src_uv2s[lastIndex + 1];
    dst_uv2s[lastIndex + 2] = src_uv2s[lastIndex + 2];
    dst_uv2s[lastIndex + 3] = src_uv2s[lastIndex + 3];

    // Need to update the appropriate 
    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
  }
}
