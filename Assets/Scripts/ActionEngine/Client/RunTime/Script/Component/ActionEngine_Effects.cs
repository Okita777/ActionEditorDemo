using System.Collections.Generic;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_Effects : MonoBehaviour
    {
        public List<ParticleSystem> particleSystems = new List<ParticleSystem>();
        public float Life = 2;

        public void Play()
        {
            gameObject.SetActive(true);
            foreach (ParticleSystem VARIABLE in particleSystems)
            {
                VARIABLE.Play();
            }
        }
        public void Stop()
        {
            foreach (ParticleSystem VARIABLE in particleSystems)
            {
                VARIABLE.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public bool useAutoRandomSeed
        {
            get { return false; }
            set
            {
                foreach (var VARIABLE in transform.GetComponentsInChildren<ParticleSystem>())
                {
                    VARIABLE.useAutoRandomSeed = value;
                }
            }
        }
    }
}