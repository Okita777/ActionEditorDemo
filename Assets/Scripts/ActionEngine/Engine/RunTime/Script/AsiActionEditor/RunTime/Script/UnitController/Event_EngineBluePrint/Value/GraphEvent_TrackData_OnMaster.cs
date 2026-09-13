using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_OnMaster : BluePrint_Unit
    {
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        #region Property

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }

        #endregion

        [System.NonSerialized] private TargetUnit m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            //EngineDebug.LogError($"[{part.ActionStateMachine.CurUnit.gameObject}] " +
            //    $"的所有者[{part.ActionStateMachine.CurUnit.GetMaster.gameObject}({part.ActionStateMachine.CurUnit.GetMaster.gameObject.GetHashCode()})]" +
            //    $"\n所有者命中了单位[{part.ActionStateMachine.CurUnit.GetMaster.ActionStateMachine.HitUnit is not null}]");
            //            if (!m_UnitVal.IsNode)
            //            {
            //#if UNITY_EDITOR
            //                if (!Valid(part, part)) return;
            //#endif
            //                m_ReturnVal = part.ActionStateMachine.CurUnit.GetMaster;
            //            }
            //            else
            //            {
            //                m_UnitVal.Init(part, _time);
            //#if UNITY_EDITOR
            //                if (!Valid(m_UnitVal.value.ActionStateMachine.FirstStatePart, part)) return;
            //#endif
            //                m_ReturnVal = m_UnitVal.value.ActionStateMachine.CurUnit.GetMaster;
            //            }
            m_ReturnVal = null;
            UnitVal.Init(part, _time);
            if (UnitVal.isValid(part)) m_ReturnVal = UnitVal.value.GetUnit().GetMaster;
            //else m_ReturnVal = null;

        }

        private bool Valid(ActionStatePart part, ActionStatePart self)
        {
#if UNITY_EDITOR
            if (part.ActionStateMachine.CurUnit.GetMaster is null)
            {
                //if (part.CurrentActionState is not null)
                //{
                //    EngineDebug.LogError("出错啦！！！  尝试获所有者，请确保当前单位为技能" +
                //        "\n出错的Action：" + self.CurrentActionState.Name + $"  单位[{self.ActionStateMachine.CurUnit.gameObject}]");
                //}
                //else
                //{
                //    EngineDebug.LogError("出错啦！！！  尝试获所有者，请确保当前单位为技能" +
                //        $"  单位[{self.ActionStateMachine.CurUnit.gameObject}]");
                //}
                return false;
            }
#endif
            return true;
        }

        public override TargetUnit value => m_ReturnVal;
        public override bool isValid(ActionStatePart part) => m_ReturnVal is not null;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_OnMaster GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_OnMaster();
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