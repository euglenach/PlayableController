using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PlayableControllers
{
    [DefaultExecutionOrder(-10)]
    public class PlayableController : MonoBehaviour
    {
        /// <summary>
        /// ブレンドするアバターマスク(なくてもいい)
        /// </summary>
        [SerializeField] private AvatarMask avatarMask;

        [SerializeField] private bool initializeOnAwake;

        // Playables Api
        private PlayableGraph graph;
        private AnimationPlayableOutput output;
        private AnimationLayerMixerPlayable layerMixer;
        
        // レイヤーごとのミキサー
        private AnimationMixer[] mixers;
        
        private Animator animator;

        private bool isFreeze;

        private bool isInitialized;

        private void Awake()
        {
            if(initializeOnAwake) Initialize();
        }

        public void Initialize()
        {
            //todo: [sf] にしても良いかも？
            animator = GetComponentInChildren<Animator>();

            animator.runtimeAnimatorController = null;
            
            graph = PlayableGraph.Create();

            mixers = new AnimationMixer[2];
            mixers[0] = new AnimationMixer(graph,this);
            mixers[1] = new AnimationMixer(graph,this);
            
            layerMixer = AnimationLayerMixerPlayable.Create(graph,2);
            if (avatarMask != null)
                layerMixer.SetLayerMaskFromAvatarMask(1, avatarMask);
            
            output = AnimationPlayableOutput.Create(graph, "output", animator);
            
            graph.Play();
            isInitialized = true;
        }

        public void Play(PlayAnimationInfo info)
        {
            if(!isInitialized) return;
            if(isFreeze) return;
            if(info.AnimInfo.Clip == null) return;
            
            var mixer = mixers[info.layer];

            // 同じアニメーションがきたらキャンセルする
            if(info.isSameAnimationCancel && mixer.currentPlayable.IsValid() && mixer.currentPlayable.GetAnimationClip() == info.AnimInfo.Clip)
                return;

            // 切断
            graph.Disconnect(layerMixer, info.layer);
            
            // 再接続
            mixer.Reconnect(info);
            layerMixer.ConnectInput(info.layer,mixer.mixer,0,1);
            
            // 出力
            output.SetSourcePlayable(layerMixer);
        }

        public void Play(AnimInfo animInfo, float duration = 0, bool isSameAnimationCancel = true, int layer = 0, bool isOverride = true)
        {
            Play(new PlayAnimationInfo(animInfo,duration,isSameAnimationCancel,layer,isOverride));
        }
        
        public void Play(AnimationClip clip, bool isLoop = false, float duration = 0, bool isSameAnimationCancel = true, int layer = 0, bool isOverride = true)
        {
            Play(new PlayAnimationInfo(clip,isLoop,duration,isSameAnimationCancel,layer,isOverride));
        }

        /// <summary>
        /// アニメーションを止める
        /// </summary>
        public void Pause(int layer = 0)
        {
            if(!isInitialized) return;
            if(isFreeze) return;
            mixers[layer].Pause();
        }

        /// <summary>
        /// ポーズを解除する
        /// </summary>
        public void Resume(int layer = 0)
        {
            if(!isInitialized) return;
            if(isFreeze) return;
            mixers[layer].Resume();
        }

        /// <summary>
        /// 入力を遮断する
        /// </summary>
        public void Freeze()
        {
            isFreeze = true;
        }

        /// <summary>
        /// フリーズの解除
        /// </summary>
        public void UnFreeze()
        {
            isFreeze = false;
        }
        
        /// <summary>
        /// 再生中のクリップ名を取得する
        /// </summary>
        public string NowPlayClipName(int layer = 0)
        {
            if(!isInitialized) return string.Empty;
            return mixers[layer].currentPlayable.GetAnimationClip().name;
        }
        
        /// <summary>
        /// 再生中のクリップを取得する
        /// </summary>
        public AnimationClip NowPlayClip(int layer = 0)
        {
            if(!isInitialized) return default;
            return mixers[layer].currentPlayable.GetAnimationClip();
        }

        /// <summary>
        /// 現在のアニメーションの0～1時間
        /// </summary>
        public float GetCurrentNormalizedTime(int layer = 0)
        {
            if(!isInitialized) return default;
            if(mixers[layer].TryGetCurrentNormalizedTime(out var t))
            {
                return t;
            }

            return 0;
        }

        /// <summary>
        /// 指定したアニメーションクリップの0～1時間
        /// </summary>
        public bool TryGetNormalizedTime(AnimationClip clip, out float normalizedTime, int layer = 0)
        {
            normalizedTime = 0;
            if(!isInitialized) return default;
            var playable = GetPlayableWithClip(clip, layer);
            if(!playable.IsValid()) return default;
            
            normalizedTime = (float)playable.GetTime() / playable.GetAnimationClip().length;
            return true;
        }

        /// <summary>
        /// 現在のアニメーションの再生時間
        /// </summary>
        public float GetCurrentTime(int layer = 0)
        {
            if(!isInitialized) return default;
            return (float)mixers[layer].currentPlayable.GetTime();
        }

        /// <summary>
        /// 指定したアニメーションクリップの再生時間
        /// </summary>
        public bool TryGetTime(AnimationClip clip, out float normalizedTime, int layer = 0)
        {
            normalizedTime = 0;
            if(!isInitialized) return default;
            var playable = GetPlayableWithClip(clip, layer);
            if(!playable.IsValid()) return default;
            
            normalizedTime = (float)playable.GetTime();
            return true;
        }
        
        /// <summary>
        /// 再生が終了しているか
        /// </summary>
        public bool IsFinished(int layer = 0)
        {
            if(!isInitialized) return default;
            return mixers[layer].IsFinishedPlay;
        }

        private Coroutine CrossFadeLayerWeightCoroutine;
        
        /// <summary>
        /// レイヤーの上書き設定
        /// </summary>
        public void SetLayerEnabled(float duration, bool enable)
        {
            if(!isInitialized) return;
            CrossFadeLayerWeightCoroutine = StartCoroutine(CrossFadeLayerWeightCore(1, duration, enable));
        }

        private IEnumerator CrossFadeLayerWeightCore(int layer, float duration, bool enable)
        {
            if(isFreeze) yield break;

            if (enable && layerMixer.GetInputWeight(layer) >= 1f) yield break;
            if (!enable && layerMixer.GetInputWeight(layer) <= 0f) yield break;
            
            if(CrossFadeLayerWeightCoroutine is not null) StopCoroutine(CrossFadeLayerWeightCoroutine);
            
            // 指定時間でアニメーションをブレンド
            var waitTime = Time.time + duration;
            yield return new WaitWhile(() =>
            {
                var diff = waitTime - Time.time;
                var rate = Mathf.Clamp01(diff / duration);
                var weight = enable ? 1 - rate : rate;
                layerMixer.SetInputWeight(layer, weight);

                return diff > 0;
            });

            CrossFadeLayerWeightCoroutine = null;
        }
        
        private AnimationClipPlayable GetPlayableWithClip(AnimationClip clip, int layer = 0)
        {
            var mixer = mixers[layer];
            var current = mixer.currentPlayable;
            var prev = mixer.prevPlayable;
            return current.GetAnimationClip() == clip? current : prev.GetAnimationClip() == clip? prev : default;
        }

        private void Update()
        {
            if(!isInitialized) return;
            if(isFreeze) return;
            // アニメーションの再生終了を監視する
            foreach(var mixer in mixers)
            {
                if(mixer.currentPlayable.IsValid() && mixer.IsFinishedPlay)
                    mixer.FinishAnimation();
            }
        }

        private void OnDestroy()
        {
            graph.Destroy();
            if(layerMixer.IsValid())layerMixer.Destroy();
            foreach(var mixer in mixers)
            {
                mixer.Dispose();
            }
        }
    }
}
