using System;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class RayCastData
    {
        //基础参数
        public GraphEvent_NoValue_Point m_point = new GraphEvent_NoValue_Point();
        // public SelectTransform m_SelectTransform = new SelectTransform();
        // public EVector3 m_OffsetPosition = new EVector3();
        // public EVector3 m_OffsetRotation = new EVector3();
        // public float m_Distance;
        public GraphEvent_NoValue_Float m_length = new GraphEvent_NoValue_Float(1);
        public int m_LayerMask;

        //写入参数
        public GValue_SetBool m_SetGBool = new GValue_SetBool();
        public GValue_SetTransform m_SetGtransform = new GValue_SetTransform();
        public GValue_SetPoint m_SetPoint = new GValue_SetPoint();
        public GValue_SetFloat m_SetFloat = new GValue_SetFloat();
        public GValue_SetString m_SetString = new GValue_SetString();
        public GValue_SetString m_SetString2 = new GValue_SetString();


        // //衍生列表
        // public RayCastData[] m_CastData;
        // public RayCastData[] m_CastData_NotHit;

        //仅Editor显示用
        public bool m_Opne_SetGValue = false;
        // public bool m_Opne_CastData = false;
        // public bool m_Opne_CastData_NotHit = false;

        [NonSerialized] private ActionStateMachine m_ActionStateMachine;

        public RayCastData()
        {
            // m_CastData = Array.Empty<RayCastData>();
            // m_CastData_NotHit = Array.Empty<RayCastData>();

            m_SetGtransform.m_IsSet = false;
            m_SetPoint.m_IsSet = false;
            m_SetFloat.m_IsSet = false;
            m_SetString.m_IsSet = false;
            m_SetString2.m_IsSet = false;
        }

        public RayCastData(GraphEvent_NoValue_Point _point, GraphEvent_NoValue_Float _length, LayerMask _layerMask)
        {
            m_point = _point;
            m_length = _length;
            // m_SelectTransform = _SelectTransform;
            // m_OffsetPosition = _OffsetPosition;
            // m_OffsetRotation = _OffsetRotation;
            // m_Distance = _Distance;
            m_LayerMask = _layerMask;
            // m_CastData = Array.Empty<RayCastData>();
            // m_CastData_NotHit = Array.Empty<RayCastData>();

            m_SetGtransform.m_IsSet = false;
            m_SetPoint.m_IsSet = false;
            m_SetFloat.m_IsSet = false;
            m_SetString.m_IsSet = false;
            m_SetString2.m_IsSet = false;
        }

        // public void Init(ActionStateMachine _actionStateMachine)
        // {
        //     m_ActionStateMachine = _actionStateMachine;
        //     if (m_SetGBool.m_IsSet)
        //     {
        //         m_SetGBool.Init(_actionStateMachine);
        //         if (m_SetGBool.m_IsSet)
        //             m_SetGBool.Set(false);
        //     }
        //
        //     // foreach (RayCastData _castData in m_CastData)
        //     //     _castData.Init(_actionStateMachine);
        //     // foreach (RayCastData _castData in m_CastData_NotHit)
        //     //     _castData.Init(_actionStateMachine);
        // }

        public void OnCheck(ActionStatePart _actionStatepart, ActionMachineTime _actionMachineTime)
        {
            if (m_SetGBool.m_IsSet)
            {
                if (m_SetGBool.m_IsSet)
                    m_SetGBool.Set(_actionStatepart, false);
            }

            // m_SelectTransform.Init(m_ActionStateMachine);
            // if (m_SelectTransform.IsValid(_characterConfig))
            {
                PointData _point = m_point.value(_actionStatepart, _actionMachineTime);
                float _length = m_length.value(_actionStatepart, _actionMachineTime);
                // Transform _target = m_SelectTransform.Get(_characterConfig);
                Vector3 _pos = _point.pos;
                Vector3 _rot = _point.rot * Vector3.forward;

                if (Physics.Raycast(_pos, _rot, out RaycastHit _hit, _length, m_LayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (m_SetGBool.m_IsSet)
                    {
                        m_SetGBool.Set(_actionStatepart, true);
                    }
                    if (m_SetGtransform.m_IsSet)
                    {
                        m_SetGtransform.Set(_actionStatepart, _hit.transform);
                    }
                    if (m_SetPoint.m_IsSet)
                    {
                        m_SetPoint.Set(_actionStatepart, new PointData(_hit.point, Quaternion.LookRotation(_hit.normal)));
                    }
                    if (m_SetFloat.m_IsSet)
                    {
                        m_SetFloat.Set(_actionStatepart, _hit.distance);
                    }
                    if (m_SetString.m_IsSet)
                    {
                        m_SetString.Set(_actionStatepart, _hit.transform.name);
                    }
                    if (m_SetString2.m_IsSet)
                    {
                        if (_hit.collider.sharedMaterial is not null)
                        {
                            m_SetString2.Set(_actionStatepart, _hit.collider.sharedMaterial.name);
                        }
                    }

                    // foreach (RayCastData _castData in m_CastData)
                    // {
                    //     _castData.OnCheck(_characterConfig);
                    // }
                }
                else
                {
                    if (m_SetGBool.m_IsSet)
                    {
                        m_SetGBool.Set(_actionStatepart, false);
                    }
                    // foreach (RayCastData _castData in m_CastData_NotHit)
                    // {
                    //     _castData.OnCheck(_characterConfig);
                    // }
                }
            }
        }

        public RayCastData Clone()
        {
#if UNITY_EDITOR
            RayCastData _castData = new RayCastData(m_point.Clone(), m_length.Clone(), m_LayerMask);
            // _castData.m_CastData = new RayCastData[m_CastData.Length];
            // _castData.m_CastData_NotHit = new RayCastData[m_CastData_NotHit.Length];
            //
            // for (int i = 0; i < m_CastData.Length; i++)
            //     _castData.m_CastData[i] = m_CastData[i].Clone();
            // for (int i = 0; i < m_CastData_NotHit.Length; i++)
            //     _castData.m_CastData_NotHit[i] = m_CastData_NotHit[i].Clone();

            _castData.m_Opne_SetGValue = m_Opne_SetGValue;
            // _castData.m_Opne_CastData = m_Opne_CastData;
            // _castData.m_Opne_CastData_NotHit = m_Opne_CastData_NotHit;

            _castData.m_SetGBool = m_SetGBool.Clone();
            _castData.m_SetGtransform = m_SetGtransform.Clone();
            _castData.m_SetPoint = m_SetPoint.Clone();
            _castData.m_SetFloat = m_SetFloat.Clone();
            _castData.m_SetString = m_SetString.Clone();
            _castData.m_SetString2 = m_SetString2.Clone();

            return _castData;
#endif
            return this;
        }
    }
}