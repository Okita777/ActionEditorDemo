using UnityEngine;
#if Cinemachine
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;
#else
        using Cinemachine;
#endif
#endif

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 针对Cinemachine相机搭建的基类，不会包含任何相机效果
    /// </summary>
    public abstract class CameraControl : MonoBehaviour
    {
        [HideInInspector] public int CamID = -1;
        [HideInInspector] public Transform followTarget_cam, lookTarget_cam;
        [HideInInspector] public Transform lookTarget;
        public ActionStateMachine stateMachine;
        public Behaviour[] allCinemachine = new Behaviour[0];//相机组

        private float m_AmplitudeGain, m_FrequencyGain;
        private Vector3 m_PivotOffset;
        // public bool isLock { get; private set; }
#if Cinemachine
        public CinemachineBasicMultiChannelPerlin perlin { get; private set; }
#endif
        public virtual void OnInit(Transform _lookTarget, ActionStateMachine _stateMachine)
        {
            followTarget_cam = _lookTarget;
            lookTarget_cam = _lookTarget;
            lookTarget = _lookTarget;
            stateMachine = _stateMachine;
        }
        public virtual void OnInit(Transform _lookTarget, ActionStateMachine _stateMachine, int _defaulCam)
        {
            OnReset();
            followTarget_cam = _lookTarget;
            lookTarget_cam = _lookTarget;
            lookTarget = _lookTarget;
            stateMachine = _stateMachine;
#if Cinemachine

            // if (_updateTarget)
            {
                // EngineDebug.Log("修改注视对象2");

                foreach (var _behaviour in allCinemachine)
                {
                    if (_behaviour is CinemachineVirtualCameraBase _cinemachine)
                    {
                        _cinemachine.Follow = _lookTarget;
                        _cinemachine.LookAt = _lookTarget;
                    }
                }
            }

            if (allCinemachine[_defaulCam] is CinemachineVirtualCamera _cinemachineVirtualCamera)
            {
                perlin = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                // EngineDebug.Log("储存了： " + (perlin is not null));
            }

            allCinemachine[_defaulCam].gameObject.SetActive(false);
            allCinemachine[_defaulCam].gameObject.SetActive(true);

            CamID = _defaulCam;
#endif

        }



        public virtual void OnReset()
        {
#if Cinemachine
            foreach (var _behaviour in allCinemachine)
            {
                if (_behaviour is CinemachineVirtualCameraBase _cinemachine)
                {
                    _cinemachine.gameObject.SetActive(false);
                }
            }
#endif
        }

        public virtual void ChangeCam(int _id, Transform _camPoint, Transform _lookAt)
        {
            // isLock = false;
            if (CamID > -1)
            {
#if Cinemachine
                // EngineDebug.Log($"进来了？ {lastCamID}  {_id}");

                if (CamID != _id)
                {
                    if (allCinemachine[_id] is CinemachineVirtualCameraBase _cinemachine)
                    {
                        // EngineDebug.Log("修改注视对象");
                        followTarget_cam = _camPoint;
                        lookTarget_cam = _lookAt;
                        // lookTarget = _lookAt;
                        _cinemachine.Follow = _camPoint;
                        _cinemachine.LookAt = _lookAt;
                    }
                    allCinemachine[_id].gameObject.SetActive(true);
                    allCinemachine[CamID].gameObject.SetActive(false);

                    if (allCinemachine[CamID] is CinemachineVirtualCamera _cinemachineVirtualCameraold)
                    {
                        var _Perlin = _cinemachineVirtualCameraold.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                        if (_Perlin is not null)
                        {
                            //读取  恢复

#if UNITY_6000_0_OR_NEWER
                            _Perlin.AmplitudeGain = m_AmplitudeGain;
                            _Perlin.FrequencyGain = m_FrequencyGain;
                            _Perlin.PivotOffset = m_PivotOffset;
#else
                            _Perlin.m_AmplitudeGain = m_AmplitudeGain;
                            _Perlin.m_FrequencyGain = m_FrequencyGain;
                            _Perlin.m_PivotOffset = m_PivotOffset;
#endif

                        }
                    }
                    if (allCinemachine[_id] is CinemachineVirtualCamera _cinemachineVirtualCamera)
                    {
                        var _Perlin = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                        perlin = _Perlin;
                        // EngineDebug.Log("储存了");
                        if (_Perlin is not null)
                        {
                            //储存
#if UNITY_6000_0_OR_NEWER
                            m_AmplitudeGain = _Perlin.AmplitudeGain;
                            m_FrequencyGain = _Perlin.FrequencyGain;
                            m_PivotOffset = _Perlin.PivotOffset;
#else
                            m_AmplitudeGain = _Perlin.m_AmplitudeGain;
                            m_FrequencyGain = _Perlin.m_FrequencyGain;
                            m_PivotOffset = _Perlin.m_PivotOffset;
#endif
                        }
                    }
                    // else
                    // {
                    //     // EngineDebug.Log("未储存");
                    // }
                    CamID = _id;
                }
                // else
                // {
                //     if (allCinemachine[_id] is CinemachineVirtualCameraBase _cinemachine)
                //     {
                //         _cinemachine.Follow = _camPoint;
                //         _cinemachine.LookAt = _camPoint;
                //     }
                //     allCinemachine[_id].gameObject.SetActive(false);
                //     allCinemachine[_id].gameObject.SetActive(true);
                // }
#endif
            }
        }
        public virtual void ChangeCam(int _id, Transform _camPoint)
        {
            ChangeCam(_id, _camPoint, _camPoint);
            // isLock = true;
            //             if (lastCamID > -1)
            //             {
            // #if Cinemachine
            //                 if (lastCamID != _id)
            //                 {
            //                     // if (allCinemachine[lastCamID] is CinemachineVirtualCamera _cinemachineVirtualCameraold)
            //                     // {
            //                     //     perlin =_cinemachineVirtualCameraold.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            //                     // }
            //                     //
            //                     // if (allCinemachine[_id] is CinemachineVirtualCameraBase _cinemachine)
            //                     // {
            //                     //     _cinemachine.Follow = _camPoint;
            //                     //     _cinemachine.LookAt = _lookAt;
            //                     // }
            //                     // allCinemachine[_id].gameObject.SetActive(true);
            //                     // allCinemachine[lastCamID].gameObject.SetActive(false);
            //                     
            //                     lastCamID = _id;
            //                 }
            //                 // else
            //                 // {
            //                 //     if (allCinemachine[_id] is CinemachineVirtualCameraBase _cinemachine)
            //                 //     {
            //                 //         _cinemachine.Follow = _camPoint;
            //                 //         _cinemachine.LookAt = _lookAt;
            //                 //     }
            //                 //     allCinemachine[_id].gameObject.SetActive(false);
            //                 //     allCinemachine[lastCamID].gameObject.SetActive(true);
            //                 // }
            // #endif
            //             }
        }

        public virtual void ChangeCam(int _id)
        {
            if (CamID > -1)
            {
                if (CamID != _id)
                {
                    // EngineDebug.Log("切换相机");
                    allCinemachine[_id].gameObject.SetActive(true);
                    allCinemachine[CamID].gameObject.SetActive(false);

                    CamID = _id;
                }
            }
        }
        public abstract void OnUpdate(float _deltaTime);
    }
}