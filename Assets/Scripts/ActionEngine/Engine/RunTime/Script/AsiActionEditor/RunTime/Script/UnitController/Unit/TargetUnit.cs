using UnityEngine;

namespace AsiActionEngine.RunTime
{
    //单位获取
    public abstract class TargetUnit : MonoBehaviour, ITargetUnit
    {
        public abstract ActionEngine_Unit GetUnit();

        //单位代理Root 单位可能存在外层包装,在这里读写
        public GameObject AgentUnitRoot
        {
            get
            {
                if (mAgentUnitRoot is null) mAgentUnitRoot = gameObject;
                return mAgentUnitRoot;
            }
            set
            {
                //EngineDebug.LogError($"设置root代理 [{value.name}]");
                mAgentUnitRoot = value;
            }
        }

        private GameObject mAgentUnitRoot = null;
    }
}