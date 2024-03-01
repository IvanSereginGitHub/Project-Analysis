using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleCardRandomFades : MonoBehaviour
{
    Animator anim;
    public List<string> stateNames;
    public int animationsCount;
    int randomFadeIn;
    int randomFadeOut;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        FadeIn();
    }

    public void FadeIn()
    {
        randomFadeIn = UnityEngine.Random.Range(0, animationsCount);
        anim.SetFloat("Speed", 1);
        Debug.Log(stateNames[randomFadeIn]);
        anim.Play(stateNames[randomFadeIn], 0, 0);
        StartCoroutine(ExecuteAfterTime(5f, () => { FadeOut(); }));
    }

    public void FadeOut()
    {
        randomFadeOut = UnityEngine.Random.Range(0, animationsCount);
        anim.SetFloat("Speed", -1);
        Debug.Log(stateNames[randomFadeOut]);
        anim.Play(stateNames[randomFadeOut], 0, 1);
    }

    IEnumerator ExecuteAfterTime(float time, Action task)
    {
        yield return new WaitForSeconds(time);
        task();
    }
}
