using System;
using System.Linq;
using AsiActionEditor_Ex.RunTime;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public partial class DrawInspector
    {
        public static string GetTitle(EditorActionEvent _actionEvent, bool _isInit)
        {
            string _title = String.Empty;

            IActionEventData _eventData = _actionEvent.EventData;
            if (_eventData == null)
            {
                return "Error";
            }

            if (_eventData is Event_Lable _eusl)
            {
                if (_eusl.IsRemoveAll)
                {
                    _title = "状态标签：<color=#FF0000>删除所有标签</color>";
                }
                else
                {
                    _title = string.Format("状态标签：<color=#FFCC00>{0}</color>\n类型：{1}",
                        ResourcesWindow.Instance.ActionLable[_eusl.StateLable],
                        _eusl.IsRemove ? "<color=#FF0000>删除该标签</color>" : "<color=#00FF00>添加该标签</color>"
                    );
                }
            }

            #region UnitAction
            else if (_eventData is Event_Attach _ewpc)
            {
                // _title = string.Format("武器挂点切换：{0}\n挂点: <color=#FFCC00>{1}</color>",
                //     _ewpc.IsRightWeapon ? "右" : "左",
                //     ((ECharacteLimbType)_ewpc.LimbPointType).ToString()
                // );

                _title = "附加对象";
                // if (_ewpc.ReferTransform.Get() is null)
                // {
                //     _title = "未设置要附加的对象";
                // }
                // else
                // {
                //     _title = string.Format("将：{0}\n附加至: <color=#FFCC00>{1}</color>",
                //         _ewpc.ReferTransform.Get().name,
                //         _ewpc.TargetTransform.Get() is null?"世界空间" : _ewpc.TargetTransform.Get().name
                //     );
                // }

            }

            //相机跳转
            else if (_eventData is Event_CameraChange _ecc)
            {
                if (ResourcesWindow.Instance.PreCamControl != null)
                {
                    string _pointName = "未找到挂点";
                    if (ResourcesWindow.Instance.GetUnit().TryGetComponent(out CharacterConfig _config))
                    {
                        ECharacteLimbType[] _ECLT = _config.HelpPointDic.Keys.ToArray();
                        for (int i = 0; i < _ECLT.Length; i++)
                        {
                            if ((int)_ECLT[i] == _ecc.EnterPoint)
                            {
                                _pointName = _ECLT[i].ToString();
                                break;
                            }
                        }
                    }

                    _title = string.Format(
                        "相机跳转：<b>{0}</b>\n进入时长：<b>{1}</b>     相机挂点：<b>{2}</b>",
                        ResourcesWindow.Instance.PreCamControl.allCinemachine[_ecc.EnterCam].name,
                        _ecc.EnterTime.ToString("F1"),
                        _pointName
                    );
                }
                else
                {
                    _title = "<color=#FF0000>未创建预览相机，无法显示</color>";
                }
            }

            //相机抖动
            else if (_eventData is Event_CameraShake _ecs)
            {
                if (ResourcesWindow.Instance.PreCamControl is not null)
                {
                    _title = string.Format(
                        "枢轴偏移: {0}\n振幅：{1}  频率：{2}",
                        _ecs.PivotOffset,
                        _ecs.AmplitudeGain.ToString("F2"),
                        _ecs.FrequencyGain.ToString("F2")
                    );
                }
                else
                {
                    _title = "<color=#FF0000>未创建预览相机，无法显示</color>";
                }
            }

            //时间缩放
            else if (_eventData is Event_TimeScale _ets)
            {
                string _timeScale = _ets.TimeScale.mSerValue.ToString();
                _title = string.Format("<b>时间缩放</b>：{0}倍", _timeScale);
            }

            //单位销毁
            else if (_eventData is Event_DestoryUnit _edu)
            {
                _title = "销毁单位(事件结束时销毁)";
            }

            //单位移动
            else if (_eventData is Event_CharacterOnMove _ecom)
            {
                string _moveInput = _ecom.IsMoveInput ? "     [移动中启用蓝图]" : "";
                string _lerp = _ecom.LerpSpeed.mSerValue == 0 ? ""
                             : string.Format("\n过渡速度：{0}", _ecom.LerpSpeed.mSerValue);
                _title = string.Format("<b>单位移动</b>{0}{1}", _lerp, _moveInput);
            }

            //单位朝向
            else if (_eventData is Event_UnitRot _eur)
            {
                string _isMove = _eur.IsMove ? "     [仅移动时转向]" : "";
                string _isPre = _eur.IsMovePre ? "     [接受方向预输入]" : "";
                string _isEuler = _eur.IsEuler ? "     [欧拉角]" : "";
                string _lerp = _eur.RotLerp == 0 ? "瞬间" : _eur.RotLerp.ToString();
                string _priority = _eur.RotPriority.mSerValue.ToString();
                string _time = _eur.TotalTime.ToString();

                _title = string.Format("<b>单位朝向</b>{0}{1}{2}\n转向速度：{3}     优先度：{4}     旋转持续：{5}s",
                                       _isMove, _isPre, _isEuler, _lerp, _priority, _time);
            }

            //Root权重
            else if (_eventData is Event_RootWeight _erw)
            {
                _title = "<b>[RootMotion无效]";
            }

            //瞄准偏移
            else if (_eventData is Event_TargetingMove _etm)
            {
                string[] _axis = { "X", "Y", "Z", "-X", "-Y", "-Z" };
                string _refBone = ((ECharacteLimbType)_etm.RefertBone).ToString();
                string _tgtBone = ((ECharacteLimbType)_etm.TargetBone).ToString();
                string _forward = _axis[_etm.TargetForward];
                string _yOffset = _etm.WorldAngle.mSerValue == 0
                               ? ""
                               : $"     Y偏移：{_etm.WorldAngle.mSerValue}"; int _links = _etm.TargetBoneLinks;
                string _freeX = _etm.FreeToX ? "[左右]" : "";
                string _freeY = _etm.FreeToY ? "[上下]" : "";
                string _enter = (_etm.EnterTime * 0.001f).ToString();
                string _exit = (_etm.ExitTime * 0.001f).ToString();

                _title = string.Format(
                    "<b>瞄准偏移</b>     链数：{4}     混入：{7}s     混出：{8}s{3}\n" +
                    "[{0}→{1}的{2}轴]   {5}   {6} ",
                    _refBone, _tgtBone, _forward, _yOffset, _links,
                    _freeX, _freeY, _enter, _exit
                    );
            }

            //单位重力
            else if (_eventData is Event_CharacterGravity _ecg)
            {
                string _gText = _ecg.Gravity == 0f ? "无重力" : _ecg.Gravity.ToString();
                _title = string.Format("<b>角色重力</b>：{0}", _gText);
            }

            //设置Animator的Float参数
            else if (_eventData is Event_SetAnimFloat _esf)
            {
                string _type = _esf.ValueType == Event_SetAnimFloat.EValueType._1D ? "1D" : "2D";
                string _src = _esf.AnimFloatFor.ToString();
                string _speed = _esf.AnimaFloatSpeed.ToString();
                string _nameX = string.IsNullOrEmpty(_esf.AnimaFloatName) ? "空" : _esf.AnimaFloatName;
                string _nameY = string.IsNullOrEmpty(_esf.AnimaFloatName2) ? "空" : _esf.AnimaFloatName2;

                _title = _type == "1D"
                    ? string.Format("<b>状态机Float类型</b>：{0}     来源：{1}\n速度：{2}     [X：{3}]", _type, _src, _speed, _nameX)
                    : string.Format("<b>状态机Float类型</b>：{0}     来源：{1}\n速度：{2}     [X：{3}]    [Y：{4}]", _type, _src, _speed, _nameX, _nameY);
            }
            #endregion


            #region SkillAction

            //技能发射器
            else if (_eventData is Event_SkillEmitter _ese)
            {
                EditorSkillWarp _skill = ResourcesWindow.Instance.GetSelectSkill();

                string _skillName = _skill.GetNameToID(_ese.SkillID);
                bool _isMiss = _skillName == "Action数据丢失";
                _skillName = $"<color={(_isMiss ? "red" : "blue")}>{_skillName}</color>";

                string _cnt = _ese.EmissionRate.mSerValue.ToString();

                Vector3 _offP = _ese.OffsetPos.GetValue();
                Vector3 _offR = _ese.OffsetRot.GetValue();

                string _offPStr = _offP.x.ToString() + "," + _offP.y.ToString() + "," + _offP.z.ToString();
                string _offRStr = _offR.x.ToString() + "," + _offR.y.ToString() + "," + _offR.z.ToString();

                _title = "技能发射：" + _skillName +
                         " 发射数量" + _cnt +
                         " 偏移位置(" + _offPStr + ")" +
                         " 偏移角度(" + _offRStr + ")";

                //if (_isMiss) Debug.LogError(_skillName);
                _actionEvent.SetError(_isMiss);//设置事件报错提示
            }
            else if (_eventData is Event_SkillEntity _esey)
            {
                _title = "技能实例\n";
                if (string.IsNullOrEmpty(_esey.SkillEntity))
                {
                    _title += "未配置对象";
                }
                else
                {
                    EngineResourcesManager.Instance.LoaderObj_Editor(_esey.SkillEntity, _Object =>
                    {
                        if (_Object is GameObject gameObject)
                        {
                            _title += gameObject.name;
                        }
                        else
                        {
                            _title += "丢失对象引用";
                        }
                    });
                }
            }


            #endregion

            #region Light
            else if (_eventData is Event_Light _elight)
            {
                int _lightCount = _elight.Lights?.Count ?? 0;
                _title = string.Format("<b>灯光</b>  数量：{0}", _lightCount);
            }
            else if (_eventData is Event_SkillLight _eskillLight)
            {
                int _count = _eskillLight.Lights?.Count ?? 0;
                _title = string.Format("<b>技能灯光</b>  数量：{0}", _count);
            }
            #endregion
            return _title;
        }
    }
}