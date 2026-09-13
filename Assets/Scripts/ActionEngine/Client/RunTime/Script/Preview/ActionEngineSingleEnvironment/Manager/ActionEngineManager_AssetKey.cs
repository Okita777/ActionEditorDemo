using System;
using AsiActionEngine.RunTime;
// using UnityEditor;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineManager_AssetKey
    {
        #region Instance
        private static ActionEngineManager_AssetKey mInstance = null;
        public static ActionEngineManager_AssetKey Instance
        {
            get
            {
                if (mInstance is null)
                {
                    mInstance = new ActionEngineManager_AssetKey();
                }
                return mInstance;
            }
        }
        #endregion

        private const string LoadName = "AssetKey";
        private ActionEngineProperty mActionEngineProperty = null;

        public void Load(Action<ActionEngineProperty> _actionEngineProperty)
        {
            if (mActionEngineProperty is not null)
            {
                _actionEngineProperty(mActionEngineProperty);
                return;
            }
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.AssetData, (_target) =>
            {
                if (_target is ActionEngineProperty _aep)
                {
                    mActionEngineProperty = _aep;
                    _actionEngineProperty(_aep);
                }
                else
                {
                    EngineDebug.LogError($"数据加载出错 [<color=#ff0000>{LoadName}</color>] ");
                    _actionEngineProperty(null);
                }
            }, 1);
        }
    }
}
