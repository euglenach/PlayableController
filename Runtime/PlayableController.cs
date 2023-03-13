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
            SetLayerWeightCore(0, 1);
            
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

            // レイヤーのWeightを持っておく
            var layerWeight = layerMixer.GetInputWeight(info.layer);
            
            // 切断
            graph.Disconnect(layerMixer, info.layer);
            
            // 再接続
            mixer.Reconnect(info);
            layerMixer.ConnectInput(info.layer, mixer.mixer, 0, layerWeight);
            SetLayerWeightCore(info.layer, layerWeight);
            
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
        
        public void SetTimeScale(float timeScale, int layer = 0)
        {
            if (!isInitialized) return;
            mixers[layer].TimeScale = timeScale;
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
        
        public float GetLayerWeight(int layer)
        {
            return layerMixer.GetInputWeight(layer);
        }

        /// <summary>
        /// レイヤーの重み設定
        /// </summary>
        public void SetLayerWeight(int layer, float weight)
        {
            if(!isInitialized) return;
            SetLayerWeightCore(layer, weight);
        }
        
        /// <summary>
        /// レイヤーの重み設定
        /// </summary>
        public void SetLayerWeight(int layer, float weight, float duration)
        {
            if(!isInitialized) return;
            CrossFadeLayerWeightCoroutine = StartCoroutine(CrossFadeLayerWeightCore(layer, weight, duration));
        }

        private IEnumerator CrossFadeLayerWeightCore(int layer, float weight, float duration)
        {
            if(isFreeze) yield break;
            
            if(CrossFadeLayerWeightCoroutine is not null) StopCoroutine(CrossFadeLayerWeightCoroutine);

            var time = 0f;
            
            // 指定時間でレイヤーをブレンド
            yield return new WaitWhile(() =>
            {
                if(isFreeze) return true;
                if (duration <= time)
                {
                    SetLayerWeightCore(layer, weight);
                    return false;
                }
                else
                {
                    var rate = Mathf.Clamp01(time / duration);
                    SetLayerWeightCore(layer, rate * weight);
                    time += Time.deltaTime;
                    return true;
                }
            });

            CrossFadeLayerWeightCoroutine = null;
        }

        void SetLayerWeightCore(int layer, float weight)
        {
            var other = layer == 0 ? 1 : 0;
            layerMixer.SetInputWeight(layer, weight);
            layerMixer.SetInputWeight(other, 1 - weight);
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
            
            foreach(var mixer in mixers)
            {
                
                mixer.TryAnimationEventFire();
                
                // アニメーションの再生終了を監視する
                if(mixer.currentPlayable.IsValid() && mixer.IsFinishedPlay)
                    mixer.FinishAnimation();
            }
        }

        private void OnDestroy()
        {
            if(graph.IsValid()) graph.Destroy();
            if(layerMixer.IsValid())layerMixer.Destroy();
            foreach(var mixer in mixers)
            {
                mixer.Dispose();
            }
        }
    }
}
