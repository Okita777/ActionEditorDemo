using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_IsSkill_Unit : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_Unit m_BluePrint_val_l = new GraphEvent_Value_SelfUnit();
        // [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_GValue_GUnit();

        #region Property
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit BluePrint_val_l
        {
            get { return m_BluePrint_val_l; }
            set { m_BluePrint_val_l = value; }
        }
        #endregion

        [System.NonSerialized] private bool m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            if (!m_BluePrint_val_l.IsNode)
            {
                m_ReturnVal = part.ActionStateMachine.CurUnit is ActionEngine_Skill;
            }
            else
            {
                m_BluePrint_val_l.Init(part, _time);
#if UNITY_EDITOR
                if (!m_BluePrint_val_l.isValid(part)) return;
#endif
                m_ReturnVal = m_BluePrint_val_l.value is ActionEngine_Skill;
            }

            //m_ReturnVal = m_BluePrint_val_l.value is ActionEngine_Skill;
        }

        public override bool value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_IsSkill_Unit _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_IsSkill_Unit();
                _graphEvent.BluePrint_val_l = (BluePrint_Unit)m_BluePrint_val_l.Clone();
                //在保存好文件后重置状态
                ActionSaveFlishEvent.ActionEvent.AddListener(() =>
                {
                    _graphEvent = null;
                });
            }
            return _graphEvent;
#endif
            return this;
        }
    }
}