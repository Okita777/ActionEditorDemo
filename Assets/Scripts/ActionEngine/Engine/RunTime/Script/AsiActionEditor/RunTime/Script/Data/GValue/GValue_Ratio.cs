using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GValue_Ratio
    {
        [SerializeField] public List<GValue_RatioPart> GValue_RatioPart = new List<GValue_RatioPart>();
        [SerializeField] public bool isOpen = true;
        [SerializeField] public bool checkAll = true;

        public bool CheckValue(ActionStateMachine stateMachine)
        {
            return CheckValue(stateMachine, stateMachine);
        }
        public bool CheckValue(ActionStateMachine stateMachine_L, ActionStateMachine stateMachine_R)
        {
            if (GValue_RatioPart.Count == 0) return true;

            foreach (var _now in GValue_RatioPart)
            {
                bool _isValid = CheckPart(stateMachine_L, stateMachine_R, _now);
                if (checkAll)
                {
                    if (!_isValid) return false;
                }
                else
                {
                    if (_isValid) return true;
                }
            }

            return checkAll;
        }

        private bool CheckPart(ActionStateMachine stateMachine_L, ActionStateMachine stateMachine_R, GValue_RatioPart _now)
        {
            // for (int i = 0; i < GValue_RatioPart.Count; i++)
            {
                ActionStatePart part_L = stateMachine_L.FirstStatePart;
                ActionStatePart part_R = stateMachine_R.FirstStatePart;
                // GValue_RatioPart _now = GValue_RatioPart[i];
                // _now.GValue_L.Init(stateMachine_L);
                // _now.GValue_R.Init(stateMachine_R);
                if (_now.GValueType == EGValueType.GBool)
                {
                    bool _ratio = _now.Ratio == 0;//等于
                    bool _value = ((GBool)_now.GValue_L).GetValue(part_L) == ((GBool)_now.GValue_R).GetValue(part_R);

                    return _value == _ratio;
                }
                if (_now.GValueType == EGValueType.GString)
                {
                    bool _ratio = _now.Ratio == 0;//等于
                    bool _value = ((GString)_now.GValue_L).GetValue(part_L) == ((GString)_now.GValue_R).GetValue(part_R);
                    return _value == _ratio;
                }

                if (_now.GValueType == EGValueType.GInt)
                {
                    if (_now.Ratio == 0) return ((GInt)_now.GValue_L).GetValue(part_L) == ((GInt)_now.GValue_R).GetValue(part_R);
                    if (_now.Ratio == 1) return ((GInt)_now.GValue_L).GetValue(part_L) != ((GInt)_now.GValue_R).GetValue(part_R);
                    if (_now.Ratio == 2) return ((GInt)_now.GValue_L).GetValue(part_L) >= ((GInt)_now.GValue_R).GetValue(part_R);
                    if (_now.Ratio == 3) return ((GInt)_now.GValue_L).GetValue(part_L) <= ((GInt)_now.GValue_R).GetValue(part_R);
                    if (_now.Ratio == 4) return ((GInt)_now.GValue_L).GetValue(part_L) > ((GInt)_now.GValue_R).GetValue(part_R);
                    if (_now.Ratio == 5) return ((GInt)_now.GValue_L).GetValue(part_L) < ((GInt)_now.GValue_R).GetValue(part_R);
                }
                else if (_now.GValueType == EGValueType.GFloat)
                {
                    // if(_now.Ratio==0) return ((GFloat)_now.GValue_L).value == ((GFloat)_now.GValue_R).value;
                    // if(_now.Ratio==1) return ((GFloat)_now.GValue_L).value != ((GFloat)_now.GValue_R).value;
                    // Debug.LogWarning($"对比GFloat: {((GFloat)_now.GValue_L).value} : {((GFloat)_now.GValue_R).value}");
                    if (_now.Ratio == 0) return ((GFloat)_now.GValue_L).GetValue(part_L) >= ((GFloat)_now.GValue_R).GetValue(part_R);
                    if (_now.Ratio == 1) return ((GFloat)_now.GValue_L).GetValue(part_L) <= ((GFloat)_now.GValue_R).GetValue(part_R);
                    if (_now.Ratio == 2) return ((GFloat)_now.GValue_L).GetValue(part_L) > ((GFloat)_now.GValue_R).GetValue(part_R);
                    if (_now.Ratio == 3) return ((GFloat)_now.GValue_L).GetValue(part_L) < ((GFloat)_now.GValue_R).GetValue(part_R);
                }
                else if (_now.GValueType == EGValueType.GEnum)
                {
                    bool _ratio = _now.Ratio == 0;//等于
                    bool _value = ((GEnum)_now.GValue_L).GetValue(part_L) == ((GEnum)_now.GValue_R).GetValue(part_R);
                    return _value == _ratio;
                }
                else if (_now.GValueType == EGValueType.GPoint)
                {
                    GPoint _gPoint = (GPoint)_now.GValue_L;
                    return stateMachine_L.PointDic.ContainsKey((_gPoint.mValueGroupIndex, _gPoint.mValueIndex, _gPoint.mSerValue)) == (_now.Ratio == 0);
                }
                else if (_now.GValueType == EGValueType.GTransform)
                {
                    return ((GTransform)_now.GValue_L).GetValue(part_L) is not null == (_now.Ratio == 0);
                }
                else if (_now.GValueType == EGValueType.GUnit)
                {
                    return ((GUnit)_now.GValue_L).GetValue(part_L) is not null == (_now.Ratio == 0);
                }
            }
            return true;
        }

        public GValue_Ratio(bool _isOpen = false)
        {
            isOpen = _isOpen;
        }

        public GValue_Ratio Clone()
        {
#if UNITY_EDITOR
            GValue_Ratio n = new GValue_Ratio();
            n.isOpen = this.isOpen;
            n.checkAll = this.checkAll;
            foreach (var VARIABLE in GValue_RatioPart)
            {
                n.GValue_RatioPart.Add(VARIABLE.Clone());
            }
            return n;
#endif
            return this;
        }
    }

    [System.Serializable]
    public class GValue_RatioPart
    {
        [SerializeField] public EGValueType GValueType;
        [SerializeReference] public GValue GValue_L;
        [SerializeField] public byte Ratio;
        [SerializeReference] public GValue GValue_R;

        public GValue_RatioPart(EGValueType _GValueType, GValue _GValue_L, byte _Ratio, GValue _GValue_R)
        {
            GValueType = _GValueType;
            GValue_L = _GValue_L;
            Ratio = _Ratio;
            GValue_R = _GValue_R;
        }

        public GValue_RatioPart Clone()
        {
            return new GValue_RatioPart(GValueType, GValue_L.Clone(), Ratio, GValue_R.Clone());
        }
    }
}