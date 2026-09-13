using System;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_LockUnit : MonoBehaviour
    {
        private ActionEngine_Unit mUnit;
        [SerializeField] protected string m_ActionName;
        [SerializeField] protected float m_Radius = 6;
        [SerializeField] protected float m_SelfAngle = 90;
        // [SerializeField] protected float m_LerpSpeed = 12;
        [SerializeField] protected int m_CheckLayer;
        // [SerializeField] protected int m_Priority;
        [SerializeField] protected bool m_ReferToCam = true;

        [NonSerialized] private int curLayer;
        #region Property
        [EditorProperty("锁定层级: ", EditorPropertyType.EEPT_LayerMask)]
        public int CheckLayer
        {
            get { return m_CheckLayer; }
            set { m_CheckLayer = value; }
        }
        [EditorProperty("按键行为: ", EditorPropertyType.EEPT_String)]
        public string ActionName
        {
            get { return m_ActionName; }
            set { m_ActionName = value; }
        }
        [EditorProperty("最大半径: ", EditorPropertyType.EEPT_Float)]
        public float Radius
        {
            get { return m_Radius; }
            set { m_Radius = value; }
        }
        [EditorProperty("最大角度差: ", EditorPropertyType.EEPT_Float)]
        public float SelfAngle
        {
            get { return m_SelfAngle; }
            set { m_SelfAngle = value; }
        }
        [EditorProperty("参考锁定方向至相机: ", EditorPropertyType.EEPT_Bool, LabelWidth = 120)]
        public bool ReferToCam
        {
            get { return m_ReferToCam; }
            set { m_ReferToCam = value; }
        }
        // [EditorProperty("旋转速度: ", EditorPropertyType.EEPT_Float)]
        // public float LerpSpeed
        // {
        //     get { return m_LerpSpeed; }
        //     set { m_LerpSpeed = value; }
        // }
        // [EditorProperty("旋转优先级: ", EditorPropertyType.EEPT_Int)]
        // public int Priority
        // {
        //     get { return m_Priority; }
        //     set { m_Priority = value; }
        // }
        #endregion
        private void Start()
        {
            mUnit = GetComponent<ActionEngine_Unit>();
            mUnit.ActionStateMachine.AddDwonAction(LcokFuntion);
            curLayer = mUnit.ActionStateMachine.GetLayer(m_CheckLayer);
        }

        private void LcokFuntion(string _dwonAction)
        {
            ActionStateMachine _actionStateMachine = mUnit.ActionStateMachine;

            // if (m_ActionName == _dwonAction)
            // {
            //     if (_actionStateMachine.IsLock)
            //     {
            //         _actionStateMachine.IsLock = false;
            //     }
            //     else
            //     {
            //         FindLock();
            //     }
            // }
        }

        // public void Update()
        // {
        //     if (mUnit.ActionStateMachine.AllActionStatePart[0].NowInputClickKey == m_ActionName)
        //     {
        //         
        //     }
        // }

        private void FindLock()
        {
            ActionStateMachine _actionStateMachine = mUnit.ActionStateMachine;
            Transform _center = _actionStateMachine.CurUnit.transform;
            Collider[] _colliders = Physics.OverlapSphere(_center.position, m_Radius, curLayer, QueryTriggerInteraction.Ignore);

            if (m_ReferToCam)
            {
                if (_actionStateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
                {
                    if (!_config.HelpPointDic.TryGetValue(ECharacteLimbType.Cam_Main, out _center))
                    {
                        EngineDebug.LogWarning("锁定运行错误：角色未配置相机挂点");
                    }
                }
                else
                {
                    EngineDebug.LogWarning("锁定运行错误：角色未配置相机挂点");
                }
            }

            Transform _findTarget = null;
            float _findMinAngle = 360;
            foreach (var _collider in _colliders)
            {

                Transform _transform = _collider.transform;
                if (_center != _transform)
                {
                    Vector3 _transDir = _transform.position - _center.position;
                    float _angleOffset = Vector3.Angle(_center.forward, _transDir.normalized) * 2;
                    if (_angleOffset < _findMinAngle)
                    {
                        _findMinAngle = _angleOffset;
                        _findTarget = _transform;
                    }
                }
            }

            if (_findMinAngle < m_SelfAngle)
            {
                EngineDebug.Log("触发锁定");

                // _actionStateMachine.IsLock = true;
                _actionStateMachine.LockTransform = _findTarget;
            }
        }
    }
}