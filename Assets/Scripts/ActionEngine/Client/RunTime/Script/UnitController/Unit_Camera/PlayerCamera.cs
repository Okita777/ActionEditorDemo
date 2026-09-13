using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiActionEditor_Ex.RunTime
{
    //角色相机点位控制
    public class PlayerCamera : CameraControl
    {
        public float m_LerpSpeed_PosY = 0;
        public bool CamRot = true;

        private float m_DefaultPosY;
        private float m_LerpPosY;
        private Transform m_DefaultTrans;
        public override void OnInit(Transform _lookTarget, ActionStateMachine _stateMachine, int _defaulCam)
        {
            base.OnInit(_lookTarget, _stateMachine, _defaulCam);

            if (_stateMachine == null) return;
            if (_stateMachine.TryGetComponent(out CharacterConfig _config, nameof(CharacterConfig)))
            {
                if (_config.HelpPointDic.TryGetValue(ECharacteLimbType.Cam_Main, out Transform _point))
                {
                    m_DefaultTrans = _point;
                    m_DefaultPosY = m_DefaultTrans.localPosition.y;
                    m_LerpPosY = _point.position.y;
                }
            }
        }

        public override void OnUpdate(float _deltaTime)
        {
            if (m_LerpSpeed_PosY > 0)
            {
                Transform _player = ActionEngineManager_Input.Instance.Player.transform;
                float _targetPosY = _player.position.y + m_DefaultPosY;
                m_LerpPosY = Mathf.Lerp(m_LerpPosY, _targetPosY, _deltaTime * m_LerpSpeed_PosY);
                m_DefaultTrans.localPosition = Vector3.up * (m_LerpPosY - _player.position.y);
                // m_LerpPosY - _player.position.y
            }

            // EngineDebug.Log("lookTarget: " + lookTarget.parent.GetInstanceID());

            if (CamRot)
                lookTarget.rotation = stateMachine.GetCamPointRot();
            else
                lookTarget.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}