using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

#if FMOD
using FMODUnity;
#endif

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineManager : MonoBehaviour
    {
        [System.Serializable]
        public struct SDynamicAttributesUpdata
        {
            public int[] gvalue1;
            public int[] gvalue2;
            public int skilltag;
            public int comp;
            public SDynamicAttributesUpdata(int[] gvalue1, int[] gvalue2, int skilltag, int comp)
            {
                this.gvalue1 = gvalue1;
                this.gvalue2 = gvalue2;
                this.skilltag = skilltag;
                this.comp = comp;
            }
        }

        public List<SDynamicAttributesUpdata> mDynamicAttributesUpdatas = new List<SDynamicAttributesUpdata>();
        private GInt mReferGInt = new GInt();
        public void InjectAttributesToUnit(ActionEngine_Unit _unit)
        {
            //string _str = $"注册!! [{_unit.gameObject.name}]  IsPlayer[<color=#ffcc00>{_unit == ActionEngineManager_Input.Instance.Player}</color>]  [{mDynamicAttributesUpdatas.Count}]";
            ActionStateMachine _stateMachine = _unit.ActionStateMachine;
            foreach (SDynamicAttributesUpdata attr in mDynamicAttributesUpdatas)
            {
                if (ActionEngineManager_GValue.Instance.GetGvalue_Key(_unit, (ushort)attr.gvalue1[0], attr.gvalue1[1], out (ushort, ushort) _key))
                {
                    mReferGInt.mValueGroupIndex = _key.Item1;
                    mReferGInt.mValueIndex = _key.Item2;
                    //_str += $"\nGroupIndex[{_key.Item1}]  ValueIndex[{_key.Item2}]   ({attr.gvalue1[0]}, {attr.gvalue1[1]})";
                    SDynamicAttributesUpdata nowAttr = attr;
                    _stateMachine.OnGIntChanged(mReferGInt, (_old, _new) =>
                    {
                        if (!ActionEngineManager_GValue.Instance.GetGvalue_Key(_unit, (ushort)nowAttr.gvalue2[0], nowAttr.gvalue2[1], out (ushort, ushort) _key2))
                        {
                            GetGVErrorDebug(nowAttr.gvalue2);
                            return;
                        }

                        //EngineDebug.LogError($"<color=#ffcc00>监测到值变化:</color> Gindex:[{_key.Item1}]  Index:[{_key.Item2}]");
                        //string debugName = $"操作对象的Debug: <color=#ffcc00>当前监测值叠加</color>\n";
                        int _value = _stateMachine.GValuePool.GetInt(_key.Item1, _key.Item2);

                        if (_unit.m_SkillDic.TryGetValue(nowAttr.skilltag, out List<ActionEngine_Skill> _skillList))
                        {
                            foreach (ActionEngine_Skill item in _skillList)
                            {
                                _value += item.ActionStateMachine.GValuePool.GetInt(_key.Item1, _key.Item2);
                                //debugName += $"当前叠加值 [{_value}]\n";
                            }
                        }
                        //if (_stateMachine.CurUnit.GetSource == ActionEngineManager_Input.Instance.Player)
                        //{
                        //    EngineDebug.LogError(debugName);
                        //}
                        _stateMachine.GValuePool.SetInt(_key2.Item1, _key2.Item2, _value);
                    });
                }
                else
                {
                    GetGVErrorDebug(attr.gvalue1);
                }
            }
            //EngineDebug.LogError(_str);
        }

        private void GetGVErrorDebug(int[] _val)
        {
            EngineDebug.LogError($"GValue ????!! Group[<color=#ffcc00>{_val[0]}</color>]  ID[<color=#ffcc00>{_val[1]}</color>]");
        }
    }
}