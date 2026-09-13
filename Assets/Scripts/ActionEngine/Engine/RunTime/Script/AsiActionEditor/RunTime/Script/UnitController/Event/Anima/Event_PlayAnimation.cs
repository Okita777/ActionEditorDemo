using System;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_PlayAnimation : IActionEventData
    {
        [SerializeField] private int m_AnimationID = 0;
        [SerializeField] private string m_AnimationName = null;

        #region property
        [EditorProperty("动画组件序号", EditorPropertyType.EEPT_Int)]
        public int AnimationID
        {
            get { return m_AnimationID; }
            set { m_AnimationID = value; }
        }
        [EditorProperty("动画片段名称", EditorPropertyType.EEPT_String)]
        public string AnimationName
        {
            get { return m_AnimationName; }
            set { m_AnimationName = value; }
        }
        #endregion

        [NonSerialized] private bool defaultActive = false;
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_PlayAnimation;
        public IActionEventData Creact() => new Event_PlayAnimation();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;

            if (_actionStateMachine.TryGetComponent(out ActionEngine_Animation _characterConfig, nameof(ActionEngine_Animation)))
            {
                _characterConfig.animations[AnimationID].Play(m_AnimationName);
                // if (_characterConfig.HelpPointDic.TryGetValue((ECharacteLimbType)m_HelpPointID, out Transform _transform))
                // {
                //     defaultActive = _transform.gameObject.activeSelf;
                //     _transform.gameObject.SetActive(m_IsActive);
                // }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_PlayAnimation _event = _eventData as Event_PlayAnimation;

            _event.AnimationID = m_AnimationID;
            _event.AnimationName = m_AnimationName;

            return _event;
        }
    }
}