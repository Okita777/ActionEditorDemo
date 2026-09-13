using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    public class GraphEvent_TrackData_ActionStatePartPoint : BluePrint_PointData
    {
        [SerializeReference] protected BluePrint_Bool m_ReadValue = new GraphEvent_Value_Bool(false);
        #region Property
        [EditorGraphProperty("Parent", true, EditorGraphPropertyType.EEPT_Bool)]
        public BluePrint_Bool ReadValue
        {
            get { return m_ReadValue; }
            set { m_ReadValue = value; }
        }
        #endregion

        private PointData _return;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;

#if UNITY_EDITOR
            //在编辑模式并且在非运行模式时修改输出参数
            if (!Application.isPlaying)
            {
                Transform _trans = EngineResourcesManager.Instance.SkillTransform;
                if (_trans is null) _trans = part.ActionStateMachine.CurUnit.transform;
                _return = new PointData(_trans.position, _trans.rotation);
                return;
            }
#endif

            m_ReadValue.Init(part, _time);
            if (m_ReadValue.value)
            {
                Transform _trans = part.ActionStateMachine.CurUnit.transform;
                _return = new PointData(_trans.position, _trans.rotation);
            }
            else
            {
                _return = new PointData(part.Pos, part.Rot);
            }
        }
        public override PointData value => _return;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_TrackData_ActionStatePartPoint GraphEventG = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_TrackData_ActionStatePartPoint();
                GraphEventG.IsNode = IsNode;
                GraphEventG.ReadValue = (BluePrint_Bool)m_ReadValue.Clone();
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