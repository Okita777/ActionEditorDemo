using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_Math_Conver_Int : BluePrint_Int
    {
        [SerializeReference] protected BluePrint_Float m_BluePrint_val_l = new GraphEvent_Value_Float();

        #region Property
        [EditorGraphProperty("Float", true, EditorGraphPropertyType.EEPT_Int)]
        public BluePrint_Float BluePrint_val_l
        {
            get { return m_BluePrint_val_l; }
            set { m_BluePrint_val_l = value; }
        }


        #endregion

        [System.NonSerialized] private int m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

            m_BluePrint_val_l.Init(part, _time);
            m_ReturnVal = (int)m_BluePrint_val_l.value;
        }

        public override int value => m_ReturnVal;
#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Conver_Int _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Math_Conver_Int();
                _graphEvent.BluePrint_val_l = (BluePrint_Float)m_BluePrint_val_l.Clone();
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