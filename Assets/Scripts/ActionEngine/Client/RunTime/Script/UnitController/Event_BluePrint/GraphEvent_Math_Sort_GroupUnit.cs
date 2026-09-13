using System.Collections.Generic;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Math_Sort_GroupUnit : BluePrint_GroupUnit
    {
        [SerializeReference] protected BluePrint_GroupUnit m_GroupValue = new GraphEvent_GValue_GGUnit();
        [SerializeReference] protected BluePrint_Float m_IsCheckValid = new GraphEvent_Value_Float();
        [SerializeReference] protected BluePrint_Bool m_SortType = new GraphEvent_Value_Bool(true);

        #region Property
        [EditorGraphProperty("GroupUnit", true, EditorGraphPropertyType.EEPT_GGroupUnit, LabelWidth = 60)]
        public BluePrint_GroupUnit GroupVal
        {
            get { return m_GroupValue; }
            set { m_GroupValue = value; }
        }

        [EditorGraphProperty("排序值(Float)", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 80)]
        public BluePrint_Float IsCheckValid
        {
            get { return m_IsCheckValid; }
            set { m_IsCheckValid = value; }
        }
        [EditorGraphProperty("升序(小到大)(Bool)", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 120)]
        public BluePrint_Bool SortType
        {
            get { return m_SortType; }
            set { m_SortType = value; }
        }
        #endregion

        [System.NonSerialized] private List<ActionEngine_Unit> m_ReturnVal = null;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;//这一步是避免同一帧内反复执行浪费性能

            List<float> _floatList = EngineResourcesManager.Instance.CreateFloats();
            m_ReturnVal = EngineResourcesManager.Instance.CreateUnits();
            m_GroupValue.Init(part, _time);
            SortType.Init(part, _time);
            //m_ReturnVal.AddRange(m_GroupValue.value);

            part.ActionStateMachine.TryGetStaticLogic(out Ex_MathFuntion _MathFuntion, nameof(Ex_MathFuntion));
            _MathFuntion.m_Dic_UnitToFloat.Clear();

            part.ActionStateMachine.IsCheckBluePrintCon = false;//避免Bool蓝图相关节点未及时更新逻辑
            for (int i = 0; i < m_GroupValue.value.Count; i++)
            {
                part.ActionStateMachine.BluePrint_LoopIndex = i;
                IsCheckValid.Init(part, _time);
                if (_MathFuntion.m_Dic_UnitToFloat.TryGetValue(IsCheckValid.value, out List<ActionEngine_Unit> _list))
                {
                    //同Key对象
                    _list.Add(m_GroupValue.value[i]);
                }
                else
                {
                    List<ActionEngine_Unit> _unitList = _MathFuntion.m_List_Unit[i];
                    _unitList.Clear();
                    _unitList.Add(m_GroupValue.value[i]);
                    _MathFuntion.m_Dic_UnitToFloat.Add(IsCheckValid.value, _unitList);
                    _floatList.Add(IsCheckValid.value);
                }
            }
            part.ActionStateMachine.IsCheckBluePrintCon = true;

            if (SortType.value) _floatList.Sort((x, y) => x.CompareTo(y));
            else _floatList.Sort((x, y) => x.CompareTo(y));
            foreach (float item in _floatList)
            {
                foreach (ActionEngine_Unit unit in _MathFuntion.m_Dic_UnitToFloat[item])
                {
                    m_ReturnVal.Add(unit);
                }
            }
            //if(SortType == 0) m_ReturnVal.Sort((x, y) => x.CompareTo(y));
            //else m_ReturnVal.Sort((x, y) => y.CompareTo(x));
        }
        public override List<ActionEngine_Unit> value => m_ReturnVal;

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Math_Sort_GroupUnit GraphEventG = null;
#endif

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (GraphEventG is null)
            {
                GraphEventG = new GraphEvent_Math_Sort_GroupUnit();
                GraphEventG.GroupVal = (BluePrint_GroupUnit)m_GroupValue.Clone();
                GraphEventG.IsCheckValid = (BluePrint_Float)m_IsCheckValid.Clone();
                GraphEventG.SortType = (BluePrint_Bool)m_SortType.Clone();
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