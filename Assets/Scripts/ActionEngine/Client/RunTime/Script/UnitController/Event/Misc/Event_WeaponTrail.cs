using System;
using AsiActionEngine.RunTime;

#if AraTrail
using Ara;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_WeaponTrail : IActionEventData
    {
        [SerializeField] protected string m_TrailPath;
        [SerializeField] protected bool m_SetToPoint = false;
        [SerializeField] protected int m_HelpPoint = 0;

        [SerializeField] protected float m_DelayTime = 0.15f;
        [SerializeField] protected float m_InitTime = 0.15f;
        // [SerializeField] protected int m_PartPointType;
        [SerializeField] protected GEnum m_PropGType = new GEnum();

        #region Properties

        [EditorProperty("武器挂载到挂点", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool SetToPoint
        {
            get => m_SetToPoint;
            set => m_SetToPoint = value;
        }
        [EditorProperty("挂点", EditorPropertyType.EEPT_CharacteLimbType)]
        public int HelpPoint
        {
            get => m_HelpPoint;
            set => m_HelpPoint = value;
        }

        [EditorProperty("武器槽位类型", EditorPropertyType.EEPT_Enum)]
        public GEnum PropGType
        {
            get => m_PropGType;
            set => m_PropGType = value;
        }

        [EditorProperty("刀光特效", EditorPropertyType.EEPT_GameObject)]
        public string TrailPath
        {
            get => m_TrailPath;
            set => m_TrailPath = value;
        }

        [EditorProperty("初始化时长", EditorPropertyType.EEPT_Float)]
        public float InitTime
        {
            get => m_InitTime;
            set => m_InitTime = value;
        }

        [EditorProperty("脱手后延迟销毁时长", EditorPropertyType.EEPT_Float)]
        public float DelayTime
        {
            get => m_DelayTime;
            set => m_DelayTime = value;
        }

        // [EditorProperty("目标挂点", EditorPropertyType.EEPT_CharacteLimbType)]
        // public int PartPointType
        // {
        //     get => m_PartPointType;
        //     set => m_PartPointType = value;
        // }

        #endregion

        public int GetEvenType() => (int)EEvenType.EET_WeaponTrail;
        public IActionEventData Creact() => new Event_WeaponTrail();

        [NonSerialized] private AraTrail m_Trail;
        [NonSerialized] private Transform m_Parent;

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //return;
            m_Parent = null;
            //EngineDebug.LogError("刀光事件进入");
            if (string.IsNullOrEmpty(m_TrailPath))
            {
                return;
            }
            if (SetToPoint)
            {
                if (_actionState.ActionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                {
                    if (_config.HelpPointDic.TryGetValue((ECharacteLimbType)HelpPoint, out Transform _target))
                    {
                        m_Parent = _target;
                    }
                }
            }
            EngineResourcesManager.Instance.CreactObjToComponent<AraTrail>(m_TrailPath, trail =>
            {
                m_Trail = trail as AraTrail;
                if (m_Trail == null)
                {
                    EngineDebug.LogError($"[Event_WeaponTrail] 刀光特效加载失败，类型转换异常: " +
                        $"路径=[{m_TrailPath}], 返回类型=[{(trail != null ? trail.GetType().FullName : "null")}], 期望类型=[AraTrail]");
                    return;
                }
                //var weaponByte = m_PropGType.GetValue(_actionState);
                ActionEngine_Unit _source = _actionState.ActionStateMachine.CurUnit.GetSource;
                ActionStateMachine stateMachine = _source.ActionStateMachine;//获取源

                //todo: 从槽位获取武器!!
                //if (!SetToPoint && stateMachine.GetPropWarpToCell(m_PropGType, out var itemWarp))
                //{
                //    m_Parent = itemWarp._prop.weaponTrailRoot;
                //}
                // trail.transform.SetParent(itemWarp._prop.weaponTrailRoot, false);
                if (!SetToPoint && _source is ActionEngine_Entity _Entity)
                {
                    if (_Entity.TryGetPropToSlotID(m_PropGType.mSerValue, out var itemWarp))
                    {
                        m_Parent = itemWarp.weaponTrailRoot;
                    }
                }

                if (m_Parent is null) m_Parent = stateMachine.CurUnit.transform;
                trail.transform.position = m_Parent.position;
            }, 3, 10, m_DelayTime, (int)EObjPoolParent.Effects);
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)//interruot 是否因为打断轨退出
        {
            m_Trail = null;
            m_Parent = null;
        }

        public void LateUpdate(ActionStatePart _actionState, ActionMachineTime _actionTime)
        {
            if (m_Trail is not null && m_Parent is not null)
            {
                m_Trail.transform.position = m_Parent.position;
                m_Trail.transform.rotation = m_Parent.rotation;
                EngineResourcesManager.Instance.ResetLife(m_Trail, m_DelayTime);
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_WeaponTrail _event = _eventData as Event_WeaponTrail;

            _event.SetToPoint = m_SetToPoint;
            _event.HelpPoint = m_HelpPoint;

            _event.TrailPath = m_TrailPath;
            _event.InitTime = m_InitTime;
            _event.DelayTime = m_DelayTime;
            // _event.PartPointType = m_PartPointType;
            _event.PropGType = (GEnum)m_PropGType.Clone();

            return _event;
        }
    }
}
#else
namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_WeaponTrail : IActionEventData
    {
        public int GetEvenType() => (int)EEvenType.EET_WeaponTrail;

        public IActionEventData Creact()
        {
            throw new NotImplementedException();
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            throw new NotImplementedException();
        }
    }
}
#endif