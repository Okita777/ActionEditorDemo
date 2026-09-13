using System.Collections.Generic;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime.GValueEquation
{
    [System.Serializable]
    public abstract class GValueEquation_Part
    {
        public abstract EEquationReturnType ReturnType { get; }
        public abstract GValueEquation_Part Clone();
    }

    [System.Serializable]
    public class GValueEquation_FloatPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_Float m_GraphEvent = new GraphEvent_NoValue_Float();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Float GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.Float;

        public float Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_FloatPart clone = new GValueEquation_FloatPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_IntPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_Int m_GraphEvent = new GraphEvent_NoValue_Int();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Int GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.Int;

        public int Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_IntPart clone = new GValueEquation_IntPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_BoolPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_Bool m_GraphEvent = new GraphEvent_NoValue_Bool();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Bool GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.Bool;

        public bool Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_BoolPart clone = new GValueEquation_BoolPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_StringPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_String m_GraphEvent = new GraphEvent_NoValue_String();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_String GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.String;

        public string Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_StringPart clone = new GValueEquation_StringPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_UnitPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_Unit m_GraphEvent = new GraphEvent_NoValue_Unit();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Unit GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.Unit;

        public TargetUnit Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_UnitPart clone = new GValueEquation_UnitPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_Vector3Part : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_Vector3 m_GraphEvent = new GraphEvent_NoValue_Vector3();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.Vector3;

        public Vector3 Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_Vector3Part clone = new GValueEquation_Vector3Part();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_PointPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_Point m_GraphEvent = new GraphEvent_NoValue_Point();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Point GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.Point;

        public PointData Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_PointPart clone = new GValueEquation_PointPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_GroupFloatPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_GroupFloat m_GraphEvent = new GraphEvent_NoValue_GroupFloat();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_GroupFloat GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.GroupFloat;

        public List<float> Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_GroupFloatPart clone = new GValueEquation_GroupFloatPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_GroupIntPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_GroupInt m_GraphEvent = new GraphEvent_NoValue_GroupInt();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_GroupInt GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.GroupInt;

        public List<int> Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_GroupIntPart clone = new GValueEquation_GroupIntPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValueEquation_GroupUnitPart : GValueEquation_Part
    {
        [SerializeField] protected GraphEvent_NoValue_GroupUnit m_GraphEvent = new GraphEvent_NoValue_GroupUnit();

        #region Property
        [EditorProperty("公式蓝图", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_GroupUnit GraphEvent
        {
            get { return m_GraphEvent; }
            set { m_GraphEvent = value; }
        }
        #endregion

        public override EEquationReturnType ReturnType => EEquationReturnType.GroupUnit;

        public List<ActionEngine_Unit> Value(ActionStatePart _part, ActionMachineTime _time, bool _isInit = true)
        {
            return m_GraphEvent.value(_part, _time, _isInit);
        }

        public override GValueEquation_Part Clone()
        {
#if UNITY_EDITOR
            ActionSaveFlishEvent.Run();
            GValueEquation_GroupUnitPart clone = new GValueEquation_GroupUnitPart();
            clone.GraphEvent = m_GraphEvent.Clone();
            return clone;
#endif
            return this;
        }
    }
}
