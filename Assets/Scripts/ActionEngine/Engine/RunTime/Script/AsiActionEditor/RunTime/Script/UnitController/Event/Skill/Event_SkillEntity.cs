using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class Event_SkillEntity : IActionEventData
    {
        [SerializeField] protected string mSkillEntity;
        [SerializeField]
        protected GraphEvent_NoValue_Vector3 mLocalScale = CreateDefaultLocalScale();

        private static GraphEvent_NoValue_Vector3 CreateDefaultLocalScale()
        {
            return new GraphEvent_NoValue_Vector3()
            {
                BluePrint_Val = new GraphEvent_Make_Vector3()
                {
                    X = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 0 },
                    Y = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 1 },
                    Z = new GraphEvent_LocalParam_Read_Float() { ParamIndex = 2 },
                },
                LocalFloatParams = new List<float>() { 1.0f, 1.0f, 1.0f },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "缩放",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef { paramName = "X", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                        new BluePrintLocalParamDef { paramName = "Y", paramType = EBluePrintLocalParamType.Float, drawToInspector = true },
                        new BluePrintLocalParamDef { paramName = "Z", paramType = EBluePrintLocalParamType.Float, drawToInspector = true }
                    }
                }
#endif
            };
        }

        #region property
        [EditorProperty("实体实例", EditorPropertyType.EEPT_GameObject, Required = true)]
        public string SkillEntity
        {
            get { return mSkillEntity; }
            set { mSkillEntity = value; }
        }
        [EditorProperty("缩放", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_Vector3 LocalScale
        {
            get
            {
                if (mLocalScale is null)
                    mLocalScale = CreateDefaultLocalScale();
                return mLocalScale;
            }
            set { mLocalScale = value; }
        }
        #endregion
        public int GetEvenType() => -(int)EEvenTypeInternal.EET_SkillEntity;

        public IActionEventData Creact()
        {
            Event_SkillEntity _skill = new Event_SkillEntity();
            //EngineDebug.LogWarning("实例技能实体: " + _skill.GetHashCode());
            return _skill;
        }

        [NonSerialized] private bool isDestory = false;
        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //EngineDebug.LogError("创建事件");

            //mMain = null;
            isDestory = false;

            if (_isSingle)
            {
#if UNITY_EDITOR
                EngineDebug.LogError($"[<color=#ffcc00>{_actionState.CurrentActionState.Name}</color>]  \n实体实例错误！！ 实体实例不能为单帧 ");
#endif
                return;
            }

            ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            EngineResourcesManager.Instance.CreactObjToComponent<Transform>(mSkillEntity, (Component _obj) =>
            {
                //mMain = _obj.transform;
                int _instanceId = _actionState.GetHashCode();
                int _eventID = GetHashCode();
                stateMachine.InstanceComponent_Add(_instanceId, _eventID, _obj.transform);

                if (isDestory && stateMachine.InstanceComponent_TryGet(_instanceId, _eventID, out _obj))
                {
                    //EngineDebug.LogError("初始化执行销毁");
                    _obj.gameObject.SetActive(false);
                    EngineResourcesManager.Instance.RemoveComponent(mSkillEntity, _obj.transform);
                    stateMachine.InstanceComponent_Remove(_instanceId, _eventID);
                    return;
                }

                if (_actionState.IsTem)
                {
                    _obj.transform.SetPositionAndRotation(_actionState.Pos, _actionState.Rot);
                }
                else
                {
                    Transform _unit = _actionState.ActionStateMachine.CurUnit.transform;
                    _obj.transform.SetPositionAndRotation(_unit.position, _unit.rotation);
                }
                _obj.transform.localScale = LocalScale.value(_actionState, EngineResourcesManager.Instance.MachineTime);
            }, 3, 30, -1, 2, false);
        }

        public void LateUpdate(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            ActionStateMachine stateMachine = _actionState.ActionStateMachine;

            if (stateMachine.InstanceComponent_TryGet(_actionState.GetHashCode(), GetHashCode(), out Component _obj))
            {
                Transform mMain = _obj.transform;
                if (mMain is null) return;

                if (_actionState.IsTem)
                {
                    mMain.SetPositionAndRotation(_actionState.Pos, _actionState.Rot);
                }
                else
                {
                    Transform _unit = _actionState.ActionStateMachine.CurUnit.transform;
                    mMain.SetPositionAndRotation(_unit.position, _unit.rotation);
                }
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            //EngineDebug.LogError("销毁事件");

            ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            if (stateMachine.InstanceComponent_TryGet(_actionState.GetHashCode(), GetHashCode(), out Component _obj))
            {
                //EngineDebug.LogError("尝试销毁");

                EngineResourcesManager.Instance.RemoveComponent(mSkillEntity, _obj.transform);
                stateMachine.InstanceComponent_Remove(_actionState.GetHashCode(), GetHashCode());
            }
            else
            {
                //EngineDebug.LogError("未正常销毁！！， 转初始化那边销毁");
                isDestory = true;
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SkillEntity eventCast = _eventData as Event_SkillEntity;
            eventCast.mSkillEntity = mSkillEntity;
            eventCast.mLocalScale = LocalScale.Clone();
            return eventCast;
        }
    }
}