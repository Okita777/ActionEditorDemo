using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_InteractObjState : StaticActionLogics
    {
        public IInteractObject mInteractObject;
        public Transform mTransform;
        private IInteractObject mUpdateInteractObject;
        private Transform mUpdateTransform;
        public void UpdateData(IInteractObject _IInteractObject, Transform _Transform)
        {
            mUpdateInteractObject = _IInteractObject;
            mUpdateTransform = _Transform;
        }

        public void InitData()
        {
            mInteractObject = mUpdateInteractObject;
            mTransform = mUpdateTransform;

#if UNITY_EDITOR    
            if(mTransform == null)
            {
                //mInteractObject = null;
                EngineDebug.LogWarning($"<color=#ff0000>交互对象已不存在</color>");
            }
            else
            {
                EngineDebug.LogWarning($"交互对象 [<color=#ffcc00>{mTransform.gameObject.name}</color>] 缓存成功");
            }
#endif
        }

        public void InteractCallBack(ActionStateMachine _machine)
        {
            if(mInteractObject == null)
            {
                EngineDebug.LogError("交互回调报错");
            }
            else
            {
                EngineDebug.LogWarning($"当前交互对象 [<color=#ffcc00>{mTransform.gameObject.name}</color>] [{mTransform.GetSiblingIndex()}]");
                mInteractObject.OnTrigger(_machine);
            }
        }
    }
}