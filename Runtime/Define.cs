using System;
using UnityEngine;

namespace PlayableControllers
{
    [Serializable]
    public struct AnimInfo
    {
        public AnimationClip Clip;
        public bool IsLoop;
        public float Speed;

        public AnimInfo(AnimationClip clip, bool isLoop = false, float speed = 1f)
        {
            Clip = clip;
            IsLoop = isLoop;
            Speed = speed;
        }
    }

    public readonly struct PlayAnimationInfo
    {
        public readonly AnimInfo AnimInfo;
        public readonly float duration;
        public readonly int layer;
        /// <summary>
        /// キューに追加しないですぐに再生する
        /// </summary>
        public readonly bool isOverride;
        /// <summary>
        /// 現在再生中のクリップと同じクリップがきたら何もしない
        /// </summary>
        public readonly bool isSameAnimationCancel;

        public PlayAnimationInfo(AnimInfo animInfo, float duration = 0, bool isSameAnimationCancel = true, int layer = 0, bool isOverride = true)
        {
            this.AnimInfo = animInfo;
            this.duration = duration;
            this.isSameAnimationCancel = isSameAnimationCancel;
            this.layer = layer;
            this.isOverride = isOverride;
        }
        
        public PlayAnimationInfo(AnimationClip clip, bool isLoop = false, float duration = 0, bool isSameAnimationCancel = true, int layer = 0, bool isOverride = true)
        {
            this.AnimInfo = new AnimInfo(clip,isLoop);
            this.duration = duration;
            this.isSameAnimationCancel = isSameAnimationCancel;
            this.layer = layer;
            this.isOverride = isOverride;
        }
    }
}
