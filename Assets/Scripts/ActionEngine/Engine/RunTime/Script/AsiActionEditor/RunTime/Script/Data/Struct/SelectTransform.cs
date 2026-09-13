using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class SelectTransform
    {
        [SerializeField] public bool m_IsCharacterLimb;
        [SerializeField] public ushort m_Index;
        [SerializeField] public ushort m_GroupIndex;
        [SerializeField] public byte m_Value;
        // [NonSerialized] private ActionStateMachine m_StateMachine = null;

        // public void Init(ActionStateMachine _actionStateMachine)
        // {
        //     m_StateMachine = _actionStateMachine;
        //     // EngineDebug.Log("设置过了啊");
        // }

        public bool IsValid(ActionStateMachine _state)
        {
#if UNITY_EDITOR
            if (_state is null)
            {
                EngineDebug.LogError("_state 空了");
                return false;
            }
#endif
            return Get(_state) is not null;
        }
        public bool IsValid(ActionStateMachine _state, CharacterConfig character)
        {
            return Get(_state, character) is not null;
        }
        public Transform Get(ActionStateMachine _state, CharacterConfig character)
        {
            if (m_IsCharacterLimb)
            {
                if (_state?.CurUnit?.Channel == ERuntimeDataChannel.Server)
                {
                    return _state.CurUnit.transform;
                }

                if (character != null &&
                    character.HelpPointDic.TryGetValue((ECharacteLimbType)m_Value, out Transform helpPoint) &&
                    helpPoint != null)
                {
                    return helpPoint;
                }
                return _state?.CurUnit?.transform;
            }

            return _state.GetTransform(m_GroupIndex, m_Index, m_Value);
        }

        public Transform Get(ActionStateMachine _state)
        {
            if (m_IsCharacterLimb)
            {
                if (_state.TryGetCharacterLimb((ECharacteLimbType)m_Value, out Transform helpPoint))
                {
                    return helpPoint;
                }

                return null;
            }

            //int _index = m_Value;
            //_index += m_Index * 1000;
            // if (_state == null)
            // {
            //     EngineDebug.LogError("Select Transform Error");
            //     return null;
            // }
            return _state.GetTransform(m_GroupIndex, m_Index, m_Value);
        }

        public SelectTransform Clone()
        {
            return this;
        }
    }
}