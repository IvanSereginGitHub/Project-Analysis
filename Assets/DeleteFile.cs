using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DeleteFile : MonoBehaviour
{
    public string filePath = "";
    public void Delete()
    {
        if (filePath != "")
        {
            Destroy(gameObject);
            File.Delete(filePath);
        }
    }
}
