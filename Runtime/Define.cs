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
        
        /// <summary>
        /// アニメーションが終わったら自動で消す
        /// </summary>
        public readonly bool AutoDestroy;

        public PlayAnimationInfo(AnimInfo animInfo, float duration = 0, bool isSameAnimationCancel = true, int layer = 0, bool isOverride = true, bool autoDestroy = false)
        {
            this.AnimInfo = animInfo;
            this.duration = duration;
            this.isSameAnimationCancel = isSameAnimationCancel;
            this.layer = layer;
            this.isOverride = isOverride;
            this.AutoDestroy = autoDestroy;
        }
        
        public PlayAnimationInfo(AnimationClip clip, bool isLoop = false, float duration = 0, bool isSameAnimationCancel = true, int layer = 0, bool isOverride = true, bool autoDestroy = false, float speed = 1f)
        {
            this.AnimInfo = new AnimInfo(clip ,isLoop ,speed);
            this.duration = duration;
            this.isSameAnimationCancel = isSameAnimationCancel;
            this.layer = layer;
            this.isOverride = isOverride;
            this.AutoDestroy = autoDestroy;
        }
    }
}
