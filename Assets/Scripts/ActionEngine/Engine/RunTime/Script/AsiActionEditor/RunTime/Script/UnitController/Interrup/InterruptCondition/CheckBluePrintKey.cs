using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 检查蓝图输入的自定义按键
    /// </summary>
    [System.Serializable]
    public class CheckBluePrintKey : IInterruptCondition
    {
        [SerializeField] private GraphEvent_NoValue_String mCheckKeyName = CreateDefaultCheckKeyName();
        [SerializeField] private EInputKeyType mInputType = EInputKeyType.OnDown;

        [EditorProperty("按键行为", EditorPropertyType.EEPT_GraphValue)]
        public GraphEvent_NoValue_String CheckKeyName
        {
            get => mCheckKeyName;
            set => mCheckKeyName = value;
        }

        [EditorProperty("按键类型", EditorPropertyType.EEPT_Enum)]
        public EInputKeyType InputType
        {
            get => mInputType;
            set => mInputType = value;
        }

        public int InterruptType => -(int)EInterruptTypeInternal.EIT_BluePrintKey;

        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            if (actionStatePart.DisableInput) return false;

            string checkKeyName = GetCheckKeyName(actionStatePart);
            if (mInputType == EInputKeyType.OnDown)
            {
                return checkKeyName == actionStatePart.NowInputDownKey;
            }
            else if (mInputType == EInputKeyType.OnUp)
            {
                return checkKeyName == actionStatePart.NowInputUpKey;
            }
            else if (mInputType == EInputKeyType.OnClick)
            {
                return checkKeyName == actionStatePart.NowInputClickKey;
            }
            else if (mInputType == EInputKeyType.Down_State)
            {
                return actionStatePart.NowInputKey.Contains(checkKeyName);
            }
            else if (mInputType == EInputKeyType.Up_State)
            {
                return !actionStatePart.NowInputKey.Contains(checkKeyName);
            }
            else
            {
                return checkKeyName == actionStatePart.NowInputHoldKey;
            }
        }

        public string GetCheckKeyName(ActionStatePart actionStatePart)
        {
            return mCheckKeyName.value(actionStatePart, new ActionMachineTime());
        }

        public IInterruptCondition Clone()
        {
            CheckBluePrintKey checkBluePrintKey = new CheckBluePrintKey();
            checkBluePrintKey.CheckKeyName = mCheckKeyName.Clone();
            checkBluePrintKey.InputType = mInputType;
            return checkBluePrintKey;
        }

        private static GraphEvent_NoValue_String CreateDefaultCheckKeyName()
        {
            return new GraphEvent_NoValue_String()
            {
                BluePrint_Val = new GraphEvent_LocalParam_Read_String() { ParamIndex = 0 },
                LocalStringParams = new List<string>() { string.Empty },
#if UNITY_EDITOR
                m_NodeEdiData = new NodeEdiData()
                {
                    nodeTitle = "按键行为",
                    localParams = new List<BluePrintLocalParamDef>()
                    {
                        new BluePrintLocalParamDef
                        {
                            paramName = "按键行为",
                            paramType = EBluePrintLocalParamType.String,
                            drawToInspector = true
                        },
                    }
                }
#endif
            };
        }
    }
}
