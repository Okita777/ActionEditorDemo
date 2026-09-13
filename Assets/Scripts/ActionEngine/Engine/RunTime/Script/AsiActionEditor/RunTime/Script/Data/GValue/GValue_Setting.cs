using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_Setting
    {
        [UnityEngine.SerializeField] public List<GValue_SettingPar> parameter = new List<GValue_SettingPar>();
        [UnityEngine.SerializeField] public bool isOpen = true;

        public GValue_Setting Clone()
        {
#if UNITY_EDITOR
            GValue_Setting setting = new GValue_Setting();
            setting.parameter = new List<GValue_SettingPar>();
            foreach (GValue_SettingPar p in parameter) {
                //if (p.isGEquation)
                //{
                //    int g = p.GValueIndexID;
                //    int i = p.GValueGroupIndexID;
                //    string _str = "(GValue_Setting)->";
                //    if (g == 0 && i == 0) _str += "<color=#ff0000>当前公式为默认值</color>";
                //    else _str += $"[<color=#ffcc00>{g},{i}</color>]";
                //    _str += $"\n{EngineDebug.GetEventPath()}";
                //    EngineDebug.LogError($"读取到公式蓝图!!" + _str);
                //}
                //if (!Application.isPlaying)
                //{
                //    //GEquation GEquationVal = m_GraphEvent;
                //    //if (GEquationVal is null)
                //    //{
                //    //    EngineDebug.LogError($"<color=#ff0000>GV公式报空!!!</color>\n{EngineDebug.GetEventPath()}");
                //    //}
                //    //else
                //    {
                //        int g = p.GValueIndexID;
                //        int i = p.GValueIndexID;
                //        string _str = "(事件)->";
                //        if (g == 0 && i == 0) _str += "<color=#ff0000>当前公式为默认值</color>";
                //        else _str += $"[<color=#ffcc00>{g},{i}</color>]";
                //        _str += $"\n{EngineDebug.GetEventPath()}";
                //        EngineDebug.Log($"读取到公式蓝图!!" + _str);
                //    }
                //}

                setting.parameter.Add(p.Clone()); 
            }
            setting.isOpen = isOpen;
            return setting;
#endif
            return this;
        }

        public void OnSet(ActionStateMachine _actionStateMachine)
        {
            OnSet(_actionStateMachine, _actionStateMachine);
        }
        public void OnSet(ActionStateMachine _actionStateMachine, ActionStateMachine _selfActionStateMachine)
        {
            foreach (GValue_SettingPar GVS in parameter)
            {
                ushort _index = GVS.GValueIndexID;
                ushort _groupID = GVS.GValueGroupIndexID;
                //int _m_index = 0;
                switch (GVS.gValueType)
                {
                    case EGValueType.GBool:
                    {
                        bool _newVal = ((GVS_Bool)GVS.GValueValue).value;
                        SetBoolWithNotify(_actionStateMachine, _groupID, _index, _newVal);
                        break;
                    }
                    case EGValueType.GInt:
                        if (GVS.isGEquation)
                        {
                            ushort _eGroupID = GVS.GEquationGroupIndexID;
                            if (_selfActionStateMachine.Equations[_eGroupID].mGValueEquation[((GVS_Int)GVS.GValueValue).value] is GValueEquation.GValueEquation_IntPart _intEqPart)
                            {
                                SetIntWithNotify(_actionStateMachine, _groupID, _index,
                                    _intEqPart.Value(_selfActionStateMachine.FirstStatePart, new ActionMachineTime(0, 0, 0, 0)));
                            }
                            else if (_selfActionStateMachine.Equations[_eGroupID].mGValueEquation[((GVS_Int)GVS.GValueValue).value] is GValueEquation.GValueEquation_FloatPart _floatEqPart)
                            {
                                SetIntWithNotify(_actionStateMachine, _groupID, _index,
                                    (int)_floatEqPart.Value(_selfActionStateMachine.FirstStatePart, new ActionMachineTime(0, 0, 0, 0)));
                            }
                        }
                        else
                        {
                            SetIntWithNotify(_actionStateMachine, _groupID, _index, ((GVS_Int)GVS.GValueValue).value);
                        }
                        break;
                    case EGValueType.GFloat:
                        if (GVS.isGEquation)
                        {
                            ushort _eGroupID = GVS.GEquationGroupIndexID;
                            var _floatEqPart = (GValueEquation.GValueEquation_FloatPart)_selfActionStateMachine.Equations[_eGroupID].mGValueEquation[(int)((GVS_Float)GVS.GValueValue).value];
                            SetFloatWithNotify(_actionStateMachine, _groupID, _index,
                                _floatEqPart.Value(_selfActionStateMachine.FirstStatePart, new ActionMachineTime(0, 0, 0, 0)));
                        }
                        else
                        {
                            SetFloatWithNotify(_actionStateMachine, _groupID, _index, ((GVS_Float)GVS.GValueValue).value);
                        }
                        break;
                    case EGValueType.GString:
                        _actionStateMachine.GValuePool.SetString(_groupID, _index, ((GVS_String)GVS.GValueValue).value);
                        //_actionStateMachine.GValues[_groupID].mEngineString[_index] = ((GVS_String)GVS.GValueValue).value;
                        break;
                    case EGValueType.GEnum:
                        SetEnumWithNotify(_actionStateMachine, _groupID, _index, ((GVS_Enum)GVS.GValueValue).value);
                        break;
                    case EGValueType.GTransform:
                        //_m_index = _actionStateMachine.GValues[_groupID].mEngineTransform[_index];

                        //_m_index = _actionStateMachine.GValuePool.GetTransform(_groupID, _index);
                        //_m_index += ((GVS_Enum)GVS.GValueValue).value * 1000;
                        _actionStateMachine.RemoveTransform(_groupID, _index, ((GVS_Enum)GVS.GValueValue).value);
                        break;
                    case EGValueType.GUnit:
                        //_m_index = _actionStateMachine.GValues[_groupID].mEngineUnit[_index];

                        //_m_index = _actionStateMachine.GValuePool.GetUnit(_groupID, _index);
                        //_m_index += ((GVS_Enum)GVS.GValueValue).value * 1000;
                        _actionStateMachine.RemoveUnit(_groupID, _index, ((GVS_Enum)GVS.GValueValue).value);
                        break;
                    case EGValueType.GPoint:
                        //_m_index = _actionStateMachine.GValues[_groupID].mEnginePointData[_index];

                        //_m_index = _actionStateMachine.GValuePool.GetPointData(_groupID, _index);
                        //_m_index += ((GVS_Enum)GVS.GValueValue).value * 1000;
                        _actionStateMachine.RemovePoint(_groupID, _index, ((GVS_Enum)GVS.GValueValue).value);
                        break;
                }
            }
        }

        private static void SetBoolWithNotify(ActionStateMachine stateMachine, ushort group, ushort id, bool value)
        {
            bool old = stateMachine.GValuePool.GetBool(group, id);
            stateMachine.GValuePool.SetBool(group, id, value);
            stateMachine.SendChangeMessage_GBool(group, id, old, value);
        }

        private static void SetIntWithNotify(ActionStateMachine stateMachine, ushort group, ushort id, int value)
        {
            int old = stateMachine.GValuePool.GetInt(group, id);
            stateMachine.GValuePool.SetInt(group, id, value);
            stateMachine.SendChangeMessage_GInt(group, id, old, value);
        }

        private static void SetFloatWithNotify(ActionStateMachine stateMachine, ushort group, ushort id, float value)
        {
            float old = stateMachine.GValuePool.GetFloat(group, id);
            stateMachine.GValuePool.SetFloat(group, id, value);
            stateMachine.SendChangeMessage_GFloat(group, id, old, value);
        }

        private static void SetEnumWithNotify(ActionStateMachine stateMachine, ushort group, ushort id, byte value)
        {
            byte old = stateMachine.GValuePool.GetEnum(group, id);
            stateMachine.GValuePool.SetEnum(group, id, value);
            stateMachine.SendChangeMessage_GEnum(group, id, old, value);
        }
    }

    [System.Serializable]
    public class GValue_CopySetting
    {
        [UnityEngine.SerializeField] public List<GValue_CopySettingPar> parameter = new List<GValue_CopySettingPar>();
        [UnityEngine.SerializeField] public bool isOpen = true;

        public GValue_CopySetting Clone()
        {
#if UNITY_EDITOR
            GValue_CopySetting setting = new GValue_CopySetting();
            setting.parameter = new List<GValue_CopySettingPar>();
            foreach (GValue_CopySettingPar p in parameter)
            {
                setting.parameter.Add(p.Clone());
            }
            setting.isOpen = isOpen;
            return setting;
#endif
            return this;
        }

        public void OnSet(ActionStatePart _targetPart, ActionStatePart _readPart)
        {
            foreach (GValue_CopySettingPar par in parameter)
            {
                par.OnSet(_targetPart, _readPart);
            }
        }
    }

    [System.Serializable]
    public class GValue_CopySettingPar
    {
        public GValue_CopySettingPar(EGValueType _egValueType, GValue _writeValue, GValue _readValue)
        {
            gValueType = _egValueType;
            WriteValue = _writeValue;
            ReadValue = _readValue;
        }

        [UnityEngine.SerializeField] public EGValueType gValueType;
        [SerializeReference] public GValue WriteValue;
        [SerializeReference] public GValue ReadValue;

        public void OnSet(ActionStatePart _targetPart, ActionStatePart _readPart)
        {
            switch (gValueType)
            {
                case EGValueType.GBool:
                    ((GBool)WriteValue).SetValue(_targetPart, ((GBool)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GInt:
                    ((GInt)WriteValue).SetValue(_targetPart, ((GInt)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GFloat:
                    ((GFloat)WriteValue).SetValue(_targetPart, ((GFloat)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GString:
                    ((GString)WriteValue).SetValue(_targetPart, ((GString)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GEnum:
                    ((GEnum)WriteValue).SetValue(_targetPart, ((GEnum)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GTransform:
                    ((GTransform)WriteValue).SetValue(_targetPart, ((GTransform)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GPoint:
                    ((GPoint)WriteValue).SetValue(_targetPart, ((GPoint)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GUnit:
                    ((GUnit)WriteValue).SetValue(_targetPart, ((GUnit)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GGroupBool:
                    ((GGroupBool)WriteValue).SetValue(_targetPart, ((GGroupBool)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GGroupInt:
                    ((GGroupInt)WriteValue).SetValue(_targetPart, ((GGroupInt)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GGroupFloat:
                    ((GGroupFloat)WriteValue).SetValue(_targetPart, ((GGroupFloat)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GGroupString:
                    ((GGroupString)WriteValue).SetValue(_targetPart, ((GGroupString)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GGroupTransform:
                    ((GGroupTransform)WriteValue).SetValue(_targetPart, ((GGroupTransform)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GGroupPoint:
                    ((GGroupPoint)WriteValue).SetValue(_targetPart, ((GGroupPoint)ReadValue).GetValue(_readPart));
                    break;
                case EGValueType.GGroupUnit:
                    ((GGroupUnit)WriteValue).SetValue(_targetPart, ((GGroupUnit)ReadValue).GetValue(_readPart));
                    break;
            }
        }

        public GValue_CopySettingPar Clone()
        {
#if UNITY_EDITOR
            return new GValue_CopySettingPar(gValueType, WriteValue.Clone(), ReadValue.Clone());
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValue_SettingPar
    {
        public GValue_SettingPar(EGValueType _egValueType, GVS _gValue)
        {
            gValueType = _egValueType;
            GValueValue = _gValue;
        }
        [UnityEngine.SerializeField] public EGValueType gValueType;
        [UnityEngine.SerializeField] public ushort GValueIndexID;
        [UnityEngine.SerializeField] public ushort GValueGroupIndexID;
        [UnityEngine.SerializeField] public ushort GEquationGroupIndexID;
        [UnityEngine.SerializeField] public bool isGEquation = false;
        [SerializeReference] public GVS GValueValue;

        public GValue_SettingPar Clone()
        {
#if UNITY_EDITOR
            GValue_SettingPar setting = new GValue_SettingPar(gValueType, GValueValue.Clone());
            setting.GValueIndexID = GValueIndexID;
            setting.isGEquation = isGEquation;
            setting.GValueGroupIndexID = GValueGroupIndexID;
            setting.GEquationGroupIndexID = GEquationGroupIndexID;

            // if (isGEquation)
            // {
            //     // setting.GValueValue =
            // }
            return setting;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GVS_Bool : GVS
    {
        public GVS_Bool(bool _value = false)
        {
            value = _value;
        }
        [UnityEngine.SerializeField] public bool value;
        public override GVS Clone()
        {
            return new GVS_Bool(value);
        }
    }
    [System.Serializable]
    public class GVS_Int : GVS
    {
        public GVS_Int(int _value = 0)
        {
            value = _value;
        }
        [UnityEngine.SerializeField] public int value = 0;
        public override GVS Clone()
        {
            return new GVS_Int(value);
        }
    }

    [System.Serializable]
    public class GVS_Float : GVS
    {
        public GVS_Float(float _value = 0.0f)
        {
            value = _value;
        }
        [UnityEngine.SerializeField] public float value = 0.0f;
        public override GVS Clone()
        {
            return new GVS_Float(value);
        }
    }

    [System.Serializable]
    public class GVS_String : GVS
    {
        public GVS_String(string _value = "")
        {
            value = _value;
        }
        [UnityEngine.SerializeField] public string value;
        public override GVS Clone()
        {
            return new GVS_String(value);
        }
    }

    [System.Serializable]
    public class GVS_Enum : GVS
    {
        public GVS_Enum(byte _value = 0)//, ushort _groupID = 0
        {
            value = _value;
            //groupID =  _groupID;
        }
        [UnityEngine.SerializeField] public byte value;
        //[UnityEngine.SerializeField] public ushort groupID;
        public override GVS Clone()
        {
            return new GVS_Enum(value);
        }
    }

    [System.Serializable]
    public abstract class GVS
    {
        public abstract GVS Clone();
    }
}