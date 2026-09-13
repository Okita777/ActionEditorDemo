using AsiActionEngine.RunTime;
using UnityEngine;

#if FMOD
using FMODUnity;
#endif

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_GValueCallback : IActionEventData
    {
        [SerializeField] public GEnum m_GEnum = new GEnum();
        [SerializeField] public GInt m_ID = new GInt();

        [EditorProperty("回调类型", EditorPropertyType.EEPT_GEnum)]
        public GEnum GEnum
        {
            get { return m_GEnum; }
            set { m_GEnum = value; }
        }
        [EditorProperty("回调参数(Int)", EditorPropertyType.EEPT_GInt)]
        public GInt ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }
        public int GetEvenType() => (int)EEvenType.EET_GValueCallback;

        public IActionEventData Creact() => new Event_GValueCallback();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            if (_actionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine.TryGetStaticLogic(out Ex_GValue_Callback _gv, nameof(Ex_GValue_Callback)))
            {
                //EngineDebug.LogError($"执行当前事件的对象 \n[{EngineDebug.DebugActionStatePart(_actionState)}]");

                _gv.CallbackInvoke(_actionState, GEnum, ID.GetValue(_actionState));
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_GValueCallback _event = _eventData as Event_GValueCallback;
            _event.GEnum = (GEnum)m_GEnum.Clone();
            _event.ID = (GInt)ID.Clone();
            return _event;
        }

        //UnityEditor
        //public static implicit operator Event_PlayAudio(SerializedObject v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}