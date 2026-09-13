using System.ComponentModel;

namespace AsiActionEngine.RunTime
{
    public enum EActionProperty
    {
        Unit,
        Action,
        Prop,
        GValue,
        Skill,
        Equation,
        Camera,
        Item,
    }

    public enum EActionEventTriggerType
    {
        Enter,//进入
        Trigger,//单帧触发
        Exit,//退出
    }

    public enum EEvenTypeInternal
    {
        EET_InValid,

        //动画状态相关


        // 角色行为相关 
        [Description("引擎/环境检测/射线检测")] EET_RayCast,


        // 动画跳转事件
        [Description("动画跳转")] EET_Interrupt,
        [Description("动画跳转[仅结束时跳转]")] EET_Interrupt_E,
        [Description("引擎/强制跳转Action")] EET_ChangeAction,
        [Description("引擎/重置相机挂点角度")] Event_ResetCamAngle,

        //道具交互

        // 杂项
        [Description("引擎/蓝图公式(Int)")] EET_GEquationInt,
        [Description("引擎/蓝图公式(Float)")] EET_GEquationFloat,

        [Description("引擎/SetGvalue(蓝图)/Bool")] EET_SetGBool,
        [Description("引擎/SetGvalue(蓝图)/Int")] EET_SetGInt,
        [Description("引擎/SetGvalue(蓝图)/Enum")] EET_SetGEnum,
        [Description("引擎/SetGvalue(蓝图)/Float")] EET_SetGFloat,
        [Description("引擎/SetGvalue(蓝图)/Point")] EET_SetGPoint,
        [Description("引擎/SetGvalue(蓝图)/Unit")] EET_SetGUnit,
        [Description("引擎/SetGvalue(蓝图)/GroupFloat")] EET_SetGGroupFloat,
        [Description("引擎/SetGvalue(蓝图)/GroupInt")] EET_SetGGroupInt,
        [Description("引擎/SetGvalue(蓝图)/GroupBool")] EET_SetGGroupBool,
        [Description("引擎/SetGvalue(蓝图)/GroupString")] EET_SetGGroupString,
        [Description("引擎/SetGvalue(蓝图)/GroupUnit")] EET_SetGGroupUnit,

        //单位相关
        [Description("攻击盒")] EET_AttackBox,
        [Description("攻击路径")] EET_AttackPath,
        [Description("引擎/创建技能")] EET_CreateSkill,
        [Description("引擎/技能发射器")] EET_SkillEmitter,
        [Description("引擎/技能实体")] EET_SkillEntity,

        //输入
        [Description("引擎/屏蔽输入")] EET_DisableAutofill,

        //单位状态
        [Description("引擎/设置挂点对象的显隐")] EET_SetObjActive,
        [Description("引擎/设置优先ActionGroupID")] EET_SetFirstActionGroupID,
        [Description("引擎/标记单位存亡状态")] EET_MarkUnitDeathState,
        [Description("引擎/播放动画组件")] EET_PlayAnimation,
        [Description("引擎/层级碰撞忽略")] EET_IgnoreLayerCollision,
        [Description("引擎/单位碰撞忽略")] EET_IgnoreCollision,
        [Description("引擎/设置单位层级")] EET_SetLayer,
        [Description("引擎/设置动画层级权重")] EET__SetLayerWeight,
        [Description("引擎/中止当前Action层级逻辑")] EET_StopActionState,

        [Description("引擎/形状检测(球-胶囊-扇形)")] EET_DetectShape,

        //不希望能在轨道配置的事件
        [Description("动画播放")] EET_DTD_PlayAnim,
    }

    public enum EInputKeyType
    {
        [Description("按下")] OnDown,
        [Description("松开")] OnUp,
        [Description("点击")] OnClick,
        [Description("长按")] OnHold,
        [Description("按键状态_按下")] Down_State,
        [Description("按键状态_抬起")] Up_State,
    }

    //Animator临时解决方案
    // public enum EAnimLayerType
    // {
    //     [Description("动画主要层级")] BaseLayer,
    //     [Description("单个肢体层级")] LimbLayer,
    //     [Description("瞄准偏移层级")] UpperLayer,
    //     [Description("抖动叠加层级")] NoiseLayer,
    //     [Description("程序逻辑层级")] ScriptLayer
    // }

    public enum EInterruptTypeInternal
    {
        EIT_InValid,
        [Description("引擎/玩家按键检测")] EIT_CheckInput,
        [Description("引擎/玩家移动输入检测")] EIT_CheckMove,
        [Description("引擎/检查Action执行状态")] EIT_CheckActionState,
        [Description("引擎/检查当前Action层级跳转状态")] EIT_CheckLayerJumpState,
        [Description("引擎/被命中")] EIT_CheckBeHit,
        [Description("引擎/命中对象")] EIT_CheckOnHit,
        [Description("引擎/随机权重")] EIT_CheckWeightRange,
        [Description("引擎/蓝图/Bool")] EIT_BluePrintBool,
        [Description("引擎/蓝图/替换跳转Action")] EIT_BluePrintAction,
        [Description("引擎/蓝图/玩家按键检测")] EIT_BluePrintKey,
        [Description("引擎/跳转权重")] EIT_CheckJumpWeight,
        [Description("引擎/定时器")] EIT_CheckJumpTimer,
    }

    public enum ECharacteLimbType
    {
        //角色肢体
        Root,
        Head,
        Neck,
        Chest,
        Spine2,
        Spine,
        Hips,
        Left_Upper_Leg,
        Left_Lower_Leg,
        Left_Foot,
        Right_Upper_Leg,
        Right_Lower_Leg,
        Right_Foot,
        Left_Shoulder,
        Left_Upper_Arm,
        Left_Lower_Arm,
        Left_Hand,
        Right_Shoulder,
        Right_Upper_Arm,
        Right_Lower_Arm,
        Right_Hand,

        //常规道具挂点
        HelpPoint_HUD,
        HelpPoint_WeaponL,
        HelpPoint_WeaponR,
        HelpPoint_WorldL,
        helpPoint_World,
        HelpPoint_WorldR,
        HelpPoint_BehindL,
        HelpPoint_BehindR,
        HelpPoint_WaistL,
        HelpPoint_WaistR,

        //相机挂点
        Cam_Main,
        Cam_Look,
        Cam_Ani_A,
        Cam_Ani_B,

        //IK挂点
        IKPoint_L_01,
        IKPoint_L_02,
        IKPoint_L_03,
        IKPoint_R_01,
        IKPoint_R_02,
        IKPoint_R_03,

        //道具挂点
        Weapone_L,
        Weapone_R,
        Attach,
        HUD,

        // 杂项
        LightGroup,
        HitPoint,

        // 拖尾挂点
        Trail_L1,
        Trail_L2,
        Trail_R1,
        Trail_R2
    }

    public enum EGValueType
    {
        // Null,
        GBool,
        GInt,
        GFloat,
        GString,
        GEnum,
        // GColor,
        // GVector2,
        // GVector3,
        // GQuaternion,
        // GUnit,
        GTransform,
        GPoint,
        GUnit,
        GGroupBool,
        GGroupInt,
        GGroupFloat,
        GGroupString,
        GGroupTransform,
        GGroupPoint,
        GGroupUnit,
        GDictionary
    }

    public enum EGValueDictionaryKeyType
    {
        Bool,
        Int,
        Float,
        String,
        Enum
    }

    // public enum EAEStype
    // {
    //     OnHit,
    // }

    public enum Ecomp_Int
    {
        [Description("==")] Equal,
        [Description("!=")] NotEqual,
        [Description(">")] Greater,
        [Description("<")] Less,
        [Description(">=")] GreaterEqual,
        [Description("<=")] LessEqual,
    }
    public enum Ecomp_Float
    {
        [Description(">=")] GreaterEqual,
        [Description("<=")] LessEqual,
    }
    public enum Ecomp_Bool
    {
        [Description("==")] Equal,
        [Description("!=")] NotEqual,
    }

    public class EnumNames
    {
        public static string[] Ecomp_Int = new[] { "==", "!=", ">", "<", ">=", "<=" };
        public static string[] Ecomp_Float = new[] { ">=", "<=" };
        public static string[] Ecomp_Bool = new[] { "==", "!=" };
    }

    public enum EUnitType
    {
        Entity,
        Skill
    }
    public enum EInfoType
    {
        InputModule,
        GValue,
        Equation,
        UnitAction,
        CameraWarp,
        UnitWarp,
        PropWarp,
        AssetData,
        Skill,
    }
    public enum EEventTrackState
    {
        Default,
        Active,
    }

    public enum EEquationReturnType
    {
        Float,
        Int,
        Bool,
        Unit,
        Vector3,
        Point,
        GroupFloat,
        GroupInt,
        GroupUnit,
        String,
    }
}
