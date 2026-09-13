using System;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SetLayerWeight : IActionEventData
    {
        [SerializeField] private int m_LayerTarget = 0;
        [SerializeField] private float m_Weight = 1.0f;
        [SerializeField] private float m_lerpTime = 0.2f;

        #region property
        [EditorProperty("层级对象", EditorPropertyType.EEPT_Int)]
        public int LayerTarget
        {
            get { return m_LayerTarget; }
            set { m_LayerTarget = value; }
        }
        [EditorProperty("目标权重", EditorPropertyType.EEPT_Float)]
        public float Weight
        {
            get { return m_Weight; }
            set { m_Weight = value; }
        }
        [EditorProperty("过渡耗时(s)", EditorPropertyType.EEPT_Float)]
        public float LerpTime
        {
            get { return m_lerpTime; }
            set { m_lerpTime = value; }
        }
        #endregion

        // [NonSerialized] private bool defaultActive = false;
        public int GetEvenType() => -(int)EEvenTypeInternal.EET__SetLayerWeight;
        public IActionEventData Creact() => new Event_SetLayerWeight();

        [NonSerialized] private float mLerpT_total;
        [NonSerialized] private float mLerpT_now;
        [NonSerialized] private float mStartWeight;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            mLerpT_total = m_lerpTime;
            mLerpT_now = 0;

            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;
            if (_actionStateMachine.TryGetComponent(out Animator _animator, nameof(Animator)))
            {
                mStartWeight = _animator.GetLayerWeight(m_LayerTarget);
                if (_isSingle)
                {
                    _animator.SetLayerWeight(m_LayerTarget, m_Weight);
                }
            }
        }

        public void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;
            if (_actionStateMachine.TryGetComponent(out Animator _animator, nameof(Animator)))
            {
                if (mLerpT_total > 0 && mLerpT_now < mLerpT_total)
                {
                    _animator.SetLayerWeight(m_LayerTarget, Mathf.Lerp( mStartWeight, m_Weight, (mLerpT_now / mLerpT_total)) );

                    mLerpT_now += _actionTime.Deltatime;
                }
                else
                {
                    //_animator.SetLayerWeight(m_LayerTarget, _actionTime.GetPercentage() * m_Weight);
                    _animator.SetLayerWeight(m_LayerTarget, m_Weight);
                }
            }
        }

        //public void Exit(ActionStatePart _actionState, bool _interruot)
        //{
        //    ActionStateMachine _actionStateMachine = _actionState.ActionStateMachine;

        //    if (_actionStateMachine.TryGetComponent(out Animator _animator))
        //    {
        //        _animator.SetLayerWeight(m_LayerTarget, m_Weight);
        //    }
        //}

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SetLayerWeight _event = _eventData as Event_SetLayerWeight;

            _event.LayerTarget = m_LayerTarget;
            _event.Weight = m_Weight;
            _event.LerpTime = m_lerpTime;

            return _event;
        }
    }
}