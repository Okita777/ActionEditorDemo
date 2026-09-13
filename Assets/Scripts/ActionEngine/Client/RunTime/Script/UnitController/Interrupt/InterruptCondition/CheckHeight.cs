using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckHeight : IInterruptCondition
    {
        [SerializeField] protected float m_Checkheight = 1;
        [SerializeField] protected bool m_IsValid = true;
        [SerializeField] protected int m_LayerMask;

        #region Property

        [EditorProperty("检查高度: ", EditorPropertyType.EEPT_Float)]
        public float Checkheight
        {
            get { return m_Checkheight; }
            set { m_Checkheight = value; }
        }
        [EditorProperty("高度内: ", EditorPropertyType.EEPT_Bool)]
        public bool IsValid
        {
            get { return m_IsValid; }
            set { m_IsValid = value; }
        }
        [EditorProperty("检查层级: ", EditorPropertyType.EEPT_LayerMask)]
        public int LayerMask
        {
            get { return m_LayerMask; }
            set { m_LayerMask = value; }
        }

        #endregion

        public int InterruptType => (int)EConditionType.EIT_CheckHeight;

        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            return Physics.Raycast(unit.transform.TransformPoint(0, 0.01f, 0), unit.transform.up * -1,
                0.01f + m_Checkheight, actionStatePart.ActionStateMachine.GetLayer(m_LayerMask)) == m_IsValid;
        }

        public IInterruptCondition Clone()
        {
            CheckHeight _condition = new CheckHeight();

            _condition.Checkheight = m_Checkheight;
            _condition.IsValid = m_IsValid;
            _condition.LayerMask = m_LayerMask;

            return _condition;
        }
    }
}