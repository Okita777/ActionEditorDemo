using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public class ActionEngineManager_EditorTempData
    {
        private static ActionEngineManager_EditorTempData _instance = null;

        public static ActionEngineManager_EditorTempData Instance
        {
            get
            {
                if (_instance is null) _instance = new ActionEngineManager_EditorTempData();
                return _instance;
            }
        }

        public int GetLayerMask(GameObject gameObject, int layerMask)
        {
            if (layerMask < 0)
            {
                if (gameObject.TryGetComponent(out ActionEngine_LayerMask layerMaskComponent))
                {
                    return layerMaskComponent.mLayers[-layerMask - 1];
                }
            }

            return layerMask;
        }
    }
}