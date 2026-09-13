using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.Event_Extend;
using AsiTimeLine.RunTime;

#if Cinemachine
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;
#else
        using Cinemachine;
#endif
#endif
using UnityEngine;

namespace AsiActionEditor_Ex.RunTime
{
    [System.Serializable]
    public class Event_CameraShake : IActionEventData
    {
        public enum ETriggerTime
        {
            Any,
            OnHit
        }
        [SerializeField] private float mAmplitudeGain = 0.1f;
        [SerializeField] private float mFrequencyGain = 0.1f;
        [SerializeField] private EVector3 mPivotOffset = new EVector3();
        [SerializeField] private ETriggerTime mTriggerTime = ETriggerTime.Any;
        [SerializeField] private float mDuration = 0.2f;

        #region Property
        [EditorProperty("枢轴偏移： ", EditorPropertyType.EEPT_Vector3)]
        public EVector3 PivotOffset
        {
            get { return mPivotOffset; }
            set { mPivotOffset = value; }
        }
        [EditorProperty("振幅： ", EditorPropertyType.EEPT_Float)]
        public float AmplitudeGain
        {
            get { return mAmplitudeGain; }
            set { mAmplitudeGain = value; }
        }
        [EditorProperty("频率： ", EditorPropertyType.EEPT_Float)]
        public float FrequencyGain
        {
            get { return mFrequencyGain; }
            set { mFrequencyGain = value; }
        }
        [EditorProperty("触发时机： ", EditorPropertyType.EEPT_Enum)]
        public ETriggerTime TriggerTime
        {
            get { return mTriggerTime; }
            set { mTriggerTime = value; }
        }
        [EditorProperty("抖动持续时长： ", EditorPropertyType.EEPT_Float)]
        public float Duration
        {
            get { return mDuration; }
            set { mDuration = value; }
        }
        #endregion
        [NonSerialized] private float mAmplitudeGain_p = 0.1f;
        [NonSerialized] private float mFrequencyGain_p = 0.1f;
        [NonSerialized] private Vector3 mPivotOffset_p = Vector3.one;
        [NonSerialized] private CinemachineBasicMultiChannelPerlin _perlin;
        public int GetEvenType() => (int)EEvenType.EET_CameraShake;
        public IActionEventData Creact() => new Event_CameraShake();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
#if Cinemachine

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            _perlin = ActionEngineManager_Input.Instance.CurCamera.perlin;
            if (_perlin is not null)
            {
#if UNITY_6000_0_OR_NEWER
                mAmplitudeGain_p = _perlin.AmplitudeGain;
                mFrequencyGain_p = _perlin.FrequencyGain;
                mPivotOffset_p = _perlin.PivotOffset;

                if (mTriggerTime == ETriggerTime.Any)
                {
                    _perlin.AmplitudeGain = mAmplitudeGain;
                    _perlin.FrequencyGain = mFrequencyGain;
                    _perlin.PivotOffset = mPivotOffset.GetValue();
                }
#else
                mAmplitudeGain_p = _perlin.m_AmplitudeGain;
                mFrequencyGain_p = _perlin.m_FrequencyGain;
                mPivotOffset_p = _perlin.m_PivotOffset;

                if (mTriggerTime == ETriggerTime.Any)
                {
                    _perlin.m_AmplitudeGain = mAmplitudeGain;
                    _perlin.m_FrequencyGain = mFrequencyGain;
                    _perlin.m_PivotOffset = mPivotOffset.GetValue();
                }
#endif


                if (!_isSingle)
                {
                    if (mTriggerTime == ETriggerTime.OnHit)
                    {
                        if (_stateMachine.TryGetStaticLogic(out Ex_AttackBox _attackBox, nameof(Ex_AttackBox)))
                        {
                            _stateMachine.EventSystem.OnHit += OnHitCallBack;
                        }
                    }
                }
            }
            // else
            // {
            //     EngineDebug.Log("空的？ :" + ActionEngineManager_Input.Instance.CurCamera.GetHashCode());
            // }
#endif

        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
#if Cinemachine

            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;

            if (_perlin is not null)
            {
                if (mTriggerTime == ETriggerTime.OnHit)
                {
                    if (_stateMachine.TryGetStaticLogic(out Ex_AttackBox _attackBox, nameof(Ex_AttackBox)))
                    {
                        _stateMachine.EventSystem.OnHit -= OnHitCallBack;
                    }
                }

#if UNITY_6000_0_OR_NEWER
                _perlin.AmplitudeGain = mAmplitudeGain_p;
                _perlin.FrequencyGain = mFrequencyGain_p;
                _perlin.PivotOffset = mPivotOffset_p;
#else
                _perlin.m_AmplitudeGain = mAmplitudeGain_p;
                _perlin.m_FrequencyGain = mFrequencyGain_p;
                _perlin.m_PivotOffset = mPivotOffset_p;
#endif

            }
#endif

        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_CameraShake clone = _eventData as Event_CameraShake;
            clone.AmplitudeGain = mAmplitudeGain;
            clone.FrequencyGain = mFrequencyGain;
            clone.PivotOffset = mPivotOffset;
            clone.TriggerTime = mTriggerTime;
            clone.mDuration = Duration;

            return clone;
        }

        private void OnHitCallBack(ActionEngine_Unit _unit, bool _isUnit, GameObject _gameObject, TargetUnit _onHiter)
        {
            // EngineDebug.Log("相机抖动");
#if Cinemachine
#if UNITY_6000_0_OR_NEWER
            _perlin.AmplitudeGain = mAmplitudeGain;
            _perlin.FrequencyGain = mFrequencyGain;
            _perlin.PivotOffset = mPivotOffset.GetValue();
#else
            _perlin.m_AmplitudeGain = mAmplitudeGain;
            _perlin.m_FrequencyGain = mFrequencyGain;
            _perlin.m_PivotOffset = mPivotOffset.GetValue();
#endif
#endif

        }
    }
}