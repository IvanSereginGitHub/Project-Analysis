using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxCapturer : MonoBehaviour
{
    public Camera cam;
    public RenderTexture rendTex, equirectTex;

    // Start is called before the first frame update
    void Start()
    { 
        Capture();
    }
    void Capture()
    {
        cam.RenderToCubemap(rendTex);
        rendTex.ConvertToEquirect(equirectTex);
        Save();
    }
[ContextMenu("Save")]
    void Save()
    {
        string path = Application.dataPath + "/cubemap.png";
        RenderTexture rendTex = equirectTex;
        Texture2D text = new Texture2D(equirectTex.width, equirectTex.height);
        RenderTexture.active = equirectTex;
        text.ReadPixels(new Rect(0,0,equirectTex.width,equirectTex.height), 0, 0);
        System.IO.File.WriteAllBytes(path, text.EncodeToPNG());
    }
}
