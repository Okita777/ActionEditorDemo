using System;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 检查自定义按键
    /// </summary>
    [System.Serializable]
    public class CheckCostomKey : IInterruptCondition
    {
        [SerializeField] private string mCheckKeyName = String.Empty;
        [SerializeField] private EInputKeyType mInputType = EInputKeyType.OnDown;

        // public bool LockRefresh { get; set; }

        public string CheckKeyName
        {
            get { return mCheckKeyName; }
            set { mCheckKeyName = value; }
        }
        public EInputKeyType InputType
        {
            get { return mInputType; }
            set { mInputType = value; }
        }
        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckInput;


        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            if (actionStatePart.DisableInput) return false;
            if (mInputType == EInputKeyType.OnDown)
            {
                return mCheckKeyName == actionStatePart.NowInputDownKey;
            }
            else if (mInputType == EInputKeyType.OnUp)
            {
                return mCheckKeyName == actionStatePart.NowInputUpKey;
            }
            else if (mInputType == EInputKeyType.OnClick)
            {
                return mCheckKeyName == actionStatePart.NowInputClickKey;
            }
            else if (mInputType == EInputKeyType.Down_State)
            {
                return actionStatePart.NowInputKey.Contains(mCheckKeyName);
            }
            else if (mInputType == EInputKeyType.Up_State)
            {
                return !actionStatePart.NowInputKey.Contains(mCheckKeyName);
            }
            else
            {
                return mCheckKeyName == actionStatePart.NowInputHoldKey;
            }
        }

        public IInterruptCondition Clone()
        {
            CheckCostomKey _CheckCostomKey = new CheckCostomKey();
            _CheckCostomKey.CheckKeyName = mCheckKeyName;
            _CheckCostomKey.InputType = mInputType;
            return _CheckCostomKey;
        }
    }
}