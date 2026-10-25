using System;
using System.Collections;
using UnityEngine;

namespace JackGame
{
    // Reusable interpolation primitives. All timing is unscaled and Web-compatible.
    public static class StoryMotion
    {
        public static float Ease(float t) => t*t*(3-2*t);
        public static IEnumerator Tween(float duration, Action<float> sample)
        {
            sample(0);
            for(float elapsed=0; elapsed<duration; elapsed+=Time.unscaledDeltaTime) { sample(Ease(Mathf.Clamp01(elapsed/Mathf.Max(.001f,duration)))); yield return null; }
            sample(1);
        }
        public static IEnumerator Move(Transform target, Vector3 end, float duration)
        { var start=target.position; return Tween(duration,t=> { if(target) target.position=Vector3.LerpUnclamped(start,end,t); }); }
        public static IEnumerator Scale(Transform target, Vector3 end, float duration)
        { var start=target.localScale; return Tween(duration,t=> { if(target) target.localScale=Vector3.LerpUnclamped(start,end,t); }); }
    }
}
