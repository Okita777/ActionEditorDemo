using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public abstract class ActionEngine_External : MonoBehaviour
    {
        public abstract void SetAfterImageConfig(float interval, float duration, float power, int maxCount, 
            Gradient gradient, AnimationCurve intensity);
        public abstract void RegisterAfterImage(Renderer render);
        public abstract void StartAfterImage();
        public abstract void UpdateAfterImage(float fTick);
        public abstract void StopAfterImage();
    }
}
