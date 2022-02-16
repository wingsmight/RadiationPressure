using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnimatorExt
{
    static public string GetCurrentName(this Animator animator, int layer = 0)
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (info.IsName(clip.name))
                return clip.name;
        }

        return null;
    }
}
