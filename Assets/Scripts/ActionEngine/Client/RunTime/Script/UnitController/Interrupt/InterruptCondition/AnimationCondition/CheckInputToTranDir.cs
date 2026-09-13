using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class CheckInputToTranDir : IInterruptCondition
    {
        [SerializeField] private EContrast mContrast = EContrast.Less;
        [SerializeField] private int mContrastValue = 45;
        [SerializeField] private int mOffsetAngle = 0;

        #region property
        [EditorProperty("偏移角度", EditorPropertyType.EEPT_Int)]
        public int OffsetAngle
        {
            get { return mOffsetAngle; }
            set { mOffsetAngle = value; }
        }
        [EditorProperty("对比符号", EditorPropertyType.EEPT_Enum)]
        public EContrast Contrast
        {
            get { return mContrast; }
            set { mContrast = value; }
        }
        [EditorProperty("对比值", EditorPropertyType.EEPT_Int)]
        public int ContrastValue
        {
            get { return mContrastValue; }
            set { mContrastValue = value; }
        }
        #endregion

        public int InterruptType => (int)EConditionType.EIT_CheckInputToTranDir;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            // ActionStateMachine _stateMachine = actionStatePart.ActionStateMachine;
            // Vector3 _dir = _stateMachine.CurUnit.transform.TransformDirection(_stateMachine.PlayerInputMoveDir);
            // int _angleOffset = (int)(_stateMachine.CamRot.eulerAngles.y - Quaternion.LookRotation(_dir).eulerAngles.y);
            // if (_angleOffset > 180) _angleOffset -= 360;
            // else if (_angleOffset < -180) _angleOffset += 360;
            int _angleOffset = 0;
            if (actionStatePart.ActionStateMachine.TryGetStaticLogic(out Ex_InputToTransDir _dir, nameof(Ex_InputToTransDir)))
            {
                _angleOffset = (int)_dir.GetOffsetAngle(mOffsetAngle);
            }

            _angleOffset = Mathf.Abs(_angleOffset);
            // EngineDebug.LogWarning("角度： " + _angleOffset);
            if (Contrast == EContrast.Greater)
            {
                return _angleOffset > mContrastValue;
            }
            else if (Contrast == EContrast.Less)
            {
                return _angleOffset < mContrastValue;
            }
            else if (Contrast == EContrast.GreaterOrEquals)
            {
                return _angleOffset >= mContrastValue;
            }
            else if (Contrast == EContrast.LessOrEquals)
            {
                return _angleOffset <= mContrastValue;
            }

            return _angleOffset == mContrastValue;
        }

        public IInterruptCondition Clone()
        {
            CheckInputToTranDir _check = new CheckInputToTranDir();

            _check.Contrast = mContrast;
            _check.ContrastValue = mContrastValue;
            _check.OffsetAngle = mOffsetAngle;

            return _check;
        }
    }
}