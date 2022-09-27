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
        
        // Playables Api
        private PlayableGraph graph;
        private AnimationPlayableOutput output;
        private AnimationLayerMixerPlayable layerMixer;
        
        // レイヤーごとのミキサー
        private AnimationMixer[] mixers;
        
        private Animator animator;

        private bool isFreeze;

        private void Awake()
        {
            // AnimatorがGetComponentで見つからなかったらInChildrenで子供に問い合わせ
            //todo: [sf] にしても良いかも？
            animator = GetComponent<Animator>();
            animator ??= GetComponentInChildren<Animator>();
            
            graph = PlayableGraph.Create();

            mixers = new AnimationMixer[2];
            mixers[0] = new AnimationMixer(graph,this);
            mixers[1] = new AnimationMixer(graph,this);
            
            layerMixer = AnimationLayerMixerPlayable.Create(graph,2);
            if (avatarMask != null)
                layerMixer.SetLayerMaskFromAvatarMask(1, avatarMask);
            
            output = AnimationPlayableOutput.Create(graph, "output", animator);
            
            graph.Play();
        }

        public void Play(PlayAnimationInfo info)
        {
            if(isFreeze) return;
            var mixer = mixers[info.layer];
            
            // 切断
            graph.Disconnect(layerMixer, info.layer);
            
            // 再接続
            mixer.Reconnect(info);
            layerMixer.ConnectInput(info.layer,mixer.mixer,0,1);
            
            // 出力
            output.SetSourcePlayable(layerMixer);
        }

        public void Play(AnimInfo animInfo, float duration = 0, int layer = 0, bool isOverride = true)
        {
            Play(new PlayAnimationInfo(animInfo,duration,layer,isOverride));
        }
        
        public void Play(AnimationClip clip, bool isLoop = false, float duration = 0, int layer = 0, bool isOverride = true)
        {
            Play(new PlayAnimationInfo(clip,isLoop,duration,layer,isOverride));
        }

        /// <summary>
        /// アニメーションを止める
        /// </summary>
        public void Pause(int layer = 0)
        {
            if(isFreeze) return;
            mixers[layer].Pause();
        }

        /// <summary>
        /// ポーズを解除する
        /// </summary>
        public void Resume(int layer = 0)
        {
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
            return mixers[layer].currentPlayable.GetAnimationClip().name;
        }
        
        /// <summary>
        /// 再生が終了しているか
        /// </summary>
        public bool IsFinished(int layer = 0)
        {
            return mixers[layer].IsFinishedPlay;
        }

        private Coroutine CrossFadeLayerWeightCoroutine;
        
        /// <summary>
        /// レイヤーの上書き設定
        /// </summary>
        public void SetLayerEnabled(float duration, bool enable)
        {
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

        private void Update()
        {
            if(isFreeze) return;
            // アニメーションの再生終了を監視する
            foreach(var mixer in mixers.Where(mixer => mixer.IsFinishedPlay))
            {
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
