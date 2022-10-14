using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PlayableControllers
{
    /// <summary>
    /// ミックス担当するやつ
    /// </summary>
    internal class AnimationMixer : IDisposable
    {
        // Playables Api
        public AnimationClipPlayable prevPlayable;
        public AnimationClipPlayable currentPlayable;
        public AnimationMixerPlayable mixer;
        private PlayableGraph graph;
        private AnimInfo animInfo;
        
        // Animation Event補足用 代入するときは並び替え済みにしとく
        private List<AnimationEvent> currentClipAnimationEvent = new();
        private List<AnimationEvent> prevClipAnimationEvent = new();

        // コルーチン用
        private readonly PlayableController controller;
        private Coroutine transCoroutine;
        
        private readonly Queue<PlayAnimationInfo> animationQueue = new();
        private bool isPause;

        /// <summary>
        /// モーション再生が終了しているか
        /// </summary>
        public bool IsFinishedPlay
        {
            get
            {
                if (!currentPlayable.IsValid())
                    return true;

                // 予定されている再生時間を超えていれば、再生終了とみなす
                return currentPlayable.GetTime() > currentPlayable.GetAnimationClip().length;
            }
        }
        
        public AnimationMixer(PlayableGraph graph,PlayableController controller)
        {
            prevPlayable = AnimationClipPlayable.Create(graph, null);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            this.graph = graph;
            this.controller = controller;
        }

        public void Reconnect(PlayAnimationInfo info)
        {
            if(isPause) return;
            if(info.isOverride || IsFinishedPlay)
            {
                ReconnectCore(info);
            } else
            {
                // キューに追加
                animationQueue.Enqueue(info);
            }
        }

        private void ReconnectCore(PlayAnimationInfo info)
        {
            animInfo = info.AnimInfo;
            foreach(var e in animInfo.Clip.events)
            {
                e.messageOptions = SendMessageOptions.DontRequireReceiver;
            }
            
            // 上書きされたときはキューを空にする
            if(info.isOverride) animationQueue.Clear();
            
            // 切断
            graph.Disconnect(mixer,0);
            graph.Disconnect(mixer,1);
            if(prevPlayable.IsValid()) prevPlayable.Destroy();
            
            //更新
            prevPlayable = currentPlayable;
            prevClipAnimationEvent = new List<AnimationEvent>(currentClipAnimationEvent);
            currentPlayable = AnimationClipPlayable.Create(graph, info.AnimInfo.Clip);
            currentClipAnimationEvent = new List<AnimationEvent>(info.AnimInfo.Clip.events.OrderBy(e => e.time));
            
            //再接続
            mixer.ConnectInput(1, prevPlayable, 0);
            mixer.ConnectInput(0, currentPlayable, 0);
            
            currentPlayable.SetSpeed(1);
            
            // フェード中のコルーチンはキャンセルする
            if(transCoroutine is not null)
            {
                prevClipAnimationEvent.Clear();
                controller.StopCoroutine(transCoroutine);
            }

            transCoroutine = controller.StartCoroutine(CrossFadeAsync(info.duration));
        }

        IEnumerator CrossFadeAsync(float duration)
        {
            var waitTime = Time.time + duration;
            
            yield return new WaitWhile(() =>
            {
                if(isPause)
                {
                    waitTime += Time.deltaTime;
                    return true;
                }
                var diff = waitTime - Time.time;
                if (diff <= 0)
                {
                    mixer.SetInputWeight(1, 0);
                    mixer.SetInputWeight(0, 1);
                    return false;
                }
                else
                {
                    var rate = Mathf.Clamp01(diff / duration);
                    mixer.SetInputWeight(1, rate);
                    mixer.SetInputWeight(0, 1 - rate);
                    return true;
                }
            });
            
            prevClipAnimationEvent.Clear();
            transCoroutine = null;
        }

        public void FinishAnimation()
        {
            if(animationQueue.TryDequeue(out var info))
            {
                // アニメーションのキューを見る
                if(transCoroutine is not null) controller.StopCoroutine(transCoroutine);
                controller.Play(info);
            } else
            {
                // キューにないときだけループする
                if(animInfo.IsLoop)
                {
                    currentPlayable.SetTime(0f);
                    graph.Evaluate(0);
                }
            }
        }

        /// <summary>
        /// 補足できるアニメーションイベントを再生する
        /// TODO: 未完成。UniRXとかでイベント投げるか？
        /// </summary>
        public void TryAnimationEventFire()
        {
            static void Check(AnimationClipPlayable playable, IList<AnimationEvent> events)
            {
                var first = events.FirstOrDefault();
                if(first != null && playable.IsValid() && playable.GetTime() >= first.time)
                {
                    // TODO: いべんとなげーる？
                    events.RemoveAt(0);
                }
            }
            
            Check(prevPlayable,prevClipAnimationEvent);
            Check(currentPlayable,currentClipAnimationEvent);
        }

        /// <summary>
        /// まだデバッグでしか使わない方が良い
        /// </summary>
        public void SetCurrentEvaluate(float normalizedTime, bool isStop = false)
        {
            if(!currentPlayable.IsValid()) return;
            normalizedTime = Mathf.Clamp01(normalizedTime);
            if(isStop) currentPlayable.SetSpeed(0);
            var animTime = currentPlayable.GetAnimationClip().length;
            currentPlayable.SetTime(animTime * normalizedTime);
            graph.Evaluate(normalizedTime);
        }

        public void Pause()
        {
            if(isPause) return;
            isPause = true;
            if(prevPlayable.IsValid()) prevPlayable.Pause();
            if(currentPlayable.IsValid()) currentPlayable.Pause();
            if(mixer.IsValid()) mixer.Pause();
        }

        public void Resume()
        {
            if(!isPause) return;
            isPause = false;
            if(prevPlayable.IsValid()) prevPlayable.Play();
            if(currentPlayable.IsValid()) currentPlayable.Play();
            if(mixer.IsValid()) mixer.Play();
        }
        
        /// <summary>
        /// カレントの0~1時間を取得
        /// </summary>
        public bool TryGetCurrentNormalizedTime(out float normalizedTime)
        {
            normalizedTime = 0;
            if (!currentPlayable.IsValid())
                return false;
            normalizedTime = (float)currentPlayable.GetTime() / currentPlayable.GetAnimationClip().length;
            return true;
        }

        public void Dispose()
        {
            if(prevPlayable.IsValid()) prevPlayable.Destroy();
            if(currentPlayable.IsValid()) currentPlayable.Destroy();
            if(mixer.IsValid()) mixer.Destroy();
            animationQueue.Clear();
            transCoroutine = null;
        }
    }
}
