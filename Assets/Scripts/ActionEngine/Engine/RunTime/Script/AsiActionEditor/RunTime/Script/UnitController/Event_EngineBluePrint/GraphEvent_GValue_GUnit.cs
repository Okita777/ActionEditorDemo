using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_GValue_GUnit : BluePrint_Unit
    {
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected GUnit m_GUnitVal = new GUnit();

        #region Property
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("GUnit", false, EditorGraphPropertyType.EEPT_GUnit)]
        public GUnit GUnitVal
        {
            get { return m_GUnitVal; }
            set { m_GUnitVal = value; }
        }
        #endregion

        [System.NonSerialized] private TargetUnit m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//避免同一帧下反复执行初始化

            if (!IsNode)
            {
                m_ReturnVal = part.ActionStateMachine.CurUnit;
                return;
            }
#if UNITY_EDITOR
            if (UnitVal is null)
            {
                UnitVal = new GraphEvent_Value_SelfUnit();
                EngineDebug.LogError($"[<color=#ff0000>GUnit 蓝图节点报错!! (战策处理)</color>], " +
                    $"请在编辑器模式下<color=#ffcc00>重新保存</color>该资产报错来源: {EngineDebug.DebugActionStatePart(part)}");
            }
#endif
            UnitVal.Init(part, _time);
            if (UnitVal.isValid(part))
            {
                m_ReturnVal = m_GUnitVal.GetValue(UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart);
            }
            else
            {
                m_ReturnVal = part.ActionStateMachine.CurUnit;
            }
            //if (!IsNode) m_ReturnVal = part.ActionStateMachine.CurUnit;
            //else m_ReturnVal = m_GUnitVal.GetValue(part);
        }
        public override TargetUnit value => m_ReturnVal;
        public override bool isValid(ActionStatePart part) => m_ReturnVal is not null;


#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_GValue_GUnit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                if (m_UnitVal is null) m_UnitVal = new GraphEvent_Value_SelfUnit();
                GraphEventG = new GraphEvent_GValue_GUnit();
                GraphEventG.GUnitVal = (GUnit)m_GUnitVal.Clone();
                GraphEventG.UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
                GraphEventG.IsNode = IsNode;
                //在保存好文件后重置状态
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    GraphEventG = null;
                });
            }
            return GraphEventG;
#endif
            return this;
        }
    }
}