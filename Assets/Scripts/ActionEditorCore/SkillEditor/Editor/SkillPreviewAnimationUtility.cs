using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    internal static class SkillPreviewAnimationUtility
    {
        private static GameObject _activeInstance;
        private static AnimationClip _activeClip;

        public static void Sample(GameObject sceneInstance, AnimationClip clip, float time)
        {
            if (sceneInstance == null || clip == null)
            {
                return;
            }

            EnsureAnimationMode();
            _activeInstance = sceneInstance;
            _activeClip = clip;
            float clampedTime = Mathf.Clamp(time, 0f, clip.length);
            AnimationMode.SampleAnimationClip(sceneInstance, clip, clampedTime);
            SceneView.RepaintAll();
        }

        public static void Stop()
        {
            _activeInstance = null;
            _activeClip = null;
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            SceneView.RepaintAll();
        }

        public static bool HasActivePreview()
        {
            return _activeInstance != null && _activeClip != null && AnimationMode.InAnimationMode();
        }

        private static void EnsureAnimationMode()
        {
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }
        }
    }
}
