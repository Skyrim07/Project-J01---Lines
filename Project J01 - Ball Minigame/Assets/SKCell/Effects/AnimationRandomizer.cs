using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SKCell;


[RequireComponent(typeof(Animator))]
public sealed class AnimationRandomizer : MonoBehaviour
{
    public string clipName;
    private Animator anim;
    private void Start()
    {
        CommonUtils.InvokeAction(0.2f, () =>
        {
            anim = GetComponent<Animator>();
            anim.Play(clipName, 0, Random.Range(0f, 1f));
        });
    }
}
