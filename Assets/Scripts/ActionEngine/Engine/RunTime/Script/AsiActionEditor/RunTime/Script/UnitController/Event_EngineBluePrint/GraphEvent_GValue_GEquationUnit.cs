using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_GValue_GEquationUnit : BluePrint_Unit, IExpectedEquationReturnType, IEquationExposedHolder
    {
        public EEquationReturnType ExpectedReturnType => EEquationReturnType.Unit;
        [SerializeField] protected GEquation m_GEquationVal = new GEquation();
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        // 运行时只读；覆写逻辑在 EquationExposedParamRuntime.Apply
        [SerializeField] protected List<EquationExposedParamBinding> m_ExposedBindings = new List<EquationExposedParamBinding>();
        // 复用型暂存缓冲，稳态 0 GC（详见 EquationExposedParamRuntime.SavedBuffer）
        //[System.NonSerialized] private readonly EquationExposedParamRuntime.SavedBuffer m_SavedBuffer = new EquationExposedParamRuntime.SavedBuffer();

        #region Property
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("Unit", false, EditorGraphPropertyType.EEPT_GEquation)]
        public GEquation GEquationVal
        {
            get { return m_GEquationVal; }
            set { m_GEquationVal = value; }
        }
        #endregion

        GEquation IEquationExposedHolder.GEquationVal => m_GEquationVal;
        List<EquationExposedParamBinding> IEquationExposedHolder.ExposedBindings => m_ExposedBindings;

        [System.NonSerialized] private TargetUnit m_ReturnVal;

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
                    m_ReturnVal = null;
                    EngineDebug.DebugUnitGruphError(m_UnitVal);
                    return;
                }
#endif
                evalPart = m_UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart;
            }

            //EquationExposedParamRuntime.Apply(this, part, evalPart, _time, m_SavedBuffer);
            //try
            //{
            //    m_ReturnVal = m_GEquationVal.unitValue(evalPart, _time);
            //}
            //finally
            //{
            //    EquationExposedParamRuntime.Restore(m_SavedBuffer);
            //}
            m_ReturnVal = m_GEquationVal.unitValue(evalPart, _time);
        }
        public override TargetUnit value => m_ReturnVal;

#if UNITY_EDITOR
        [System.NonSerialized] protected GraphEvent_GValue_GEquationUnit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_GValue_GEquationUnit();
                GraphEventG.GEquationVal = (GEquation)m_GEquationVal.Clone();
                GraphEventG.UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
                GraphEventG.m_ExposedBindings = EquationExposedParamBinding.CloneList(m_ExposedBindings);
                GraphEventG.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => { GraphEventG = null; });
            }
            return GraphEventG;
#endif
            return this;
        }

        public override bool isValid(ActionStatePart part) => m_ReturnVal is not null;
    }
}
