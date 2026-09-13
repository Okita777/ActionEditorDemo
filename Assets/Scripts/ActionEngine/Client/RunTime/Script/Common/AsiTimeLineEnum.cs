using System.ComponentModel;

namespace AsiTimeLine.RunTime
{
    public enum EConditionType
    {
        [Description("检测玩家是否在地面")] EIT_CheckGround,
        [Description("检测当前单位是否为玩家")] EIT_CheckIsPlayer,
        [Description("检测玩家和地面距离")] EIT_CheckHeight,
        [Description("检测玩家前方障碍")] EIT_CheckBarrier,
        // [Description("检查当前找到的交互点")] EIT_CheckFindPoint,

        [Description("检查目标位置或者角度/Transform")] EIT_CheckTransfrom_RotAndPosOffset,

        [Description("检测玩家输入方向和角色方向差值")] EIT_CheckInputToTranDir,
        [Description("检测单位行为状态")] EIT_CheckActionState,
        [Description("检测单位行为标签")] EIT_CheckActionLable,
        [Description("检查输入方向")] EIT_CheckInputDir,
        [Description("检查方向差")] EIT_CheckDirOffset,

        [Description("检测GValue")] EIT_CheckGValue,
        [Description("检测半径内的对象")] EIT_CheckRadiusObj,
    }

    public enum EGValueListenKind
    {
        [Description("监听GInt")] GInt,
        [Description("监听GFloat")] GFloat,
        [Description("监听GEnum")] GEnum,
        [Description("监听GBool")] GBool,
        [Description("监听GUnit")] GUnit,
    }

    public enum EEvenType
    {
        //相机切换
        [Description("相机跳转")] EET_CameraChange,
        [Description("相机抖动")] EET_CameraShake,

        //在场景中寻找目标
        //[Description("按范围检测对象/球形")] EET_Event_FindTargetToSphere,
        [Description("按范围检测对象/圆")] EET_FindTargetToCircle,

        //动画状态相关
        [Description("设置Animator的Float参数")] EET_SetAnimFloat,
        [Description("设置Animator的Float参数(蓝图)")] EET_SetAnimFloatFromBluePrint,
        //[Description("设置角色对齐到障碍交互点位")] EET_InteractBarrier,
        //[Description("移动角色到钩锁交互点")] EET_MoveToHookPoint,
        [Description("瞄准偏移")] EET_TargetingMove,
        [Description("双骨骼IK")] EET_TowBoneIK,
        [Description("设置点数据")] EET_SetPointData,

        // 角色行为相关 
        [Description("音效")] EET_Audio,
        [Description("特效")] EET_Partocle,
        [Description("武器刀光")] EET_WeaponTrail,
        [Description("按GV执行回调")] EET_GValueCallback,
        [Description("自定义事件回调")] EET_CustomEventCallback,
        [Description("修改Action总时长")] EET_SetActionTotal,
        [Description("从单位移除skill")] EET_RemoveSkillToUnit,

        //道具交互
        [Description("对象附加")] EET_Attach,
        [Description("单位对齐")] EET_UnitAlignment,
        [Description("场景交互对象事件")] EET_SceneInteractObject,
        [Description("交互点类型")] EET_FindPoint,

        // 杂项
        [Description("Debug")] EET_ActionDebug,
        [Description("SetGValue")] EET_SetGValue,
        [Description("从GV写入GV")] EET_SetGValueFromGValue,
        [Description("SetGTransform")] EET_SetGvalue_Transform,
        [Description("材质校正-边缘光")] EET_RimLight,
        [Description("材质校正-溶解")] EET_Dissolve,
        [Description("表现-残影")] EET_AfterImage,
        [Description("表现-运动模糊")] EET_MotionBlur,
        [Description("表现-残影实体")] EET_AfterImageEntity,
        [Description("表现-灯光")] EET_Light,
        [Description("表现-技能灯光")] EET_SkillLight,

        //单位相关
        // [Description("角色位移")] EET_CharacterMove,
        [Description("单位重力")] EET_CharacterGravity,
        [Description("角色力度施加")] EET_CharacterAddForce,
        [Description("自动寻路事件")] EET_PathFind,
        [Description("从寻路网格限制位移")] EET_BanMoveToNavMash,
        [Description("模拟按键行为")] EET_SimulatedInput,
        [Description("单位位移")] EET_CharacterOnMove,
        [Description("单位坐标")] EET_CharacterPos,

        [Description("单位朝向")] EET_UnitRot,
        [Description("软锁定")] EET_SoftLock,
        [Description("Root权重")] EET_RootWeight,
        [Description("时间缩放")] EET_TimeScale,
        [Description("设置层级")] EET_ChangeUnitLayer,
        [Description("销毁单位")] EET_DestoryUnit,//BlockUnitMove
        [Description("受击盒-盒型")] EET_HitkBox_Box,
        [Description("受击盒-胶囊形")] EET_HitkBox_Capsule,
        [Description("受击盒-球形")] EET_HitkBox_Sphere,

        //ActionLable
        //[Description("延迟标签")] EET_Lable_Delay,
        //[Description("单位状态标签")] EET_Lable,

        //输入相关
        [Description("屏蔽输入")] EET_DisableAutofill,

        //GValue变更监听
        [Description("监听GValue变化/GInt")] EET_OnGIntChanged,
        [Description("监听GValue变化/GFloat")] EET_OnGFloatChanged,
        [Description("监听GValue变化/GEnum")] EET_OnGEnumChanged,
        [Description("监听GValue变化/GBool")] EET_OnGBoolChanged,
        [Description("监听GValue变化/GUnit")] EET_OnGUnitChanged,

        //GValue变更监听(数组版)
        [Description("监听GValue变化/GGroupInt")] EET_OnGIntChanged_Array,
        [Description("监听GValue变化/GGroupFloat")] EET_OnGFloatChanged_Array,
        [Description("监听GValue变化/GGroupInt(GEnum)")] EET_OnGEnumChanged_Array,
        [Description("监听GValue变化/GGroupBool")] EET_OnGBoolChanged_Array,
        [Description("监听GValue变化/GGroupUnit")] EET_OnGUnitChanged_Array,

        //2D控制器
        //[Description("2D角色位移")] EET_2D_Move,
        //[Description("2D角色重力")] EET_2D_Gravity,

        //物理布娃娃
        [Description("PuppetMaster控制")] EET_PuppetMaster,

        //占格围环（新增追加在末尾，避免插中间移位已有事件的整数值）
        [Description("占格围环(自驱动)")] EET_OccupancyRing,
    }
}