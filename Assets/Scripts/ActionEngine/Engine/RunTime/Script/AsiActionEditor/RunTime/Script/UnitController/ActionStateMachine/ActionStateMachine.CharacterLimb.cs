using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        /// <summary>
        /// 获取角色挂点。服务端无骨骼或客户端挂点缺失时，统一回退到单位根节点。
        /// </summary>
        public bool TryGetCharacterLimb(ECharacteLimbType limbType, out Transform limbTransform)
        {
            limbTransform = null;
            if (CurUnit == null)
            {
                return false;
            }

            if (CurUnit.Channel == ERuntimeDataChannel.Server)
            {
                limbTransform = CurUnit.transform;
                return limbTransform != null;
            }

            if (TryGetComponent(out CharacterConfig characterConfig, nameof(CharacterConfig)) &&
                characterConfig.HelpPointDic.TryGetValue(limbType, out limbTransform) &&
                limbTransform != null)
            {
                return true;
            }

            limbTransform = CurUnit.transform;
            return limbTransform != null;
        }
    }
}
