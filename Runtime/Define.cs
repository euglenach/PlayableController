using System;
using UnityEngine;

namespace PlayableControllers
{
    [Serializable]
    public struct AnimInfo
    {
        public AnimationClip Clip;
        public bool IsLoop;

        public AnimInfo(AnimationClip clip, bool isLoop = false)
        {
            Clip = clip;
            IsLoop = isLoop;
        }
    }

    public readonly struct PlayAnimationInfo
    {
        public readonly AnimInfo AnimInfo;
        public readonly float duration;
        public readonly int layer;
        public readonly bool isOverride;

        public PlayAnimationInfo(AnimInfo animInfo, float duration = 0, int layer = 0, bool isOverride = true)
        {
            this.AnimInfo = animInfo;
            this.duration = duration;
            this.layer = layer;
            this.isOverride = isOverride;
        }
        
        public PlayAnimationInfo(AnimationClip clip, bool isLoop = false, float duration = 0, int layer = 0, bool isOverride = true)
        {
            this.AnimInfo = new AnimInfo(clip,isLoop);
            this.duration = duration;
            this.layer = layer;
            this.isOverride = isOverride;
        }
    }
}
