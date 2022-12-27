using System;
using UnityEditor;
using UnityEngine;

namespace PlayableControllers.Samples
{
    public class PlayableControllerSample : MonoBehaviour
    {
        
        [SerializeField] private AnimInfo[] info;
        [SerializeField] private int currentIndex;
        [SerializeField] private float duration;
        [SerializeField] private bool isOverride = true;
        [SerializeField] private bool playOnAwake;
        [SerializeField, Range(0, 2)]  private float speed = 1;
        
        private PlayableController controller;
        private float prevSpeed = 1;

        private void Start()
        {
            controller = GetComponent<PlayableController>();
            if(playOnAwake) Play();
        }

        private void Update()
        {
            if(prevSpeed != speed)
            {
                controller.SetTimeScale(speed);
                prevSpeed = speed;
            }
            
            if(Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentIndex++;
                if(currentIndex >= info.Length) currentIndex = 0;
            } else if(Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentIndex--;
                if(currentIndex < 0) currentIndex = info.Length - 1;
            }else if(Input.GetKeyDown(KeyCode.P))
            {
                controller.Pause();
                return;
            }
            else if(Input.GetKeyDown(KeyCode.R))
            {
                controller.Resume();
                return;
            }
            else
            {
                return;
            }
            
            Play();
        }
        
        [ContextMenu("Play!")]
        void Play()
        {
            controller.Play(info[currentIndex],duration,false,0,isOverride);
        }
        
#if UNITY_EDITOR
       
        [CustomEditor(typeof(PlayableControllerSample))]
        public class PlayableControllerPlayerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                var creator = target as PlayableControllerSample;
                EditorGUILayout.Space();

                if (GUILayout.Button("Play"))
                {
                    creator?.Play();
                }
            }
        }
#endif
    }
}
