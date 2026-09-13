using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    [System.Serializable]
    //带参的Vector3
    public class GraphEvent_Value_Transform : BluePrint_Transform
    {
        [SerializeReference] protected BluePrint_Unit m_UnitVal = new GraphEvent_Value_SelfUnit();
        [SerializeField] protected int m_IntVal = 0;
        #region Property
        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit UnitVal
        {
            get { return m_UnitVal; }
            set { m_UnitVal = value; }
        }
        [EditorGraphProperty("挂点", false, EditorGraphPropertyType.EEPT_CharacteLimbTypeAll)]
        public int IntVal
        {
            get { return m_IntVal; }
            set { m_IntVal = value; }
        }

        #endregion

        public override Transform value => m_ReturnVal;

        [System.NonSerialized] private Transform m_ReturnVal;
        public override void Init(ActionStatePart part, ActionMachineTime _time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            //if (!part.ActionStateMachine.IsLocalClient) m_ReturnVal = part.ActionStateMachine.CurUnit.GetUnit().transform;
            m_ReturnVal = null;
            if (m_UnitVal.IsNode)
            {
                m_UnitVal.Init(part, _time);
                if (!m_UnitVal.isValid(part))
                {
                    m_ReturnVal = part.ActionStateMachine.CurUnit.transform;
                    //EngineDebug.LogError("出错啦！！！  单位获取为空: " + m_UnitVal.GetType().Name + "\n出错的Action：" + part.CurrentActionState.Name);
                    return;
                }

                ActionEngine_Unit _curUnit = m_UnitVal.value.GetUnit();
                if (_curUnit.Channel == ERuntimeDataChannel.Server)
                {
                    m_ReturnVal = _curUnit.transform;
                    return;
                }
#if UNITY_EDITOR
                bool _isError = true;
                if (m_UnitVal.value is null)
                    EngineDebug.LogError("报错对象_<color=#ff0000>A</color>");
                else if (m_UnitVal.value is null)
                    EngineDebug.LogError("报错对象_<color=#ff0000>B</color>");
                else if (m_UnitVal.value.GetUnit() is null)
                    EngineDebug.LogError("报错对象_<color=#ff0000>C</color>");
                else if (m_UnitVal.value.GetUnit().ActionStateMachine is null)
                    EngineDebug.LogError("报错对象_<color=#ff0000>D</color>");
                else _isError = false;
                if (_isError)
                {
                    EngineDebug.LogError($"报错对象路径_\n<color=#ff0000>{EngineDebug.DebugActionStatePart(part)}</color>");
                }
#endif

                if (m_UnitVal.value.GetUnit().ActionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                {
                    if (!_config.HelpPointDic.TryGetValue((ECharacteLimbType)m_IntVal, out m_ReturnVal))
                    {

                    }
                }
                else
                {
                    m_ReturnVal = m_UnitVal.value.GetUnit().transform;
                }

                //if (part.IsTem)
                //{
                //    EngineDebug.LogError($"获取单位挂点[{m_UnitVal.value.gameObject}] [{m_ReturnVal.position}]");
                //}
            }
            else
            {
                ActionStateMachine _stateMachine = part.ActionStateMachine;
                if (_stateMachine.CurUnit.Channel == ERuntimeDataChannel.Server)
                {
                    m_ReturnVal = _stateMachine.CurUnit.transform;
                    return;
                }
                if (_stateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                {
                    if (!_config.HelpPointDic.TryGetValue((ECharacteLimbType)m_IntVal, out m_ReturnVal))
                    {

                    }
                }
            }
        }

#if UNITY_EDITOR
        //保存时避免重复实例化，导致引用地址变更
        [System.NonSerialized] protected GraphEvent_Value_Transform _graphEvent = null;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (_graphEvent is null)
            {
                _graphEvent = new GraphEvent_Value_Transform();
                _graphEvent.UnitVal = (BluePrint_Unit)m_UnitVal.Clone();
                _graphEvent.IntVal = m_IntVal;
                _graphEvent.IsNode = IsNode;
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