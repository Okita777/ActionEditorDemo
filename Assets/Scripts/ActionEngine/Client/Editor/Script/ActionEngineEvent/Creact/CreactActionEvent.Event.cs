using AsiActionEditor_Ex.RunTime;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;

namespace AsiTimeLine.Editor
{
    public partial class CreactActionEvent
    {
        public static EditorActionEvent Events(int _id)
        {
            EEvenType _eventType = (EEvenType)_id;
            switch (_eventType)
            {
                //Camera
                case EEvenType.EET_CameraChange:
                    return CreatAction(new Event_CameraChange(), -100);
                case EEvenType.EET_CameraShake:
                    return CreatAction(new Event_CameraShake());

                //FindTarget
                case EEvenType.EET_FindTargetToCircle:
                    return CreatAction(new Event_FindTargetToCircle());

                //Misc
                case EEvenType.EET_ActionDebug:
                    return CreatAction(new Event_ActionDebug(), true);//单帧
                case EEvenType.EET_Partocle:
                    return CreatActionInheritable(new Event_PlayParticle(), true);//单帧
                case EEvenType.EET_WeaponTrail:
                    return CreatActionInheritable(new Event_WeaponTrail());//单帧
                case EEvenType.EET_SimulatedInput:
                    return CreatAction(new Event_SimulatedInput(), true);//单帧
                case EEvenType.EET_GValueCallback:
                    return CreatAction(new Event_GValueCallback());//单帧
                case EEvenType.EET_CustomEventCallback:
                    return CreatActionInheritable(new Event_CustomEventCallback(), true);//单帧
                case EEvenType.EET_SetGValue:
                    return CreatActionInheritable(new Event_SetGValue(), true);//单帧, 可继承
                case EEvenType.EET_SetGValueFromGValue:
                    return CreatActionInheritable(new Event_SetGValueFromGValue(), true);//单帧, 可继承
                case EEvenType.EET_SetGvalue_Transform:
                    return CreatAction(new Event_SetGvalue_Transform(), true);//单帧
                case EEvenType.EET_Audio:
                    return CreatAction(new Event_PlayAudio(), true);//单帧
                case EEvenType.EET_SetActionTotal:
                    return CreatAction(new Event_SetActionTotal(), true);//单帧
                case EEvenType.EET_RimLight:
                    return CreatActionInheritable(new Event_RimLight());//单帧
                case EEvenType.EET_Dissolve:
                    return CreatActionInheritable(new Event_Dissolve());//单帧
                case EEvenType.EET_RemoveSkillToUnit:
                    return CreatActionInheritable(new Event_RemoveSkillToUnit());//单帧
                //Anima
                case EEvenType.EET_SetAnimFloat:
                    return CreatActionInheritable(new Event_SetAnimFloat(), true);//单帧, 可继承
                case EEvenType.EET_SetAnimFloatFromBluePrint:
                    return CreatActionInheritable(new Event_SetAnimFloatFromBluePrint(), true);//单帧, 可继承
                //case EEvenType.EET_InteractBarrier: 
                //    return CreatAction(new Event_InteractBarrier());
                case EEvenType.EET_RootWeight:
                    return CreatActionInheritable(new Event_RootWeight());//可继承
                case EEvenType.EET_TargetingMove:
                    return CreatActionInheritable(new Event_TargetingMove());//可继承
                case EEvenType.EET_TowBoneIK:
                    return CreatActionInheritable(new Event_TowBoneIK());//可继承
                case EEvenType.EET_SetPointData:
                    return CreatActionInheritable(new Event_SetPointData());//可继承

                //ItemInteract
                case EEvenType.EET_Attach:
                    return CreatAction(new Event_Attach(), true);//单帧
                case EEvenType.EET_UnitAlignment:
                    return CreatAction(new Event_Alignment());
                case EEvenType.EET_SceneInteractObject:
                    return CreatAction(new Event_SceneInteractObject(), true);//单帧

                //Unit
                // case EEvenType.EET_CharacterMove:
                //     return CreatActionInheritable(new Event_CharacterMove());//可继承
                case EEvenType.EET_CharacterGravity:
                    return CreatAction(new Event_CharacterGravity(), true);//单帧
                case EEvenType.EET_CharacterPos:
                    return CreatAction(new Event_CharacterPos(), true);//单帧
                case EEvenType.EET_CharacterAddForce:
                    return CreatAction(new Event_CharacterAddForce(), true);//单帧
                case EEvenType.EET_CharacterOnMove:
                    return CreatActionInheritable(new Event_CharacterOnMove());//可继承
                case EEvenType.EET_UnitRot:
                    return CreatActionInheritable(new Event_UnitRot());//可继承
                case EEvenType.EET_PathFind:
                    return CreatActionInheritable(new Event_PathFind());//可继承
                case EEvenType.EET_OccupancyRing:
                    return CreatActionInheritable(new Event_OccupancyRing());//可继承
                case EEvenType.EET_TimeScale:
                    return CreatActionInheritable(new Event_TimeScale());//可继承
                case EEvenType.EET_ChangeUnitLayer:
                    return CreatActionInheritable(new Event_ChangeUnitLayer(), true);//可继承
                case EEvenType.EET_BanMoveToNavMash:
                    return CreatActionInheritable(new Event_BanMoveToNavMash(), true);//可继承
                case EEvenType.EET_DestoryUnit:
                    return CreatAction(new Event_DestoryUnit(), false);//单帧
                case EEvenType.EET_HitkBox_Box:
                    return CreatAction(new Event_HitBox_Box());
                case EEvenType.EET_HitkBox_Capsule:
                    return CreatAction(new Event_HitBox_Capsule());
                case EEvenType.EET_HitkBox_Sphere:
                    return CreatAction(new Event_HitBox_Sphere());

                case EEvenType.EET_SoftLock:
                    return CreatActionInheritable(new Event_SoftLock());//可继承

                //GValue变更监听
                case EEvenType.EET_OnGIntChanged:
                    return CreatAction(new Event_OnGIntChanged());
                case EEvenType.EET_OnGFloatChanged:
                    return CreatAction(new Event_OnGFloatChanged());
                case EEvenType.EET_OnGEnumChanged:
                    return CreatAction(new Event_OnGEnumChanged());
                case EEvenType.EET_OnGBoolChanged:
                    return CreatAction(new Event_OnGBoolChanged());
                case EEvenType.EET_OnGUnitChanged:
                    return CreatAction(new Event_OnGUnitChanged());

                //GValue变更监听(数组版)
                case EEvenType.EET_OnGIntChanged_Array:
                    return CreatAction(new Event_OnGIntChanged_Array());
                case EEvenType.EET_OnGFloatChanged_Array:
                    return CreatAction(new Event_OnGFloatChanged_Array());
                case EEvenType.EET_OnGEnumChanged_Array:
                    return CreatAction(new Event_OnGEnumChanged_Array());
                case EEvenType.EET_OnGBoolChanged_Array:
                    return CreatAction(new Event_OnGBoolChanged_Array());
                case EEvenType.EET_OnGUnitChanged_Array:
                    return CreatAction(new Event_OnGUnitChanged_Array());

                    ////动画状态标签管理
                    //case EEvenType.EET_Lable:
                    //    return CreatActionInheritable(new Event_Lable());//可继承
                    //case EEvenType.EET_Lable_Delay:
                    //    return CreatActionInheritable(new Event_Lable_Delay());//可继承

                // 残影
                case EEvenType.EET_AfterImage:
                    return CreatAction(new Event_AfterImage());
                case EEvenType.EET_AfterImageEntity:
                    return CreatAction(new Event_AfterImageEntity());

                // 后处理
                case EEvenType.EET_MotionBlur:
                    return CreatAction(new Event_MotionBlur());

                // 灯光
                case EEvenType.EET_Light:
                    return CreatAction(new Event_Light());

                // 技能灯光
                case EEvenType.EET_SkillLight:
                    return CreatAction(new Event_SkillLight());

                //PuppetMaster
                case EEvenType.EET_PuppetMaster:
#if PuppetMaster
                    return CreatAction(new Event_PuppetMaster(), true);//单帧
#else
                    EngineDebug.LogWarning("PuppetMaster 插件未启用，无法创建该事件");
                    return null;
#endif
            }

            EngineDebug.LogError($"客户端下，未写明 [<color=#FFCC00>{_eventType}</color>] 的实例化，请告知客户端程序");
            return null;
        }

        private static EditorActionEvent CreatAction(IActionEventData _event, bool _Single = false)
        {
            return new EditorActionEvent(_event, _Single ? 0 : 333);//AsiActionEngine.RunTime.MotionEngineConst.TimeDoubling
        }
        private static EditorActionEvent CreatAction(IActionEventData _event, int _Duration)
        {
            return new EditorActionEvent(_event, _Duration);
        }

        //创建可上下继承内部参数的事件轨道
        private static EditorActionEvent CreatActionInheritable(IActionEventData _event, bool _Single = false)
        {
            EditorActionEvent _actionEvent = CreatAction(_event, _Single);
            _actionEvent.EditorInheritable = true;
            _actionEvent.Inheritable = false;
            return _actionEvent;
        }
        private static EditorActionEvent CreatActionInheritable(IActionEventData _event, int _Duration)
        {
            EditorActionEvent _actionEvent = CreatAction(_event, _Duration);
            _actionEvent.EditorInheritable = true;
            _actionEvent.Inheritable = false;
            return _actionEvent;
        }
    }
}