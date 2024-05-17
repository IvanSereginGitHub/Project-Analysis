using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Debug : UnityEngine.Debug
{
  private static ILogger _logger = Debug.unityLogger;
  ///<summary>
  ///Controls what is the separator symbol for objects when calling Debug.Log
  ///</summary>
  public static string separator = " ";
  [HideInCallstack]
  public static void LogObjects(params object[] objects)
  {
    UnityEngine.Debug.Log(string.Join(separator, objects));
  }
  [HideInCallstack]
  public static void LogArray(object[] objects)
  {
    UnityEngine.Debug.Log($"[{string.Join(separator, objects)}]");
  }
  [HideInCallstack]
  public static void LogArray<T>(T[] objects)
  {
    UnityEngine.Debug.Log($"(Length: {objects.Length}) [{string.Join(separator, objects)}]");
  }
  [HideInCallstack]
  public static void LogList<T>(IEnumerable<T> list)
  {
    UnityEngine.Debug.Log($"[{string.Join(separator, list)}] ({list.Count()})");
  }
  public static string ListToString<T>(IEnumerable<T> list)
  {
    return $"[{string.Join(separator, list)}] ({list.Count()})";
  }
  public static string ArrayToString<T>(T[] array)
  {
    return $"[{string.Join(separator, array)}] ({array.Length})";
  }
  [HideInCallstack]
  public static void LogDictionary<K, V>(Dictionary<K, V> dictionary)
  {
    string res = "";
    foreach (KeyValuePair<K, V> pair in dictionary)
    {
      res += $"{pair.Key}: '{pair.Value}'\n";
    }
    UnityEngine.Debug.Log(res);
  }
}