using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_OnHiter : BluePrint_Unit
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
            UnitVal.Init(part, _time);
            if (UnitVal.isValid(part))
            {
                m_ReturnVal = UnitVal.value.GetUnit().ActionStateMachine.HitUnit;
                if (EngineResourcesManager.Instance.Player == part.ActionStateMachine.CurUnit)
                {
                    ActionStatePart part2 = UnitVal.value.GetUnit().ActionStateMachine.FirstStatePart;
                    if (UnitVal is GraphEvent_Value_SelfUnit)
                    {
                        if (m_ReturnVal is null)
                            Debug.LogError($"<color=#ff0000>没有命中对象</color>\n{EngineDebug.DebugActionStatePart(part2)}");
                        else
                            Debug.LogError($"<color=#ffcc00>有命中对象</color>\n{EngineDebug.DebugActionStatePart(part2)}");
                    }
                }
            }
            else m_ReturnVal = null;
            //m_ReturnVal = part.ActionStateMachine.HitUnit;
        }

        private bool Valid(ActionStatePart part, ActionStatePart self)
        {
#if UNITY_EDITOR
            if (part.ActionStateMachine.HitUnit is null)
            {
                //if (part.ActionStateMachine.AttackerUnit is null)
                {
                    if (part.CurrentActionState is not null)
                    {
                        EngineDebug.LogError("出错啦！！！  尝试获取命中者，但是还没有发生任何攻击行为" +
                            "\n出错的Action：" + self.CurrentActionState.Name + $"  单位[{self.ActionStateMachine.CurUnit.gameObject}]");
                    }
                    else
                    {
                        EngineDebug.LogError("出错啦！！！  尝试获取命中者，但是还没有发生任何攻击行为" +
                            $"  单位[{self.ActionStateMachine.CurUnit.gameObject}]");
                    }
                }
                return false;
            }
#endif
            return true;
        }

        public override TargetUnit value => m_ReturnVal;
        public override bool isValid(ActionStatePart part) => m_ReturnVal is not null;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_OnHiter GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_OnHiter();
                if (m_UnitVal is null) GraphEventG.UnitVal = new GraphEvent_GValue_GUnit();
                else GraphEventG.UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
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