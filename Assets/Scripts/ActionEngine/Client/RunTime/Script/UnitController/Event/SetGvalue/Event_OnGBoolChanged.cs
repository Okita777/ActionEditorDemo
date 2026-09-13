using System;
using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_OnGBoolChanged : IActionEventData, IGValueListenEvent
    {
        [SerializeField] protected EGValueListenKind m_ListenKind = EGValueListenKind.GBool;

        [SerializeField] protected GInt m_ListenGInt = new GInt(0, true);
        [SerializeField] protected GFloat m_ListenGFloat = new GFloat(0, true);
        [SerializeField] protected GEnum m_ListenGEnum = new GEnum(0, true);
        [SerializeField] protected GBool m_ListenGBool = new GBool(true, true);
        [SerializeField] protected GUnit m_ListenGUnit = new GUnit(true);

        [SerializeField] protected GraphEvent_NoValue_Unit m_ListenTargetUnit = new GraphEvent_NoValue_Unit();
        [SerializeField] protected GraphEvent_NoValue_Bool m_BluePrint = new GraphEvent_NoValue_Bool();
        [SerializeField] protected GBool m_WriteGBool = new GBool(true, true);
        [SerializeField] protected GraphEvent_NoValue_Unit m_TargetUnit = new GraphEvent_NoValue_Unit();

        [EditorProperty("监听目标类型", EditorPropertyType.EEPT_Enum, EnumNames = new[] { "GInt", "GFloat", "GEnum", "GBool", "GUnit" })]
        public EGValueListenKind ListenKind { get => m_ListenKind; set => m_ListenKind = value; }

        EGValueListenKind IGValueListenEvent.ListenKind => m_ListenKind;

        [EditorProperty("(监听)GInt", EditorPropertyType.EEPT_Int)]
        public GInt ListenGInt { get => m_ListenGInt; set => m_ListenGInt = value; }
        [EditorProperty("(监听)GFloat", EditorPropertyType.EEPT_Float)]
        public GFloat ListenGFloat { get => m_ListenGFloat; set => m_ListenGFloat = value; }
        [EditorProperty("(监听)GEnum", EditorPropertyType.EEPT_Enum)]
        public GEnum ListenGEnum { get => m_ListenGEnum; set => m_ListenGEnum = value; }
        [EditorProperty("(监听)GBool", EditorPropertyType.EEPT_Bool)]
        public GBool ListenGBool { get => m_ListenGBool; set => m_ListenGBool = value; }
        [EditorProperty("(监听)GUnit", EditorPropertyType.EEPT_Unit)]
        public GUnit ListenGUnit { get => m_ListenGUnit; set => m_ListenGUnit = value; }

        [EditorProperty("监听目标单位", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit ListenTargetUnit
        {
            get
            {
#if UNITY_EDITOR
                if (m_ListenTargetUnit is null) m_ListenTargetUnit = new GraphEvent_NoValue_Unit();
#endif
                return m_ListenTargetUnit;
            }
            set { m_ListenTargetUnit = value; }
        }

        [EditorProperty("蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Bool BluePrint { get => m_BluePrint; set => m_BluePrint = value; }
        [EditorProperty("写入GBool", EditorPropertyType.EEPT_Bool)]
        public GBool WriteTarget { get => m_WriteGBool; set => m_WriteGBool = value; }
        [EditorProperty("被设置GV的目标单位", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit TargetUnit
        {
            get
            {
#if UNITY_EDITOR
                if (m_TargetUnit is null) m_TargetUnit = new GraphEvent_NoValue_Unit();
#endif
                return m_TargetUnit;
            }
            set { m_TargetUnit = value; }
        }

        public int GetEvenType() => (int)EEvenType.EET_OnGBoolChanged;
        public IActionEventData Creact() => new Event_OnGBoolChanged();

        [NonSerialized] private ActionStateMachine.DOnGIntChanged m_CallbackInt;
        [NonSerialized] private ActionStateMachine.DOnGFloatChanged m_CallbackFloat;
        [NonSerialized] private ActionStateMachine.DOnGEnumChanged m_CallbackEnum;
        [NonSerialized] private ActionStateMachine.DOnGBoolChanged m_CallbackBool;
        [NonSerialized] private ActionStateMachine.DOnGUnitChanged m_CallbackUnit;
        [NonSerialized] private ActionStateMachine m_StateMachine;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionEngine_Unit listenUnit = ListenTargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime).GetUnit();
            m_StateMachine = listenUnit != null ? listenUnit.ActionStateMachine : _actionState.ActionStateMachine;
            void RunWrite()
            {
                ActionEngine_Unit targetUnit = TargetUnit.value(_actionState, EngineResourcesManager.Instance.MachineTime).GetUnit();
                if (targetUnit == null) return;
                bool val = m_BluePrint.value(_actionState, EngineResourcesManager.Instance.MachineTime);
                m_WriteGBool.SetValue(targetUnit.ActionStateMachine.FirstStatePart, val);
            }
            switch (m_ListenKind)
            {
                case EGValueListenKind.GInt:
                    m_CallbackInt = (_, __) => RunWrite();
                    m_StateMachine.OnGIntChanged(m_ListenGInt, m_CallbackInt);
                    break;
                case EGValueListenKind.GFloat:
                    m_CallbackFloat = (_, __) => RunWrite();
                    m_StateMachine.OnGFloatChanged(m_ListenGFloat, m_CallbackFloat);
                    break;
                case EGValueListenKind.GEnum:
                    m_CallbackEnum = (_, __) => RunWrite();
                    m_StateMachine.OnGEnumChanged(m_ListenGEnum, m_CallbackEnum);
                    break;
                case EGValueListenKind.GBool:
                    m_CallbackBool = (_, __) => RunWrite();
                    m_StateMachine.OnGBoolChanged(m_ListenGBool, m_CallbackBool);
                    break;
                case EGValueListenKind.GUnit:
                    m_CallbackUnit = (_, __) => RunWrite();
                    m_StateMachine.OnGUnitChanged(m_ListenGUnit, m_CallbackUnit);
                    break;
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (m_StateMachine is null) return;
            switch (m_ListenKind)
            {
                case EGValueListenKind.GInt:
                    if (m_CallbackInt != null)
                    {
                        m_StateMachine.RemoveGIntChanged(m_ListenGInt, m_CallbackInt);
                        m_CallbackInt = null;
                    }
                    break;
                case EGValueListenKind.GFloat:
                    if (m_CallbackFloat != null)
                    {
                        m_StateMachine.RemoveGFloatChanged(m_ListenGFloat, m_CallbackFloat);
                        m_CallbackFloat = null;
                    }
                    break;
                case EGValueListenKind.GEnum:
                    if (m_CallbackEnum != null)
                    {
                        m_StateMachine.RemoveGEnumChanged(m_ListenGEnum, m_CallbackEnum);
                        m_CallbackEnum = null;
                    }
                    break;
                case EGValueListenKind.GBool:
                    if (m_CallbackBool != null)
                    {
                        m_StateMachine.RemoveGBoolChanged(m_ListenGBool, m_CallbackBool);
                        m_CallbackBool = null;
                    }
                    break;
                case EGValueListenKind.GUnit:
                    if (m_CallbackUnit != null)
                    {
                        m_StateMachine.RemoveGUnitChanged(m_ListenGUnit, m_CallbackUnit);
                        m_CallbackUnit = null;
                    }
                    break;
            }
            m_StateMachine = null;
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_OnGBoolChanged _event = _eventData as Event_OnGBoolChanged;
            _event.m_ListenKind = m_ListenKind;
            _event.m_ListenGInt = (GInt)m_ListenGInt.Clone();
            _event.m_ListenGFloat = (GFloat)m_ListenGFloat.Clone();
            _event.m_ListenGEnum = (GEnum)m_ListenGEnum.Clone();
            _event.m_ListenGBool = (GBool)m_ListenGBool.Clone();
            _event.m_ListenGUnit = (GUnit)m_ListenGUnit.Clone();
            _event.m_BluePrint = m_BluePrint.Clone();
            _event.m_WriteGBool = (GBool)m_WriteGBool.Clone();
            _event.m_TargetUnit = TargetUnit.Clone();
            _event.m_ListenTargetUnit = ListenTargetUnit.Clone();
            return _event;
        }
    }
}
