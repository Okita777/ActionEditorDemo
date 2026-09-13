using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    //蓝图类型，以输出的变量类型分类
    [System.Serializable]
    public abstract class BluePrint_Vector3 : BluePrint_Value
    {
        public abstract Vector3 value { get; }
    }
    // [System.Serializable] public abstract class BluePrint_Quaternion : BluePrint_Value
    // {
    //     public abstract Quaternion value { get; }
    // }
    [System.Serializable]
    public abstract class BluePrint_Int : BluePrint_Value_Number​
    {
        public abstract int value { get; }
        public override object number() => value;
        public override bool isFloat() => false;
    }
    [System.Serializable]
    public abstract class BluePrint_Float : BluePrint_Value_Number​
    {
        public abstract float value { get; }
        public override object number() => value;
        public override bool isFloat() => true;
    }
    [System.Serializable]
    public abstract class BluePrint_Bool : BluePrint_Value
    {
        public abstract bool value { get; }
    }
    [System.Serializable]
    public abstract class BluePrint_String : BluePrint_Value
    {
        public abstract string value { get; }
    }
    [System.Serializable]
    public abstract class BluePrint_GDictionary : BluePrint_Value
    {
        public abstract GDictionary value { get; }
        public abstract ActionStatePart TargetPart { get; }
    }
    [System.Serializable]
    public abstract class BluePrint_PointData : BluePrint_Value_Trans
    {
        public abstract PointData value { get; }
        public override Vector3 position() => value.pos;
        public override Quaternion rotation() => value.rot;
    }
    [System.Serializable]
    public abstract class BluePrint_Transform : BluePrint_Value_Trans
    {
        public abstract Transform value { get; }
        public override Vector3 position() => value.position;
        public override Quaternion rotation() => value.rotation;
    }
    [System.Serializable]
    public abstract class BluePrint_Unit : BluePrint_Value_Trans
    {
        public abstract TargetUnit value { get; }
        public abstract bool isValid(ActionStatePart part);
        public override Vector3 position() => value.GetUnit().transform.position;
        public override Quaternion rotation() => value.GetUnit().transform.rotation;
    }

    #region GValue
    [System.Serializable]
    public abstract class BluePrint_GroupInt : BluePrint_Value_List
    {
        public abstract List<int> value { get; }
        public override int GetLength() => value.Count;
    }
    [System.Serializable]
    public abstract class BluePrint_GroupFloat : BluePrint_Value_List
    {
        public abstract List<float> value { get; }
        public override int GetLength() => value.Count;
    }
    [System.Serializable]
    public abstract class BluePrint_GroupPointData : BluePrint_Value_List
    {

        public abstract List<PointData> value { get; }
        public override int GetLength() => value.Count;
    }
    [System.Serializable]
    public abstract class BluePrint_GroupTransform : BluePrint_Value_List
    {
        public abstract List<Transform> value { get; }
        public override int GetLength() => value.Count;
    }
    [System.Serializable]
    public abstract class BluePrint_GroupUnit : BluePrint_Value_List
    {
        public abstract List<ActionEngine_Unit> value { get; }
        public override int GetLength() => value.Count;
    }
    [System.Serializable]
    public abstract class BluePrint_GroupBool : BluePrint_Value_List
    {
        public abstract List<bool> value { get; }
        public override int GetLength() => value.Count;
    }
    [System.Serializable]
    public abstract class BluePrint_GroupString : BluePrint_Value_List
    {
        public abstract List<string> value { get; }
        public override int GetLength() => value.Count;
    }
    [System.Serializable]
    public abstract class BluePrint_GBool : BluePrint_Value
    {
        public abstract GBool value(ActionStatePart part, ActionMachineTime _time);
    }
    [System.Serializable]
    public abstract class BluePrint_GInt : BluePrint_Value
    {
        public abstract GInt value(ActionStatePart part, ActionMachineTime _time);
    }
    [System.Serializable]
    public abstract class BluePrint_GFloat : BluePrint_Value
    {
        public abstract GFloat value(ActionStatePart part, ActionMachineTime _time);
    }
    #endregion

    #region 蓝图类型，输出的值为常量的类型
    [System.Serializable]
    public abstract class BluePrint_GPoint : BluePrint_Value
    {
        public abstract GPoint value(ActionStatePart part, ActionMachineTime _time);
    }
    [System.Serializable]
    public abstract class BluePrint_Value_Number​ : BluePrint_Value
    {
        public abstract object number();
        public abstract bool isFloat();
    }
    [System.Serializable]
    public abstract class BluePrint_Value_Trans : BluePrint_Value
    {
        public abstract Vector3 position();
        public abstract Quaternion rotation();
    }

    [System.Serializable]
    public abstract class BluePrint_Value_List : BluePrint_Value
    {
        public abstract int GetLength();
    }
    [System.Serializable]
    public abstract class BluePrint_Value : IProperty
    {
        public bool IsNode = false;

        /// <summary>
        /// 仅在当前帧第一次获取参数时执行一次, 避免因多次调用引起重复运算
        /// </summary>
        /// <param name="part"></param>
        /// <param name="_time"></param>
        public abstract void Init(ActionStatePart part, ActionMachineTime _time);
        public abstract BluePrint_Value Clone();
    }
    #endregion

}