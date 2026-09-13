using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_GValue_GEquationFloat : BluePrint_Float, IExpectedEquationReturnType, IEquationExposedHolder
    {
        public EEquationReturnType ExpectedReturnType => EEquationReturnType.Float;
        [SerializeField] protected GEquation m_IntVal = new GEquation();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        // Runtime read-only; override logic lives in EquationExposedParamRuntime.Apply.
        [SerializeField] protected List<EquationExposedParamBinding> m_ExposedBindings = new List<EquationExposedParamBinding>();
        // Reusable scratch buffer (zero-GC after warmup). See EquationExposedParamRuntime.SavedBuffer.
        //[System.NonSerialized] private readonly EquationExposedParamRuntime.SavedBuffer m_SavedBuffer = new EquationExposedParamRuntime.SavedBuffer();

        #region Property
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("Float", false, EditorGraphPropertyType.EEPT_GEquation)]
        public GEquation GEquationVal
        {
            get {
                return m_IntVal;
            }
            set { m_IntVal = value; }
        }
        #endregion

        GEquation IEquationExposedHolder.GEquationVal => m_IntVal;
        List<EquationExposedParamBinding> IEquationExposedHolder.ExposedBindings => m_ExposedBindings;

        [System.NonSerialized] private float m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            ActionStatePart evalPart;
            if (!m_UnitVal.IsNode)
            {
                evalPart = part;
            }
            else
            {
                m_UnitVal.Init(part, _time);
#if UNITY_EDITOR
                if (m_UnitVal.value is null)
                {
                    m_ReturnVal = 0;
                    EngineDebug.DebugUnitGruphError(m_UnitVal);
                    return;
                }
#endif
                evalPart = m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart;
            }

            //EquationExposedParamRuntime.Apply(this, part, evalPart, _time, m_SavedBuffer);
            //try
            //{
            //    m_ReturnVal = m_IntVal.value(evalPart, _time);
            //}
            //finally
            //{
            //    EquationExposedParamRuntime.Restore(m_SavedBuffer);
            //}
            m_ReturnVal = m_IntVal.value(evalPart, _time);
        }
        public override float value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_GValue_GEquationFloat GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GEquationFloat();
                //if (!Application.isPlaying)
                //{
                //    if (GEquationVal is null)
                //    {
                //        EngineDebug.LogError($"<color=#ff0000>GV???????!!!</color>\n{EngineDebug.GetEventPath()}");
                //    }
                //    else
                //    {
                //        int g = GEquationVal.mValueGroupIndex;
                //        int i = GEquationVal.mValueIndex;
                //        string _str = "(???)->";
                //        if (g == 0 && i == 0) _str += "<color=#ff0000>???????????</color>";
                //        else _str += $"[<color=#ffcc00>{g},{i}</color>]";
                //        _str += $"\n{EngineDebug.GetEventPath()}";
                //        EngineDebug.Log($"???????????!!" + _str);
                //    }
                //}

                GraphEventG.GEquationVal = (GEquation)GEquationVal.Clone();
                GraphEventG.UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
                GraphEventG.m_ExposedBindings = EquationExposedParamBinding.CloneList(m_ExposedBindings);
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}
