using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    // 输出指定单位当前帧的移动速度(m/s)。
    // 默认按单位每帧记录的世界位置差分计算；也可切换为读取引擎最终输出到CharacterController的位移向量换算速度。
    public class GraphEvent_Other_Float_UnitMoveSpeed : BluePrint_Float
    {
        [SerializeReference] protected BluePrint_Unit m_Unit = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected bool m_HorizontalOnly = true;
        [SerializeField] protected bool m_UseEngineMove = false;

        #region Property
        [EditorGraphProperty("单位", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit Unit
        {
            get { return m_Unit; }
            set { m_Unit = value; }
        }

        [EditorGraphProperty("仅水平(忽略Y)", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public bool HorizontalOnly
        {
            get { return m_HorizontalOnly; }
            set { m_HorizontalOnly = value; }
        }

        [EditorGraphProperty("取引擎输出位移", false, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 80)]
        public bool UseEngineMove
        {
            get { return m_UseEngineMove; }
            set { m_UseEngineMove = value; }
        }
        #endregion

        public override float value => m_ReturnVal;

        [System.NonSerialized] private float m_ReturnVal;

        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//避免同一帧内反复执行浪费性能

            m_Unit.Init(part, _time);
            if (!m_Unit.isValid(part))
            {
                m_ReturnVal = 0f;
                return;
            }

            ActionEngine_Unit unit = m_Unit.value.GetUnit();
            if (unit == null)
            {
                m_ReturnVal = 0f;
                return;
            }

            float dt = _time.Deltatime;
            if (dt <= 0f)
            {
                m_ReturnVal = 0f;
                return;
            }

            // 引擎输出位移：本帧最终传给CharacterController.Move的向量；否则用单位记录的世界位置差分
            m_ReturnVal = m_Unit.value.GetUnit().UnitSpeed;
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Other_Float_UnitMoveSpeed _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Other_Float_UnitMoveSpeed
                {
                    Unit = (BluePrint_Unit)m_Unit.Clone(),
                    HorizontalOnly = m_HorizontalOnly,
                    UseEngineMove = m_UseEngineMove,
                    IsNode = IsNode
                };
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
