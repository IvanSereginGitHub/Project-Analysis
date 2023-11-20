using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static EasingFunction;
using static Conversions;
using System.Reflection;
using UnityEngine.Events;

[Serializable]
public class TimedVarPair<T>
{
  public GameObject obj;
  public TimedVar<T> timedVar;
  public object componentOrigin;
  public dynamic componentReference;
  public T autoStartValue;

  public TimedVarPair(GameObject obj, TimedVar<T> colVar)
  {
    this.obj = obj;
    this.timedVar = colVar;
  }

  public void ExecuteVariable(float time)
  {
    timedVar.Execute(time);
  }
}
public interface ITimedVariable
{

  public float Time { get; set; }

  public float StartDelay { get; set; }

  public EasingFunction.Ease EasingType { get; set; }

  public Vector2 LoopDelayPair { get; set; }

  public bool UseAutoStartVal { get; set; }

  public bool UseRandom { get; set; }

  public bool UseRandomSeed { get; set; }

  public bool UseRandomAtStart { get; set; }

  public int RandomSeed { get; set; }
  public void Execute(float time);
  public void Reset();
  public void InitializeRandom();
}

//An universal type for any simple variable present in the game. Completely obsoletes (well, not really, just supersimplifies) most of the triggers.
[Serializable]
public class TimedVar<T> : ITimedVariable
{
  public enum ValueChangeType
  {
    Local,
    Global
  }
  [field: SerializeField]
  public T StartValue { get; set; }
  [field: SerializeField]
  public T EndValue { get; set; }
  [field: SerializeField]
  public float Time { get; set; } = 0;
  [field: SerializeField]
  public float StartDelay { get; set; } = 0;
  [field: SerializeField]
  public Ease EasingType { get; set; } = Ease.None;
  [field: SerializeField]
  public bool UseRandom { get; set; }
  [field: SerializeField]
  public bool UseRandomSeed { get; set; }
  [field: SerializeField]
  public bool UseRandomAtStart { get; set; }
  [field: SerializeField]
  public int RandomSeed { get; set; }
  [field: SerializeField]
  public T MinRandomValue { get; set; }
  [field: SerializeField]
  public T MaxRandomValue { get; set; }
  [field: SerializeField]
  public Vector2 LoopDelayPair { get; set; } = Vector2.zero;
  [field: SerializeField]
  public bool UseAutoStartVal { get; set; }

  public bool SameValueForAllAxis = false;
  public ValueChangeType valueChangeType = ValueChangeType.Local;
  bool finishedExecution = false;
  public bool hasStarted = false;
  public System.Random rand;
  public UnityEvent eventWhenStarted = new UnityEvent();

  public void InitializeRandom()
  {
    rand = (UseRandom && !UseRandomSeed) ? new System.Random(RandomSeed) : new System.Random();
  }

  public TimedVar()
  {
    InitializeRandom();
  }
  public TimedVar(T endValue)
  {
    EndValue = endValue;
    InitializeRandom();
  }
  public TimedVar(T startVal, T endVal, float time, float startDelay, EasingFunction.Ease easingType, bool useRandom, bool useRandomSeed, bool useRandomAtStart, int randomSeed, T minVal, T maxVal, Vector2 loopDelayPair, bool useAutoVal)
  {
    this.StartValue = startVal;
    this.EndValue = endVal;
    this.Time = time;
    this.StartDelay = startDelay;
    this.EasingType = easingType;
    this.UseRandom = useRandom;
    this.UseRandomSeed = useRandomSeed;
    this.UseRandomAtStart = useRandomAtStart;
    this.RandomSeed = randomSeed;
    this.MinRandomValue = minVal;
    this.MaxRandomValue = maxVal;
    this.LoopDelayPair = loopDelayPair;
    this.UseAutoStartVal = useAutoVal;

    InitializeRandom();
  }
  public TimedVar(string startVal, string endVal, float time, float startDelay, EasingFunction.Ease easingType, bool useRandom, bool useRandomSeed, bool useRandomAtStart, int randomSeed, string minVal, string maxVal, Vector2 loopDelayPair, bool useAutoVal)
  {
    try
    {
      this.StartValue = FromStringToVar<T>(startVal);
      this.EndValue = FromStringToVar<T>(endVal);
      this.MinRandomValue = FromStringToVar<T>(minVal);
      this.MaxRandomValue = FromStringToVar<T>(maxVal);
    }
    catch (Exception ex)
    {
      Debug.LogError(ex.Message);
    }
    this.Time = time;
    this.StartDelay = startDelay;
    this.EasingType = easingType;
    this.UseRandom = useRandom;
    this.UseRandomSeed = useRandomSeed;
    this.UseRandomAtStart = useRandomAtStart;
    this.RandomSeed = randomSeed;
    this.LoopDelayPair = loopDelayPair;
    this.UseAutoStartVal = useAutoVal;

    InitializeRandom();
  }
  public TimedVar(TimedVar<T> timedVar)
  {
    this.StartValue = timedVar.StartValue;
    this.EndValue = timedVar.EndValue;
    this.Time = timedVar.Time;
    this.StartDelay = timedVar.StartDelay;
    this.EasingType = timedVar.EasingType;
    this.UseRandom = timedVar.UseRandom;
    this.UseRandomSeed = timedVar.UseRandomSeed;
    this.UseRandomAtStart = timedVar.UseRandomAtStart;
    this.RandomSeed = timedVar.RandomSeed;
    this.MinRandomValue = timedVar.MinRandomValue;
    this.MaxRandomValue = timedVar.MaxRandomValue;
    this.LoopDelayPair = timedVar.LoopDelayPair;
    this.UseAutoStartVal = timedVar.UseAutoStartVal;

    InitializeRandom();
  }
  float _Timer = 0;
  float _StartDelayTimer = 0;
  float _LoopDelayTimer = 0;
  float LocalTimer
  {
    get
    {
      return Mathf.Clamp(_Timer, 0, Time);
    }
    set
    {
      _Timer = value;
    }
  }
  public float GetLocalTimer()
  {
    return LocalTimer;
  }
  float LocalStartDelayTimer
  {
    get
    {
      return Mathf.Clamp(_StartDelayTimer, 0, StartDelay);
    }
    set
    {
      _StartDelayTimer = value;
    }
  }
  float LocalLoopDelayTimer
  {
    get
    {
      return Mathf.Clamp(_LoopDelayTimer, 0, LoopDelayPair.y);
    }
    set
    {
      _LoopDelayTimer = value;
    }
  }
  int LocalLoopCounter { get; set; }
  public T CurrentVal;
  public T PreviousVal;

  public void ApplyValueTo(ref T var, float timeScale)
  {
    if (!finishedExecution)
    {
      Execute(timeScale);
      var = CurrentVal;
    }
  }
  //Instaed of var ... = *nameOfTimedVar*.CurrentVal;
  public static implicit operator T(TimedVar<T> t)
  {
    return t.CurrentVal;
  }

  public static implicit operator bool(TimedVar<T> t)
  {
    return t.finishedExecution;
  }

  public void Execute(float deltaTime)
  {
    //Incase of weird post-finish execution stuff, this shows that function will fire, but will be immediately terminated
    //This bool could be replaced with a regular trigger removal later, added it for a test
    if (finishedExecution)
      return;

    LocalStartDelayTimer = _StartDelayTimer + deltaTime;
    if (LocalStartDelayTimer < StartDelay)
      return;

    LocalLoopDelayTimer = _LoopDelayTimer + deltaTime;
    if (LocalLoopCounter != 0 && LocalLoopDelayTimer < LoopDelayPair.y)
      return;
    if (!hasStarted)
    {
      hasStarted = true;
      eventWhenStarted.Invoke();
      if (UseRandomAtStart)
      {
        EndValue = (T)NewRandomGeneric(this);
      }
    }
    PreviousVal = CurrentVal;
    if (LocalTimer == 0)
    {
      PreviousVal = StartValue;
    }
    LocalTimer = _Timer + deltaTime;
    CurrentVal = (T)FromGenericToConvert(this);
    //this could affect performance at some point, might optimize it a bit

    if (LocalTimer >= Time)
    {
      if (LocalLoopCounter < LoopDelayPair.x)
      {
        hasStarted = false;
        LocalLoopCounter++;
        LocalLoopDelayTimer = 0;
        LocalTimer = 0;
        if (UseRandom)
        {
          EndValue = (T)NewRandomGeneric(this);
        }
        return;
      }
      finishedExecution = true;
    }
  }

  public void Reset()
  {
    finishedExecution = false;
    LocalTimer = 0;
    LocalStartDelayTimer = 0;
    LocalLoopDelayTimer = 0;
    LocalLoopCounter = 0;
    InitializeRandom();
  }

  public override string ToString()
  {
    string result = "|";
    PropertyInfo[] info = this.GetType().GetProperties();
    for (int i = 0; i < info.Length; i++)
    {
      result += $"{info[i].Name}:{GetType().GetProperty(info[i].Name).GetValue(this)}{(i == info.Length - 1 ? "" : ";")}";
    }
    result += "|";
    return result;
  }
}
