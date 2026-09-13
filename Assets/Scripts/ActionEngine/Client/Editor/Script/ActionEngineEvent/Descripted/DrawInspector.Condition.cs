using System.Collections.Generic;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public partial class DrawInspector
    {
        private static List<string> DrawNames = new List<string>();
        private static readonly string[] duibiFH = new[] { ">=", "<=" };
        public static void DrawCondition(IInterruptCondition _interruptCondition, bool _isInit)
        {
            DrawNames.Clear();
            switch ((EConditionType)_interruptCondition.InterruptType)
            {
                // case EInterruptType.EIT_CheckInput:
                //     DrawCheckCostomKey((CheckCostomKey)_interruptCondition);
                //     break;
                case EConditionType.EIT_CheckDirOffset:
                    DrawDirOffset((CheckDirOffset)_interruptCondition);
                    break;
                case EConditionType.EIT_CheckTransfrom_RotAndPosOffset:
                    Drawransfrom_RotAndPosOffset((CheckTransfrom_RotAndPosOffset)_interruptCondition);
                    break;
                default:
                    DrawEditorAttribute.Draw(_interruptCondition);
                    break;
            }
        }

        #region MyRegion

        private static void Drawransfrom_RotAndPosOffset(CheckTransfrom_RotAndPosOffset _check)
        {
            DrawNames.Add("selectTransform");
            DrawNames.Add("IsLocalPlayer");
            DrawNames.Add("CheckType");
            // if (!_check.IsLocalPlayer)
            {
                DrawNames.Add("Target");
            }
            DrawEditorAttribute.Draw(_check, DrawNames.ToArray());
            DrawNames.Clear();
            if (_check.CheckType == CheckTransfrom_RotAndPosOffset.ECheckType.DistanceAndAngle)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("对比角度", GUILayout.Width(60));
                    _check.m_CheckAngleGreater = EditorGUILayout.Popup(_check.m_CheckAngleGreater ? 0 : 1, duibiFH) == 0;
                    _check.m_Angle = EditorGUILayout.FloatField(_check.m_Angle);
                    // _check.IsRange = EditorGUILayout.Toggle("居中",_check.IsRange);
                }
                _check.AngleOffset = EditorGUILayout.FloatField("偏移角度 ", _check.AngleOffset);

                // DrawNames.Add("AngleOffset");
                // DrawNames.Add("IsRange");
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("对比距离", GUILayout.Width(60));
                    _check.m_CheckDisGreater = EditorGUILayout.Popup(_check.m_CheckDisGreater ? 0 : 1, duibiFH) == 0;
                    //_check.m_Distance = EditorGUILayout.FloatField(_check.m_Distance);
                    //_check.m_Distance = DrawEditorAttribute.DrawGValue()
                    DrawEditorAttribute.Draw(_check, new[] { "Distance" });
                    _check.ConstomHeightS = EditorGUILayout.Toggle("柱形", _check.ConstomHeightS);
                }
                if (_check.ConstomHeightS)
                    _check.ConstomHeight = EditorGUILayout.FloatField("     高度:", _check.ConstomHeight);
                DrawNames.Add("PosOffset");
            }
            else if (_check.CheckType == CheckTransfrom_RotAndPosOffset.ECheckType.OnlyAngle)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("对比角度", GUILayout.Width(60));
                    _check.m_CheckAngleGreater = EditorGUILayout.Popup(_check.m_CheckAngleGreater ? 0 : 1, duibiFH) == 0;
                    _check.m_Angle = EditorGUILayout.FloatField(_check.m_Angle);
                    // _check.IsRange = EditorGUILayout.Toggle("居中",_check.IsRange);
                }
                _check.AngleOffset = EditorGUILayout.FloatField("偏移角度 ", _check.AngleOffset);

                // DrawNames.Add("AngleOffset");
                // DrawNames.Add("IsRange");
            }
            else
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("对比距离", GUILayout.Width(60));
                    _check.m_CheckDisGreater = EditorGUILayout.Popup(_check.m_CheckDisGreater ? 0 : 1, duibiFH) == 0;
                    //_check.m_Distance = EditorGUILayout.FloatField(_check.m_Distance);
                    DrawEditorAttribute.Draw(_check, new[] { "Distance" });
                    _check.ConstomHeightS = EditorGUILayout.Toggle("柱形", _check.ConstomHeightS);
                }
                if (_check.ConstomHeightS)
                    _check.ConstomHeight = EditorGUILayout.FloatField("     高度:", _check.ConstomHeight);
                DrawNames.Add("PosOffset");
            }
            DrawEditorAttribute.Draw(_check, DrawNames.ToArray());
        }

        private static void DrawDirOffset(CheckDirOffset _checkDirOffset)
        {
            DrawNames.Add("ForAngleType");
            if (_checkDirOffset.ForAngleType == EAngleType.GTransForward ||
                _checkDirOffset.ForAngleType == EAngleType.LookToGTrans)
            {
                DrawNames.Add("STransform_r");
            }
            DrawNames.Add("OffsetAngle");
            DrawNames.Add("ToAngleType");
            if (_checkDirOffset.ToAngleType == EAngleType.GTransForward ||
                _checkDirOffset.ToAngleType == EAngleType.LookToGTrans)
            {
                DrawNames.Add("STransform_t");
            }
            DrawNames.Add("SelfAngleABS");
            DrawNames.Add("FH");
            DrawNames.Add("SelfAngle");
            DrawEditorAttribute.Draw(_checkDirOffset, DrawNames.ToArray());
        }
        #endregion

    }
}