using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    /// <summary>
    /// 此脚本仅用于编辑器运行角色操作的单机环境
    /// 但是就算Runtime环境挂载这个脚本也不会出现任何问题
    /// </summary>
    public class UnitEditorPreview : ActionPreviewMark
    {
        private float mCamChangeTime;
        private GValue_Setting GValueSetting;

        public bool mIsPlayer
        {
            // get { return IsPlayer; }
            set
            {
                if (value)
                {
                    mCamChangeTime = 1;//相机过渡时间
                }
                IsPlayer = value;
            }
        }

        [HideInInspector]
        public ActionEngine_Unit mUnit;

        [SerializeField, Min(1)]
        private long mNavigationStableSourceId;

#if UNITY_EDITOR
        private void Reset()
        {
            mNavigationStableSourceId =
                NavigationStableUnitIdentity.CreateAuthoringSourceId();
        }
#endif

        private void Start()
        {
            if (mNavigationStableSourceId <= 0L)
            {
                throw new System.InvalidOperationException(
                    $"UnitEditorPreview requires Navigation Stable Source ID: {name}.");
            }

            ActionEngineManager_Input.Instance.CreateGameManager();

            mUnit = GetComponent<ITargetUnit>().GetUnit();
            NavigationStableUnitIdentity.Bind(
                mUnit,
                NavigationStableUnitId.FromPositiveInt64(
                    NavigationStableUnitDomain.AuthoringPreview,
                    mNavigationStableSourceId));
            mUnit.CameraID = DefaltCamera;
            mUnit.SetMaster(mUnit, mUnit);
            ActionEngineManager_Unit.Instance.GetUnitWarp(mUnit.UnitWarpID, warp =>
            {
                GValueSetting = warp.GValueSetting;
                ActionEngineManager_Unit.Instance.GetActionList(ActionName, list =>
                {
                    ActionEngineManager_GValue.Instance.GetGValue((gvalue, equation) =>
                    {
                        //Debug.Log("加载equation: " + (equation is not null)+ "\n加载gvalue: " + (gvalue.Count));
                        ActionStateMachine statePart = new ActionStateMachine(mUnit, mUnit.GetComponent<Animator>(),
                            list, gvalue, equation, GValueSetting);
                        mUnit.SetActionStateMachine(statePart);

                        statePart.InitGValue();//初始化GValue

                        //执行初始化层级的事件
                        ActionStatePart _part = EngineResourcesManager.Instance.GetPreActionStatePart();
                        _part.SetInitValue(statePart, false);
                        statePart.ActionStateInfo.PerformActionEvent(_part);

                        statePart.InitState();//执行默认Action

                        mUnit.Init();

                        ActionEngineManager_Unit.Instance.AddUnit(mUnit);

                        if (IsPlayer)
                        {
                            ActionEngineManager_Input.Instance.ChangePlayer(mUnit); //注册输入系统的操作对象
                        }
                    });
                });
            });

        }

        private void OnDestroy()
        {
            NavigationStableUnitIdentity.Clear(mUnit);
        }

        // public override void ReLoadActionInfo()
        // {
        //     ActionEngineManager_Unit.Instance.GetActionList(ActionName, list =>
        //     {
        //         ActionEngineManager_GValue.Instance.GetGValueData(gvalue =>
        //         {
        //             ActionStateMachine statePart = new ActionStateMachine(mUnit, GetComponent<Animator>(), 
        //                 list, gvalue, GValueSetting);
        //             mUnit.SetActionStateMachine(statePart);
        //
        //             if (IsPlayer)
        //             {
        //                 CameraControl _cameraControl= ActionEngineManager_Input.Instance.CurCamera;
        //                 if (statePart.TryGetComponent(out CharacterConfig _config))
        //                 {
        //                     if (_config.HelpPointDic.TryGetValue(ECharacteLimbType.Cam_Main, out Transform _trans))
        //                     {
        //                         _cameraControl.OnInit(_trans, statePart, 0);
        //                     }
        //                 }
        //             }
        //         });
        //     });
        // }

        private void Update()
        {
            if (!IsPlayer)
            {
                return;
            }

            float _deltatime = Time.deltaTime;

            ActionEngineManager_Input _input = ActionEngineManager_Input.Instance;
            if (mCamChangeTime > 0)
            {
                mCamChangeTime -= _deltatime;

                _input.CamOffsetPos = Vector3.Lerp(_input.CamOffsetPos, CamOffsetPos, 7 * _deltatime);
                _input.CamGlobalOffsetPos = Vector3.Lerp(_input.CamGlobalOffsetPos, Vector3.zero, 7 * _deltatime);

                if (mCamChangeTime <= 0)
                {
                    _input.CamGlobalOffsetPos = Vector3.zero;
                }
            }
            else
            {
                _input.CamOffsetPos = CamOffsetPos;
                _input.SetCamRotSpeed(CamRotSpeed);
            }
        }
    }
}
