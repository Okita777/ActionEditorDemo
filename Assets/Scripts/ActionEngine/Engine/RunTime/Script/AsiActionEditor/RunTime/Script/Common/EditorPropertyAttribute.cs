using System;
using System.Diagnostics;

namespace AsiActionEngine.RunTime
{
    public enum EditorPropertyType
    {
        EEPT_Bool,
        EEPT_Int,
        EEPT_Float,
        EEPT_String,
        EEPT_Vector2,
        EEPT_Vector3,
        EEPT_Vector4,
        EEPT_Color,
        EEPT_Quaternion,
        EEPT_Enum,
        EEPT_Unit,
        EEPT_EnumToActionConfig,
        EEPT_LayerMask,
        EEPT_ObjectLayer,
        EEPT_Object,
        EEPT_AnimationCurve,
        EEPT_GUnit,
        EEPT_GBool,
        EEPT_GInt,
        EEPT_GFloat,
        EEPT_GEnum,
        EEPT_GString,
        EEPT_GGroupBool,
        EEPT_GGroupInt,
        EEPT_GGroupFloat,
        EEPT_GGroupString,
        //EEPT_GGroupBool_Select,
        //EEPT_GGroupInt_Select,
        //EEPT_GGroupFloat_Select,
        EEPT_GGroupString_Select,
        EEPT_GGroupTransform,
        EEPT_GGroupUnit,
        EEPT_GGroupPoint,

        EEPT_GameObject,
        EEPT_RunTimeAnimator,
        EEPT_OverAnimator,

        //下拉列表
        EEPT_AnimatorState,
        EEPT_AnimatorParam,
        EEPT_CustomProperty,
        EEPT_Action,
        EEPT_ActionLable,
        EEPT_ActionLayer,
        EEPT_ActionInfo,
        EEPT_CharacteLimbType,
        EEPT_Camera,
        EEPT_SetGBool,
        EEPT_SetGInt,
        EEPT_SetGEnum,
        EEPT_SetGFloat,
        EEPT_SetGString,
        EEPT_SetGPoint,
        EEPT_SetGTransform,
        EEPT_SetGUnit,
        EEPT_GTransform,
        EEPT_GPoint,
        EEPT_SelectTransform,

        //列表
        EEPT_List,
        EEPT_EnumList,
        EEPT_Skill,
        EEPT_SkillAction,
        EEPT_GameObjectList,
        EEPT_ActionList,
        EEPT_SkillActionList,
        EEPT_GValueSetting,
        EEPT_GValueCopySetting,
        EEPT_GValueSRatio,
        EEPT_GraphValue,
        EEPT_GEquation,
        EEPT_EnumMask,
        EEPT_CurveObject,
        EEPT_Gradient,
        EEPT_ColorObject,
        EEPT_Texture,

        EEPT_UnitTypeTag,
        EEPT_SkillTypeTag,

        EEPT_ListUnit,
        EEPT_ListProp,

        // Rendering Layer（非 Physics User Layer），对应 Light.renderingLayerMask，
        // 下拉项名称来自当前 RenderPipelineAsset 的 renderingLayerMaskNames
        EEPT_RenderingLayerMask,

        // 固定签名的 GDictionary 引用
        EEPT_GDictionary,
    }

    public enum EditorGraphPropertyType
    {
        EEPT_Bool = 1,
        EEPT_Int,
        EEPT_Float,
        EEPT_String,
        EEPT_Vector3,
        EEPT_Transform,
        EEPT_LayerMask,
        EEPT_CharacteLimbType,
        EEPT_CharacteLimbTypeAll,
        EEPT_PointData,
        EEPT_GroupInt,
        EEPT_GroupFloat,
        EEPT_GroupUnit,
        EEPT_GroupTransform,
        EEPT_GroupPoint,
        EEPT_GBool,
        EEPT_GInt,
        EEPT_GFloat,
        EEPT_GEnum,
        EEPT_GString,
        EEPT_GVector3,
        EEPT_GUnit,
        EEPT_GTransform,
        EEPT_GPoint,
        EEPT_GGroupInt,
        EEPT_GGroupFloat,
        EEPT_GGroupBool,
        EEPT_GGroupString,
        EEPT_GGroupUnit,
        EEPT_GGroupTransform,
        EEPT_GGroupPoint,
        EEPT_GGroupValue,
        EEPT_Enum,
        EEPT_EnumCustom,
        EEPT_GEquation,
        EEPT_Color,

        EEPT_UnitTypeTag,
        EEPT_SkillTypeTag,
        EEPT_Skill,

        EEPT_GroupBool,
        EEPT_GroupString,

        EEPT_GDictionary,
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class EditorGraphPropertyAttribute : Attribute
    {
        private string mPropertyName;
        private EditorGraphPropertyType mPropertyType;
        private bool mIsInput;
        private string mDescription;
        private string[] mEnumNames;
        private float mLabelWidth;
        private bool mRequired = false;
        private bool mOnCheckGEnum = false;

        /// <summary>
        /// 控制柄的绘制
        /// </summary>
        /// <param name="name">控制柄描述</param>
        /// <param name="isInput">是否为输入端</param>
        /// <param name="type">控制柄参数类型</param>
        public EditorGraphPropertyAttribute(string name, bool isInput, EditorGraphPropertyType type)
        {
            mPropertyName = name;
            mPropertyType = type;
            mIsInput = isInput;
            mLabelWidth = 50;
        }

        #region Property
        public string PropertyName
        {
            get { return mPropertyName; }
            set { mPropertyName = value; }
        }
        public EditorGraphPropertyType PropertyType
        {
            get { return mPropertyType; }
            set { mPropertyType = value; }
        }
        public bool IsInput
        {
            get { return mIsInput; }
            set { mIsInput = value; }
        }
        public bool Required
        {
            get { return mRequired; }
            set { mRequired = value; }
        }
        public bool OnCheckGEnum
        {
            get { return mOnCheckGEnum; }
            set { mOnCheckGEnum = value; }
        }
        public string Tooltip
        {
            get { return mDescription; }
            set { mDescription = value; }
        }

        public string[] EnumNames
        {
            get { return mEnumNames; }
            set
            {
                // EngineDebug.LogWarning("设置枚举");
                mEnumNames = value;
            }
        }
        public float LabelWidth
        {
            get { return mLabelWidth; }
            set { mLabelWidth = value; }
        }
        #endregion
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class EditorPropertyAttribute : Attribute
    {
        private string mPropertyName;
        private EditorPropertyType mPropertyType;
        private bool mEdit;
        private string mDescription;
        private string[] mEnumNames;
        private float mLabelWidth;
        private bool mRequired = false;


        public EditorPropertyAttribute(string name, EditorPropertyType type)
        {
            mPropertyName = name;
            mPropertyType = type;
            mEdit = true;
            mLabelWidth = 100;
        }

        #region Property
        public string PropertyName
        {
            get { return mPropertyName; }
            set { mPropertyName = value; }
        }
        public EditorPropertyType PropertyType
        {
            get { return mPropertyType; }
            set { mPropertyType = value; }
        }
        public bool Edit
        {
            get { return mEdit; }
            set { mEdit = value; }
        }
        public string Tooltip
        {
            get { return mDescription; }
            set { mDescription = value; }
        }
        public bool Required
        {
            get { return mRequired; }
            set { mRequired = value; }
        }
        public string[] EnumNames
        {
            get { return mEnumNames; }
            set
            {
                // EngineDebug.LogWarning("设置枚举");
                mEnumNames = value;
            }
        }
        public float LabelWidth
        {
            get { return mLabelWidth; }
            set { mLabelWidth = value; }
        }
        #endregion
    }
}