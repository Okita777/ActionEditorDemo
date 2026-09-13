using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_IsDead_Unit : BluePrint_Bool
    {
        [SerializeReference] protected BluePrint_Unit m_BluePrint_val_l = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected bool m_BluePrint_val_r = true;

        #region Property
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit BluePrint_val_l
        {
            get { return m_BluePrint_val_l; }
            set { m_BluePrint_val_l = value; }
        }
        [EditorGraphProperty("存活状态", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80,
            Tooltip = "死亡时返回Falue, 反之返回Turn")]
        public bool BluePrint_val_r
        {
            get { return m_BluePrint_val_r; }
            set { m_BluePrint_val_r = value; }
        }
        #endregion

        [System.NonSerialized] private bool m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            m_BluePrint_val_l.Init(part, _time);
            if (m_BluePrint_val_l.isValid(part))
            {
                //EngineDebug.LogError($"单位死亡状态:{m_BluePrint_val_l.value.ActionStateMachine.GetDieState}\n{EngineDebug.DebugActionStatePart(m_BluePrint_val_l.value.ActionStateMachine.FirstStatePart)}");
                m_ReturnVal = m_BluePrint_val_l.value.GetUnit().ActionStateMachine.GetDieState != BluePrint_val_r;
            }
            else
            {
                m_ReturnVal = false;
            }
        }

        public override bool value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_IsDead_Unit _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_IsDead_Unit();
                _graphEvent.BluePrint_val_l = (BluePrint_Unit)m_BluePrint_val_l.Clone();
                _graphEvent.BluePrint_val_r = BluePrint_val_r;
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