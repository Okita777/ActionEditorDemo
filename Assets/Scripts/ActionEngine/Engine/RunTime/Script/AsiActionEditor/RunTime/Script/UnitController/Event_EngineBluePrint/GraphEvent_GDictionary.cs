using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime.Graph
{
    public interface IGDictionaryTypedPortHolder
    {
        BluePrint_GDictionary DictionaryPort { get; }
        GDictionaryKeyBinding KeyBinding { get; }
        EGValueType ExpectedValueType { get; }
    }

    public interface IGDictionaryValuePortHolder
    {
        BluePrint_GDictionary DictionaryPort { get; }
        GDictionaryValueBinding ValueBinding { get; }
    }

    public interface IBluePrintAdditionalOutputHolder
    {
        int AdditionalOutputCount { get; }
        BluePrint_Value GetAdditionalOutput(int index);
        string GetAdditionalOutputName(int index);
    }

    public interface IBluePrintAdditionalOutput
    {
        BluePrint_Value OwnerNode { get; }
    }

    public interface IGDictionaryGetResult
    {
        bool Found { get; }
        void InitGetResult(ActionStatePart part, ActionMachineTime time);
    }

    [Serializable]
    public sealed class GDictionaryKeyBinding
    {
        [SerializeField] private EGValueDictionaryKeyType m_KeyType;
        [SerializeReference] private BluePrint_Value m_Pin = new GraphEvent_Value_Bool();

        public EGValueDictionaryKeyType KeyType => m_KeyType;
        public BluePrint_Value Pin
        {
            get => m_Pin;
            set => m_Pin = value;
        }

        public GDictionaryKey Evaluate(ActionStatePart part, ActionMachineTime time)
        {
            if (m_Pin == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    $"GDictionary Key输入为空, KeyType[{m_KeyType}]");
            }

            m_Pin.Init(part, time);
            switch (m_KeyType)
            {
                case EGValueDictionaryKeyType.Bool:
                    return GDictionaryKey.FromBool(RequirePin<BluePrint_Bool>().value);
                case EGValueDictionaryKeyType.Int:
                    return GDictionaryKey.FromInt(RequirePin<BluePrint_Int>().value);
                case EGValueDictionaryKeyType.Float:
                    return GDictionaryKey.FromFloat(RequirePin<BluePrint_Float>().value);
                case EGValueDictionaryKeyType.String:
                    return GDictionaryKey.FromString(RequirePin<BluePrint_String>().value);
                case EGValueDictionaryKeyType.Enum:
                {
                    int value = RequirePin<BluePrint_Int>().value;
                    if (value < byte.MinValue || value > byte.MaxValue)
                    {
                        throw GDictionaryTypeUtility.InvalidArgument(
                            $"GDictionary Enum Key超出Byte范围[0..255], 实际[{value}]");
                    }
                    return GDictionaryKey.FromEnum((byte)value);
                }
                default:
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary Key类型非法[{m_KeyType}]");
            }
        }

        private T RequirePin<T>() where T : BluePrint_Value
        {
            if (m_Pin is T typed)
            {
                return typed;
            }
            throw GDictionaryTypeUtility.InvalidArgument(
                $"GDictionary Key Pin类型不匹配, KeyType[{m_KeyType}], Pin[{m_Pin.GetType().Name}]");
        }

#if UNITY_EDITOR
        public bool SynchronizeEditor(EGValueDictionaryKeyType keyType)
        {
            bool compatible = m_KeyType == keyType &&
                (keyType == EGValueDictionaryKeyType.Bool &&
                 m_Pin is BluePrint_Bool ||
                 (keyType == EGValueDictionaryKeyType.Int ||
                  keyType == EGValueDictionaryKeyType.Enum) &&
                 m_Pin is BluePrint_Int ||
                 keyType == EGValueDictionaryKeyType.Float &&
                 m_Pin is BluePrint_Float ||
                 keyType == EGValueDictionaryKeyType.String &&
                 m_Pin is BluePrint_String);

            bool changed = m_KeyType != keyType;
            m_KeyType = keyType;
            if (!compatible)
            {
                m_Pin = CreateDefaultPinEditor(keyType);
                changed = true;
            }
            return changed;
        }

        public GDictionaryKeyBinding Clone()
        {
            return new GDictionaryKeyBinding
            {
                m_KeyType = m_KeyType,
                m_Pin = m_Pin == null ? null : m_Pin.Clone()
            };
        }

        private static BluePrint_Value CreateDefaultPinEditor(EGValueDictionaryKeyType keyType)
        {
            switch (keyType)
            {
                case EGValueDictionaryKeyType.Bool:
                    return new GraphEvent_Value_Bool();
                case EGValueDictionaryKeyType.Int:
                case EGValueDictionaryKeyType.Enum:
                    return new GraphEvent_Value_Int();
                case EGValueDictionaryKeyType.Float:
                    return new GraphEvent_Value_Float();
                case EGValueDictionaryKeyType.String:
                    return new GraphEvent_Value_String();
                default:
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary Key类型非法[{keyType}]");
            }
        }
#endif
    }

    [Serializable]
    public sealed class GDictionaryValueBinding
    {
        [SerializeField] private EGValueType m_ValueType;
        [SerializeReference] private BluePrint_Value m_Pin = new GraphEvent_Value_Bool();

        public EGValueType ValueType => m_ValueType;
        public BluePrint_Value Pin
        {
            get => m_Pin;
            set => m_Pin = value;
        }

        public GDictionaryValue Evaluate(ActionStatePart part, ActionMachineTime time)
        {
            if (m_Pin == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    $"GDictionary Value输入为空, ValueType[{m_ValueType}]");
            }

            m_Pin.Init(part, time);
            switch (m_ValueType)
            {
                case EGValueType.GBool:
                    return GDictionaryValue.FromBool(RequirePin<BluePrint_Bool>().value);
                case EGValueType.GInt:
                    return GDictionaryValue.FromInt(RequirePin<BluePrint_Int>().value);
                case EGValueType.GFloat:
                    return GDictionaryValue.FromFloat(RequirePin<BluePrint_Float>().value);
                case EGValueType.GString:
                    return GDictionaryValue.FromString(RequirePin<BluePrint_String>().value);
                case EGValueType.GEnum:
                    return GDictionaryValue.FromEnum(
                        GDictionaryBluePrintUtility.RequireByte(
                            RequirePin<BluePrint_Int>().value,
                            "Enum Value"));
                case EGValueType.GPoint:
                    return GDictionaryValue.FromPoint(
                        RequirePin<BluePrint_PointData>().value);
                case EGValueType.GTransform:
                    return GDictionaryValue.FromTransform(
                        RequirePin<BluePrint_Transform>().value);
                case EGValueType.GUnit:
                    return GDictionaryValue.FromUnit(
                        RequirePin<BluePrint_Unit>().value);
                case EGValueType.GGroupBool:
                    return GDictionaryValue.FromGroupBool(
                        GDictionaryBluePrintUtility.ToArray(
                            RequirePin<BluePrint_GroupBool>().value));
                case EGValueType.GGroupInt:
                    return GDictionaryValue.FromGroupInt(
                        GDictionaryBluePrintUtility.ToArray(
                            RequirePin<BluePrint_GroupInt>().value));
                case EGValueType.GGroupFloat:
                    return GDictionaryValue.FromGroupFloat(
                        GDictionaryBluePrintUtility.ToArray(
                            RequirePin<BluePrint_GroupFloat>().value));
                case EGValueType.GGroupString:
                    return GDictionaryValue.FromGroupString(
                        GDictionaryBluePrintUtility.ToArray(
                            RequirePin<BluePrint_GroupString>().value));
                case EGValueType.GGroupPoint:
                    return GDictionaryValue.FromGroupPoint(
                        RequirePin<BluePrint_GroupPointData>().value);
                case EGValueType.GGroupTransform:
                    return GDictionaryValue.FromGroupTransform(
                        RequirePin<BluePrint_GroupTransform>().value);
                case EGValueType.GGroupUnit:
                    return GDictionaryValue.FromGroupUnit(
                        RequirePin<BluePrint_GroupUnit>().value);
                default:
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary Value类型非法[{m_ValueType}]");
            }
        }

        private T RequirePin<T>() where T : BluePrint_Value
        {
            if (m_Pin is T typed)
            {
                return typed;
            }
            throw GDictionaryTypeUtility.InvalidArgument(
                $"GDictionary Value Pin类型不匹配, ValueType[{m_ValueType}], Pin[{m_Pin.GetType().Name}]");
        }

#if UNITY_EDITOR
        public bool SynchronizeEditor(EGValueType valueType)
        {
            bool compatible = m_ValueType == valueType &&
                              IsPinCompatible(valueType, m_Pin);
            bool changed = m_ValueType != valueType;
            m_ValueType = valueType;
            if (!compatible)
            {
                m_Pin = CreateDefaultPinEditor(valueType);
                changed = true;
            }
            return changed;
        }

        public GDictionaryValueBinding Clone()
        {
            return new GDictionaryValueBinding
            {
                m_ValueType = m_ValueType,
                m_Pin = m_Pin == null ? null : m_Pin.Clone()
            };
        }

        private static bool IsPinCompatible(EGValueType valueType, BluePrint_Value pin)
        {
            switch (valueType)
            {
                case EGValueType.GBool:
                    return pin is BluePrint_Bool;
                case EGValueType.GInt:
                case EGValueType.GEnum:
                    return pin is BluePrint_Int;
                case EGValueType.GFloat:
                    return pin is BluePrint_Float;
                case EGValueType.GString:
                    return pin is BluePrint_String;
                case EGValueType.GPoint:
                    return pin is BluePrint_PointData;
                case EGValueType.GTransform:
                    return pin is BluePrint_Transform;
                case EGValueType.GUnit:
                    return pin is BluePrint_Unit;
                case EGValueType.GGroupBool:
                    return pin is BluePrint_GroupBool;
                case EGValueType.GGroupInt:
                    return pin is BluePrint_GroupInt;
                case EGValueType.GGroupFloat:
                    return pin is BluePrint_GroupFloat;
                case EGValueType.GGroupString:
                    return pin is BluePrint_GroupString;
                case EGValueType.GGroupPoint:
                    return pin is BluePrint_GroupPointData;
                case EGValueType.GGroupTransform:
                    return pin is BluePrint_GroupTransform;
                case EGValueType.GGroupUnit:
                    return pin is BluePrint_GroupUnit;
                default:
                    return false;
            }
        }

        private static BluePrint_Value CreateDefaultPinEditor(EGValueType valueType)
        {
            switch (valueType)
            {
                case EGValueType.GBool:
                    return new GraphEvent_Value_Bool();
                case EGValueType.GInt:
                case EGValueType.GEnum:
                    return new GraphEvent_Value_Int();
                case EGValueType.GFloat:
                    return new GraphEvent_Value_Float();
                case EGValueType.GString:
                    return new GraphEvent_Value_String();
                case EGValueType.GPoint:
                    return new GraphEvent_BValue_Point();
                case EGValueType.GTransform:
                    return new GraphEvent_BValue_Transform();
                case EGValueType.GUnit:
                    return new GraphEvent_Value_SelfUnit();
                case EGValueType.GGroupBool:
                    return new GraphEvent_Value_GroupBool();
                case EGValueType.GGroupInt:
                    return new GraphEvent_Value_GroupInt();
                case EGValueType.GGroupFloat:
                    return new GraphEvent_Value_GroupFloat();
                case EGValueType.GGroupString:
                    return new GraphEvent_Value_GroupString();
                case EGValueType.GGroupPoint:
                    return new GraphEvent_GValue_GGPoint();
                case EGValueType.GGroupTransform:
                    return new GraphEvent_Value_GroupTransform();
                case EGValueType.GGroupUnit:
                    return new GraphEvent_Value_GroupUnit();
                default:
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary Value类型非法[{valueType}]");
            }
        }
#endif
    }

    [Serializable]
    public sealed class GraphEvent_GDictionary_GetFound :
        BluePrint_Bool,
        IBluePrintAdditionalOutput
    {
        [SerializeReference] private BluePrint_Value m_OwnerNode;
        [NonSerialized] private GraphEvent_GDictionary_GetFound m_Clone;

        public BluePrint_Value OwnerNode => m_OwnerNode;
        public override bool value
        {
            get
            {
                if (m_OwnerNode is IGDictionaryGetResult result)
                {
                    return result.Found;
                }
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary Get Found输出未绑定有效Owner");
            }
        }

        public GraphEvent_GDictionary_GetFound()
        {
        }

        public GraphEvent_GDictionary_GetFound(BluePrint_Value ownerNode)
        {
            Bind(ownerNode);
        }

        public void Bind(BluePrint_Value ownerNode)
        {
            if (ownerNode is not IGDictionaryGetResult)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary Get Found输出Owner类型无效");
            }
            m_OwnerNode = ownerNode;
        }

        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (m_OwnerNode is not IGDictionaryGetResult result)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary Get Found输出未绑定有效Owner");
            }
            result.InitGetResult(part, time);
        }

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = new GraphEvent_GDictionary_GetFound
                {
                    IsNode = IsNode
                };
                if (m_OwnerNode != null)
                {
                    m_Clone.m_OwnerNode =
                        (BluePrint_Value)m_OwnerNode.Clone();
                }
                ActionSaveFlishEvent.ActionEvent.AddListener(
                    () => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }

    internal readonly struct GDictionaryNodeContext
    {
        internal readonly GDictionary Dictionary;
        internal readonly ActionStatePart TargetPart;
        internal readonly GDictionaryKey Key;

        internal GDictionaryNodeContext(
            GDictionary dictionary,
            ActionStatePart targetPart,
            GDictionaryKey key)
        {
            Dictionary = dictionary;
            TargetPart = targetPart;
            Key = key;
        }
    }

    internal static class GDictionaryBluePrintUtility
    {
        internal static GDictionaryNodeContext Prepare(
            IGDictionaryTypedPortHolder holder,
            ActionStatePart part,
            ActionMachineTime time)
        {
            if (holder.DictionaryPort == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument("GDictionary输入端口为空");
            }

            holder.DictionaryPort.Init(part, time);
            GDictionary dictionary = holder.DictionaryPort.value;
            ActionStatePart targetPart = holder.DictionaryPort.TargetPart;
            if (dictionary == null || targetPart == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary引用节点未返回有效字典或目标ActionStatePart");
            }

            GDictionarySignature signature = dictionary.Signature;
            signature.Validate();
            if (holder.ExpectedValueType != EGValueType.GDictionary &&
                signature.ValueType != holder.ExpectedValueType)
            {
                throw GDictionaryTypeUtility.InvalidSignature(
                    $"GDictionary节点Value签名不匹配, 节点[{holder.ExpectedValueType}], 字典[{signature.ValueType}]");
            }
            if (holder.KeyBinding == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument("GDictionary KeyBinding为空");
            }
            if (holder.KeyBinding.KeyType != signature.KeyType)
            {
                throw GDictionaryTypeUtility.InvalidSignature(
                    $"GDictionary节点Key签名不匹配, 节点[{holder.KeyBinding.KeyType}], 字典[{signature.KeyType}]");
            }

            return new GDictionaryNodeContext(
                dictionary,
                targetPart,
                holder.KeyBinding.Evaluate(part, time));
        }

        internal static byte RequireByte(int value, string role)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    $"GDictionary {role}超出Byte范围[0..255], 实际[{value}]");
            }
            return (byte)value;
        }

        internal static bool[] ToArray(List<bool> source)
        {
            if (source == null || source.Count == 0) return new bool[0];
            bool[] result = new bool[source.Count];
            for (int i = 0; i < source.Count; i++) result[i] = source[i];
            return result;
        }

        internal static int[] ToArray(List<int> source)
        {
            if (source == null || source.Count == 0) return new int[0];
            int[] result = new int[source.Count];
            for (int i = 0; i < source.Count; i++) result[i] = source[i];
            return result;
        }

        internal static float[] ToArray(List<float> source)
        {
            if (source == null || source.Count == 0) return new float[0];
            float[] result = new float[source.Count];
            for (int i = 0; i < source.Count; i++) result[i] = source[i];
            return result;
        }

        internal static string[] ToArray(List<string> source)
        {
            if (source == null || source.Count == 0) return new string[0];
            string[] result = new string[source.Count];
            for (int i = 0; i < source.Count; i++) result[i] = source[i];
            return result;
        }

        internal static List<bool> ToList(bool[] source)
        {
            List<bool> result = EngineResourcesManager.Instance.CreateBools();
            if (source != null) for (int i = 0; i < source.Length; i++) result.Add(source[i]);
            return result;
        }

        internal static List<int> ToList(int[] source)
        {
            List<int> result = EngineResourcesManager.Instance.CreateInts();
            if (source != null) for (int i = 0; i < source.Length; i++) result.Add(source[i]);
            return result;
        }

        internal static List<float> ToList(float[] source)
        {
            List<float> result = EngineResourcesManager.Instance.CreateFloats();
            if (source != null) for (int i = 0; i < source.Length; i++) result.Add(source[i]);
            return result;
        }

        internal static List<string> ToList(string[] source)
        {
            List<string> result = EngineResourcesManager.Instance.CreateStrings();
            if (source != null) for (int i = 0; i < source.Length; i++) result.Add(source[i]);
            return result;
        }
    }

    [Serializable]
    public sealed class GraphEvent_GValue_GDictionary : BluePrint_GDictionary
    {
        [SerializeField] private GDictionary m_Dictionary = new GDictionary();
        [SerializeReference] private BluePrint_Unit m_Unit = new GraphEvent_Value_SelfUnit();
        [NonSerialized] private ActionStatePart m_TargetPart;

        [EditorGraphProperty("Unit", true, EditorGraphPropertyType.EEPT_GUnit)]
        public BluePrint_Unit Unit
        {
            get => m_Unit;
            set => m_Unit = value;
        }

        [EditorGraphProperty("GDictionary", false, EditorGraphPropertyType.EEPT_GDictionary, LabelWidth = 75)]
        public GDictionary Dictionary
        {
            get => m_Dictionary;
            set => m_Dictionary = value;
        }

        public override GDictionary value => m_Dictionary;
        public override ActionStatePart TargetPart => m_TargetPart;

        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            if (m_Dictionary == null || m_Unit == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary引用节点的字典或Unit输入为空");
            }

            if (!m_Unit.IsNode)
            {
                m_TargetPart = part;
                return;
            }

            m_Unit.Init(part, time);
            if (!m_Unit.isValid(part) || m_Unit.value == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary引用节点的Unit输入无效");
            }
            m_TargetPart = m_Unit.value.GetUnit().ActionStateMachine.FirstStatePart;
        }

#if UNITY_EDITOR
        [NonSerialized] private GraphEvent_GValue_GDictionary m_Clone;
#endif
        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = new GraphEvent_GValue_GDictionary
                {
                    m_Dictionary = (GDictionary)m_Dictionary.Clone(),
                    m_Unit = (BluePrint_Unit)m_Unit.Clone(),
                    IsNode = IsNode
                };
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }

    [Serializable]
    public abstract class GraphEvent_GDictionaryBoolBase :
        BluePrint_Bool,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_Bool m_Input = new GraphEvent_Value_Bool();
        [NonSerialized] private bool m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryBoolBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GBool;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_Bool, LabelWidth = 85)]
        public BluePrint_Bool Input { get => m_Input; set => m_Input = value; }
        public override bool value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromBool(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found ? stored.Get<bool>() : false;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryBoolBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_Bool)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_Bool : GraphEvent_GDictionaryBoolBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_Bool : GraphEvent_GDictionaryBoolBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryIntBase :
        BluePrint_Int,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_Int m_Input = new GraphEvent_Value_Int();
        [NonSerialized] private int m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryIntBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GInt;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_Int, LabelWidth = 85)]
        public BluePrint_Int Input { get => m_Input; set => m_Input = value; }
        public override int value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromInt(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found ? stored.Get<int>() : 0;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryIntBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_Int)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_Int : GraphEvent_GDictionaryIntBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_Int : GraphEvent_GDictionaryIntBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryFloatBase :
        BluePrint_Float,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_Float m_Input = new GraphEvent_Value_Float();
        [NonSerialized] private float m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryFloatBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GFloat;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_Float, LabelWidth = 85)]
        public BluePrint_Float Input { get => m_Input; set => m_Input = value; }
        public override float value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromFloat(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found ? stored.Get<float>() : 0f;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryFloatBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_Float)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_Float : GraphEvent_GDictionaryFloatBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_Float : GraphEvent_GDictionaryFloatBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryStringBase :
        BluePrint_String,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_String m_Input = new GraphEvent_Value_String();
        [NonSerialized] private string m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryStringBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GString;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_String, LabelWidth = 85)]
        public BluePrint_String Input { get => m_Input; set => m_Input = value; }
        public override string value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromString(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found ? stored.Get<string>() : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryStringBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_String)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_String : GraphEvent_GDictionaryStringBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_String : GraphEvent_GDictionaryStringBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryEnumBase :
        BluePrint_Int,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_Int m_Input = new GraphEvent_Value_Int();
        [NonSerialized] private int m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryEnumBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GEnum;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_Int, LabelWidth = 85, OnCheckGEnum = true)]
        public BluePrint_Int Input { get => m_Input; set => m_Input = value; }
        public override int value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                byte storedValue = GDictionaryBluePrintUtility.RequireByte(m_Input.value, "Enum Value");
                m_Return = storedValue;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromEnum(storedValue));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found ? stored.Get<byte>() : 0;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryEnumBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_Int)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_Enum : GraphEvent_GDictionaryEnumBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_Enum : GraphEvent_GDictionaryEnumBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryPointBase :
        BluePrint_PointData,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_PointData m_Input = new GraphEvent_BValue_Point();
        [NonSerialized] private PointData m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryPointBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GPoint;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_PointData, LabelWidth = 85)]
        public BluePrint_PointData Input { get => m_Input; set => m_Input = value; }
        public override PointData value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromPoint(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found ? stored.Get<PointData>() : default;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryPointBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_PointData)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_Point : GraphEvent_GDictionaryPointBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_Point : GraphEvent_GDictionaryPointBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryTransformBase :
        BluePrint_Transform,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_Transform m_Input = new GraphEvent_BValue_Transform();
        [NonSerialized] private Transform m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryTransformBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GTransform;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_Transform, LabelWidth = 85)]
        public BluePrint_Transform Input { get => m_Input; set => m_Input = value; }
        public override Transform value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromTransform(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found ? stored.Get<Transform>() : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryTransformBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_Transform)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_Transform : GraphEvent_GDictionaryTransformBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_Transform : GraphEvent_GDictionaryTransformBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryUnitBase :
        BluePrint_Unit,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_Unit m_Input = new GraphEvent_Value_SelfUnit();
        [NonSerialized] private TargetUnit m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryUnitBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GUnit;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_GUnit, LabelWidth = 85)]
        public BluePrint_Unit Input { get => m_Input; set => m_Input = value; }
        public override TargetUnit value => m_Return;
        public override bool isValid(ActionStatePart part) => m_Return != null;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromUnit(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found ? stored.Get<TargetUnit>() : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryUnitBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_Unit)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_Unit : GraphEvent_GDictionaryUnitBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_Unit : GraphEvent_GDictionaryUnitBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryGroupBoolBase :
        BluePrint_GroupBool,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_GroupBool m_Input = new GraphEvent_Value_GroupBool();
        [NonSerialized] private List<bool> m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryGroupBoolBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GGroupBool;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_GroupBool, LabelWidth = 85)]
        public BluePrint_GroupBool Input { get => m_Input; set => m_Input = value; }
        public override List<bool> value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromGroupBool(GDictionaryBluePrintUtility.ToArray(m_Return)));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found
                    ? GDictionaryBluePrintUtility.ToList(stored.Get<bool[]>())
                    : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryGroupBoolBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_GroupBool)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_GroupBool : GraphEvent_GDictionaryGroupBoolBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_GroupBool : GraphEvent_GDictionaryGroupBoolBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryGroupIntBase :
        BluePrint_GroupInt,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_GroupInt m_Input = new GraphEvent_Value_GroupInt();
        [NonSerialized] private List<int> m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryGroupIntBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GGroupInt;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_GroupInt, LabelWidth = 85)]
        public BluePrint_GroupInt Input { get => m_Input; set => m_Input = value; }
        public override List<int> value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromGroupInt(GDictionaryBluePrintUtility.ToArray(m_Return)));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found
                    ? GDictionaryBluePrintUtility.ToList(stored.Get<int[]>())
                    : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryGroupIntBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_GroupInt)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_GroupInt : GraphEvent_GDictionaryGroupIntBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_GroupInt : GraphEvent_GDictionaryGroupIntBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryGroupFloatBase :
        BluePrint_GroupFloat,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_GroupFloat m_Input = new GraphEvent_Value_GroupFloat();
        [NonSerialized] private List<float> m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryGroupFloatBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GGroupFloat;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_GroupFloat, LabelWidth = 85)]
        public BluePrint_GroupFloat Input { get => m_Input; set => m_Input = value; }
        public override List<float> value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromGroupFloat(GDictionaryBluePrintUtility.ToArray(m_Return)));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found
                    ? GDictionaryBluePrintUtility.ToList(stored.Get<float[]>())
                    : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryGroupFloatBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_GroupFloat)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_GroupFloat : GraphEvent_GDictionaryGroupFloatBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_GroupFloat : GraphEvent_GDictionaryGroupFloatBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryGroupStringBase :
        BluePrint_GroupString,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_GroupString m_Input = new GraphEvent_Value_GroupString();
        [NonSerialized] private List<string> m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryGroupStringBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GGroupString;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_GroupString, LabelWidth = 85)]
        public BluePrint_GroupString Input { get => m_Input; set => m_Input = value; }
        public override List<string> value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromGroupString(GDictionaryBluePrintUtility.ToArray(m_Return)));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found
                    ? GDictionaryBluePrintUtility.ToList(stored.Get<string[]>())
                    : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryGroupStringBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_GroupString)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_GroupString : GraphEvent_GDictionaryGroupStringBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_GroupString : GraphEvent_GDictionaryGroupStringBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryGroupPointBase :
        BluePrint_GroupPointData,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_GroupPointData m_Input = new GraphEvent_GValue_GGPoint();
        [NonSerialized] private List<PointData> m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryGroupPointBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GGroupPoint;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_GroupPoint, LabelWidth = 85)]
        public BluePrint_GroupPointData Input { get => m_Input; set => m_Input = value; }
        public override List<PointData> value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromGroupPoint(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found
                    ? stored.Get<List<PointData>>()
                    : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryGroupPointBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_GroupPointData)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_GroupPoint : GraphEvent_GDictionaryGroupPointBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_GroupPoint : GraphEvent_GDictionaryGroupPointBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryGroupTransformBase :
        BluePrint_GroupTransform,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_GroupTransform m_Input = new GraphEvent_Value_GroupTransform();
        [NonSerialized] private List<Transform> m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryGroupTransformBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GGroupTransform;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_GroupTransform, LabelWidth = 85)]
        public BluePrint_GroupTransform Input { get => m_Input; set => m_Input = value; }
        public override List<Transform> value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromGroupTransform(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found
                    ? stored.Get<List<Transform>>()
                    : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryGroupTransformBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_GroupTransform)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_GroupTransform : GraphEvent_GDictionaryGroupTransformBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_GroupTransform : GraphEvent_GDictionaryGroupTransformBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryGroupUnitBase :
        BluePrint_GroupUnit,
        IGDictionaryTypedPortHolder,
        IGDictionaryGetResult
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [SerializeReference] private BluePrint_GroupUnit m_Input = new GraphEvent_Value_GroupUnit();
        [NonSerialized] private List<ActionEngine_Unit> m_Return;
        [NonSerialized] private bool m_Found;
        [NonSerialized] private GraphEvent_GDictionaryGroupUnitBase m_Clone;
        protected abstract bool IsSet { get; }
        public EGValueType ExpectedValueType => EGValueType.GGroupUnit;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        [EditorGraphProperty("Value", true, EditorGraphPropertyType.EEPT_GroupUnit, LabelWidth = 85)]
        public BluePrint_GroupUnit Input { get => m_Input; set => m_Input = value; }
        public override List<ActionEngine_Unit> value => m_Return;
        public bool Found => m_Found;
        public void InitGetResult(ActionStatePart part, ActionMachineTime time) =>
            Init(part, time);
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            if (IsSet)
            {
                m_Input.Init(part, time);
                m_Return = m_Input.value;
                c.Dictionary.Set(c.TargetPart, c.Key, GDictionaryValue.FromGroupUnit(m_Return));
            }
            else
            {
                m_Found = c.Dictionary.TryGet(
                    c.TargetPart,
                    c.Key,
                    out GDictionaryValue stored);
                m_Return = m_Found
                    ? stored.Get<List<ActionEngine_Unit>>()
                    : null;
            }
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryGroupUnitBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.m_Input = (BluePrint_GroupUnit)m_Input.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_GetOrDefault_GroupUnit : GraphEvent_GDictionaryGroupUnitBase { protected override bool IsSet => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Set_GroupUnit : GraphEvent_GDictionaryGroupUnitBase { protected override bool IsSet => true; }

    [Serializable]
    public abstract class GraphEvent_GDictionaryKeyBoolBase : BluePrint_Bool, IGDictionaryTypedPortHolder
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryKeyBinding m_Key = new GDictionaryKeyBinding();
        [NonSerialized] private bool m_Return;
        [NonSerialized] private GraphEvent_GDictionaryKeyBoolBase m_Clone;
        protected abstract bool IsRemove { get; }
        public EGValueType ExpectedValueType => EGValueType.GDictionary;
        public GDictionaryKeyBinding KeyBinding => m_Key;
        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        public override bool value => m_Return;
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            GDictionaryNodeContext c = GDictionaryBluePrintUtility.Prepare(this, part, time);
            m_Return = IsRemove
                ? c.Dictionary.Remove(c.TargetPart, c.Key)
                : c.Dictionary.Contains(c.TargetPart, c.Key);
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryKeyBoolBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.m_Key = m_Key.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_Contains : GraphEvent_GDictionaryKeyBoolBase { protected override bool IsRemove => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Remove : GraphEvent_GDictionaryKeyBoolBase { protected override bool IsRemove => true; }

    [Serializable]
    public sealed class GraphEvent_GDictionary_ContainsValue :
        BluePrint_Bool,
        IGDictionaryValuePortHolder
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary =
            new GraphEvent_GValue_GDictionary();
        [SerializeField] private GDictionaryValueBinding m_Value =
            new GDictionaryValueBinding();
        [NonSerialized] private bool m_Return;
        [NonSerialized] private GraphEvent_GDictionary_ContainsValue m_Clone;

        public BluePrint_GDictionary DictionaryPort => m_Dictionary;
        public GDictionaryValueBinding ValueBinding => m_Value;

        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary
        {
            get => m_Dictionary;
            set => m_Dictionary = value;
        }

        public override bool value => m_Return;

        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            if (m_Dictionary == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary输入端口为空");
            }

            m_Dictionary.Init(part, time);
            GDictionary dictionary = m_Dictionary.value;
            ActionStatePart targetPart = m_Dictionary.TargetPart;
            if (dictionary == null || targetPart == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary引用节点未返回有效字典或目标ActionStatePart");
            }

            GDictionarySignature signature = dictionary.Signature;
            signature.Validate();
            if (m_Value == null || m_Value.ValueType != signature.ValueType)
            {
                throw GDictionaryTypeUtility.InvalidSignature(
                    $"GDictionary ContainsValue签名不匹配, 节点[{m_Value?.ValueType}], 字典[{signature.ValueType}]");
            }

            m_Return = dictionary.ContainsValue(
                targetPart,
                m_Value.Evaluate(part, time));
        }

        public override BluePrint_Value Clone()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = new GraphEvent_GDictionary_ContainsValue
                {
                    m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone(),
                    m_Value = m_Value.Clone(),
                    IsNode = IsNode
                };
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }

    [Serializable]
    public abstract class GraphEvent_GDictionaryCountBase : BluePrint_Int
    {
        [SerializeReference] private BluePrint_GDictionary m_Dictionary = new GraphEvent_GValue_GDictionary();
        [NonSerialized] private int m_Return;
        [NonSerialized] private GraphEvent_GDictionaryCountBase m_Clone;
        protected abstract bool IsClear { get; }
        [EditorGraphProperty("Dictionary", true, EditorGraphPropertyType.EEPT_GDictionary)]
        public BluePrint_GDictionary Dictionary { get => m_Dictionary; set => m_Dictionary = value; }
        public override int value => m_Return;
        public override void Init(ActionStatePart part, ActionMachineTime time)
        {
            if (part.ActionStateMachine.BluePrintIsUse(this)) return;
            if (m_Dictionary == null)
                throw GDictionaryTypeUtility.InvalidArgument("GDictionary输入端口为空");
            m_Dictionary.Init(part, time);
            if (m_Dictionary.value == null || m_Dictionary.TargetPart == null)
                throw GDictionaryTypeUtility.InvalidArgument("GDictionary输入端口未返回有效字典或目标");
            m_Return = m_Dictionary.value.Count(m_Dictionary.TargetPart);
            if (IsClear) m_Dictionary.value.Clear(m_Dictionary.TargetPart);
        }
        public override BluePrint_Value Clone() => CloneNode();
        private BluePrint_Value CloneNode()
        {
#if UNITY_EDITOR
            if (m_Clone == null)
            {
                m_Clone = (GraphEvent_GDictionaryCountBase)Activator.CreateInstance(GetType());
                m_Clone.m_Dictionary = (BluePrint_GDictionary)m_Dictionary.Clone();
                m_Clone.IsNode = IsNode;
                ActionSaveFlishEvent.ActionEvent.AddListener(() => m_Clone = null);
            }
            return m_Clone;
#endif
            return this;
        }
    }
    [Serializable] public sealed class GraphEvent_GDictionary_Count : GraphEvent_GDictionaryCountBase { protected override bool IsClear => false; }
    [Serializable] public sealed class GraphEvent_GDictionary_Clear : GraphEvent_GDictionaryCountBase { protected override bool IsClear => true; }
}
