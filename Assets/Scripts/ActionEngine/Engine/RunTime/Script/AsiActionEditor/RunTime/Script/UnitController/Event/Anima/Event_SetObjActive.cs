using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SetObjActive : IActionEventData
    {
        [SerializeField] private int m_HelpPointID = 0;
        [SerializeField] private bool m_IsActive = false;

        #region property
        [EditorProperty("显隐对象", EditorPropertyType.EEPT_CharacteLimbType)]
        public int HelpPointID
        {
            get { return m_HelpPointID; }
            set { m_HelpPointID = value; }
        }
        [EditorProperty("显示", EditorPropertyType.EEPT_Bool)]
        public bool IsActive
        {
            get { return m_IsActive; }
            set { m_IsActive = value; }
        }
        #endregion

        // [NonSerialized] private bool defaultActive = false;
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_SetObjActive;
        public IActionEventData Creact() => new Event_SetObjActive();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;

            if (_actionStateMachine.TryGetComponent(out CharacterConfig _characterConfig, nameof(CharacterConfig)))
            {
                if (_characterConfig.HelpPointDic.TryGetValue((ECharacteLimbType)m_HelpPointID, out Transform _transform))
                {
                    // defaultActive = _transform.gameObject.activeSelf;
                    _transform.gameObject.SetActive(m_IsActive);
                }
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;

            if (_actionStateMachine.TryGetComponent(out CharacterConfig _characterConfig, nameof(CharacterConfig)))
            {
                if (_characterConfig.HelpPointDic.TryGetValue((ECharacteLimbType)m_HelpPointID, out Transform _transform))
                {
                    // _transform.gameObject.SetActive(defaultActive);
                    _transform.gameObject.SetActive(!m_IsActive);
                }
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetObjActive _event = _eventData as Event_SetObjActive;

            _event.HelpPointID = m_HelpPointID;
            _event.IsActive = m_IsActive;

            return _event;
        }
    }
}