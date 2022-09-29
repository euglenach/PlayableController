using System;
using UnityEngine;

namespace PlayableControllers.Samples
{
    public class PlayableControllerSample : MonoBehaviour
    {
        
        [SerializeField] private AnimInfo[] info;
        [SerializeField] private float duration;
        [SerializeField] private bool isOverride;
        [SerializeField] private bool playOnAwake;
        [SerializeField] private int currentIndex;
        
        private PlayableController controller;

        private void Start()
        {
            controller = GetComponent<PlayableController>();
            if(playOnAwake) Play();
        }

        private void Update()
        {
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
            controller.Play(info[currentIndex],duration,true,0,isOverride);
        }
    }
}
