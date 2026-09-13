using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_MathFuntion : StaticActionLogics
    {
        public override void OnStart(ActionStateMachine _actionState)
        {
            ListInit();
        }

        #region 预分配动态列表
        public Dictionary<float, List<ActionEngine_Unit>> m_Dic_UnitToFloat;
        public List<ActionEngine_Unit>[] m_List_Unit;
        private void ListInit()
        {
            m_Dic_UnitToFloat = new Dictionary<float, List<ActionEngine_Unit>>(100);
            m_List_Unit = new List<ActionEngine_Unit>[100];
            //Debug.LogWarning($"生成长度 [{m_List_Unit.Length}]");
            for (int i = 0; m_List_Unit.Length > i; i++)
            {
                m_List_Unit[i] = new List<ActionEngine_Unit>(10);
            }
        }
        #endregion

        /// <summary>
        /// 检查两个位置是否在目标距离范围内
        /// </summary>
        /// <param name="_pos1">位置1</param>
        /// <param name="_pos2">位置2</param>
        /// <param name="_dis">目标距离</param>
        /// <param name="_IgoneX">忽略X轴</param>
        /// <param name="_IgoneY">忽略Y轴</param>
        /// <param name="_IgoneZ">忽略Z轴</param>
        /// <returns>范围内时返回true</returns>
        public bool SelfDistance(Vector3 _pos1, Vector3 _pos2, float _dis, bool _IgoneX = false, bool _IgoneY = false, bool _IgoneZ = false)
            => OnSelfDistance(_pos1, _pos2, _dis, _IgoneX, _IgoneY, _IgoneZ);
        public bool SelfAngle(Vector3 _dir1, Vector3 _dir2, float _angle)
            => OnSelfAngle(_dir1, _dir2, _angle);
        public bool SelfAngle(Vector3 _dir1, Vector3 _dir2, float _angle, Vector3 _axis)
            => OnSelfAngle(_dir1, _dir2, _angle, _axis);


        private bool OnSelfDistance(Vector3 _pos1, Vector3 _pos2, float _dis, bool _IgoneX, bool _IgoneY, bool _IgoneZ)
        {
            Vector3 _target = _pos1 - _pos2;
            _target.x *= _IgoneX ? 0 : 1;
            _target.y *= _IgoneY ? 0 : 1;
            _target.z *= _IgoneZ ? 0 : 1;
            return _target.sqrMagnitude < _dis * _dis;
        }

        private bool OnSelfAngle(Vector3 _dir1, Vector3 _dir2, float _angle)
        {
            return Vector3.Angle(_dir1, _dir2) * 2 < _angle;
        }
        private bool OnSelfAngle(Vector3 _dir1, Vector3 _dir2, float _angle, Vector3 _axis)
        {
            return Mathf.Abs(Vector3.SignedAngle(_dir1, _dir2, _axis)) * 2 < _angle;
        }
    }
}