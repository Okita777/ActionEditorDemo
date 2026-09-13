using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [Serializable]
    public struct GDictionarySignature : IEquatable<GDictionarySignature>
    {
        [SerializeField] private EGValueDictionaryKeyType mKeyType;
        [SerializeField] private EGValueType mValueType;

        public EGValueDictionaryKeyType KeyType => mKeyType;
        public EGValueType ValueType => mValueType;

        public GDictionarySignature(EGValueDictionaryKeyType keyType, EGValueType valueType)
        {
            mKeyType = keyType;
            mValueType = valueType;
        }

        public void Validate()
        {
            GDictionaryTypeUtility.ValidateSignature(this);
        }

        public bool Equals(GDictionarySignature other)
        {
            return mKeyType == other.mKeyType && mValueType == other.mValueType;
        }

        public override bool Equals(object obj)
        {
            return obj is GDictionarySignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)mKeyType * 397) ^ (int)mValueType;
            }
        }

        public static bool operator ==(GDictionarySignature left, GDictionarySignature right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GDictionarySignature left, GDictionarySignature right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{mKeyType}->{mValueType}";
        }
    }

    [Serializable]
    public struct GDictionaryKey : IEquatable<GDictionaryKey>
    {
        [SerializeField] private EGValueDictionaryKeyType mKeyType;
        [SerializeField] private bool mBoolValue;
        [SerializeField] private int mIntValue;
        [SerializeField] private float mFloatValue;
        [SerializeField] private string mStringValue;
        [SerializeField] private byte mEnumValue;

        public EGValueDictionaryKeyType KeyType => mKeyType;

        public static GDictionaryKey FromBool(bool value)
        {
            return new GDictionaryKey
            {
                mKeyType = EGValueDictionaryKeyType.Bool,
                mBoolValue = value
            };
        }

        public static GDictionaryKey FromInt(int value)
        {
            return new GDictionaryKey
            {
                mKeyType = EGValueDictionaryKeyType.Int,
                mIntValue = value
            };
        }

        public static GDictionaryKey FromFloat(float value)
        {
            ValidateFloat(value);
            return new GDictionaryKey
            {
                mKeyType = EGValueDictionaryKeyType.Float,
                mFloatValue = value
            };
        }

        public static GDictionaryKey FromString(string value)
        {
            if (value == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary String Key禁止为null，请显式使用string.Empty");
            }
            return new GDictionaryKey
            {
                mKeyType = EGValueDictionaryKeyType.String,
                mStringValue = value
            };
        }

        public static GDictionaryKey FromEnum(byte value)
        {
            return new GDictionaryKey
            {
                mKeyType = EGValueDictionaryKeyType.Enum,
                mEnumValue = value
            };
        }

        public bool GetBool()
        {
            EnsureType(EGValueDictionaryKeyType.Bool);
            return mBoolValue;
        }

        public int GetInt()
        {
            EnsureType(EGValueDictionaryKeyType.Int);
            return mIntValue;
        }

        public float GetFloat()
        {
            EnsureType(EGValueDictionaryKeyType.Float);
            ValidateFloat(mFloatValue);
            return mFloatValue;
        }

        public string GetString()
        {
            EnsureType(EGValueDictionaryKeyType.String);
            return mStringValue;
        }

        public byte GetEnum()
        {
            EnsureType(EGValueDictionaryKeyType.Enum);
            return mEnumValue;
        }

        public void Validate(EGValueDictionaryKeyType expectedType)
        {
            GDictionaryTypeUtility.ValidateKeyType(mKeyType);
            EnsureType(expectedType);
            if (mKeyType == EGValueDictionaryKeyType.Float)
            {
                ValidateFloat(mFloatValue);
            }
            else if (mKeyType == EGValueDictionaryKeyType.String && mStringValue == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary String Key禁止为null，请显式使用string.Empty");
            }
        }

        internal static GDictionaryKey Create<TKey>(TKey value)
        {
            Type type = typeof(TKey);
            if (type == typeof(bool))
            {
                return FromBool((bool)(object)value);
            }
            if (type == typeof(int))
            {
                return FromInt((int)(object)value);
            }
            if (type == typeof(float))
            {
                return FromFloat((float)(object)value);
            }
            if (type == typeof(string))
            {
                return FromString((string)(object)value);
            }
            if (type == typeof(byte))
            {
                return FromEnum((byte)(object)value);
            }
            throw GDictionaryTypeUtility.InvalidArgument(
                $"GDictionary不支持Key CLR类型[{type.FullName}]；GEnum Key必须使用Byte");
        }

        public bool Equals(GDictionaryKey other)
        {
            if (mKeyType != other.mKeyType)
            {
                return false;
            }

            switch (mKeyType)
            {
                case EGValueDictionaryKeyType.Bool:
                    return mBoolValue == other.mBoolValue;
                case EGValueDictionaryKeyType.Int:
                    return mIntValue == other.mIntValue;
                case EGValueDictionaryKeyType.Float:
                    ValidateFloat(mFloatValue);
                    ValidateFloat(other.mFloatValue);
                    return mFloatValue.Equals(other.mFloatValue);
                case EGValueDictionaryKeyType.String:
                    return string.Equals(mStringValue, other.mStringValue, StringComparison.Ordinal);
                case EGValueDictionaryKeyType.Enum:
                    return mEnumValue == other.mEnumValue;
                default:
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary Key类型非法[{mKeyType}]");
            }
        }

        public override bool Equals(object obj)
        {
            return obj is GDictionaryKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int valueHash;
                switch (mKeyType)
                {
                    case EGValueDictionaryKeyType.Bool:
                        valueHash = mBoolValue.GetHashCode();
                        break;
                    case EGValueDictionaryKeyType.Int:
                        valueHash = mIntValue;
                        break;
                    case EGValueDictionaryKeyType.Float:
                        ValidateFloat(mFloatValue);
                        valueHash = mFloatValue.GetHashCode();
                        break;
                    case EGValueDictionaryKeyType.String:
                        valueHash = mStringValue == null
                            ? 0
                            : StringComparer.Ordinal.GetHashCode(mStringValue);
                        break;
                    case EGValueDictionaryKeyType.Enum:
                        valueHash = mEnumValue;
                        break;
                    default:
                        throw GDictionaryTypeUtility.InvalidSignature(
                            $"GDictionary Key类型非法[{mKeyType}]");
                }
                return ((int)mKeyType * 397) ^ valueHash;
            }
        }

        public static bool operator ==(GDictionaryKey left, GDictionaryKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GDictionaryKey left, GDictionaryKey right)
        {
            return !left.Equals(right);
        }

        private void EnsureType(EGValueDictionaryKeyType expectedType)
        {
            if (mKeyType != expectedType)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    $"GDictionary Key类型不匹配, 期望[{expectedType}], 实际[{mKeyType}]");
            }
        }

        private static void ValidateFloat(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    $"GDictionary Float Key必须是有限数值, 实际[{value}]");
            }
        }
    }

    [Serializable]
    public sealed class GDictionaryValue
    {
        [SerializeField] private EGValueType mValueType;
        [SerializeField] private bool mBoolValue;
        [SerializeField] private int mIntValue;
        [SerializeField] private float mFloatValue;
        [SerializeField] private string mStringValue;
        [SerializeField] private byte mEnumValue;
        [SerializeField] private PointData mPointValue;
        [NonSerialized] private Transform mTransformValue;
        [NonSerialized] private TargetUnit mUnitValue;
        [SerializeField] private bool[] mGroupBoolValue;
        [SerializeField] private int[] mGroupIntValue;
        [SerializeField] private float[] mGroupFloatValue;
        [SerializeField] private string[] mGroupStringValue;
        [NonSerialized] private List<Transform> mGroupTransformValue;
        [SerializeField] private List<PointData> mGroupPointValue;
        [NonSerialized] private List<ActionEngine_Unit> mGroupUnitValue;

        public EGValueType ValueType => mValueType;

        public static GDictionaryValue FromBool(bool value)
        {
            return new GDictionaryValue { mValueType = EGValueType.GBool, mBoolValue = value };
        }

        public static GDictionaryValue FromInt(int value)
        {
            return new GDictionaryValue { mValueType = EGValueType.GInt, mIntValue = value };
        }

        public static GDictionaryValue FromFloat(float value)
        {
            return new GDictionaryValue { mValueType = EGValueType.GFloat, mFloatValue = value };
        }

        public static GDictionaryValue FromString(string value)
        {
            return new GDictionaryValue { mValueType = EGValueType.GString, mStringValue = value };
        }

        public static GDictionaryValue FromEnum(byte value)
        {
            return new GDictionaryValue { mValueType = EGValueType.GEnum, mEnumValue = value };
        }

        public static GDictionaryValue FromPoint(PointData value)
        {
            return new GDictionaryValue { mValueType = EGValueType.GPoint, mPointValue = value };
        }

        public static GDictionaryValue FromTransform(Transform value)
        {
            return new GDictionaryValue { mValueType = EGValueType.GTransform, mTransformValue = value };
        }

        public static GDictionaryValue FromUnit(TargetUnit value)
        {
            return new GDictionaryValue { mValueType = EGValueType.GUnit, mUnitValue = value };
        }

        public static GDictionaryValue FromGroupBool(bool[] value)
        {
            return new GDictionaryValue
            {
                mValueType = EGValueType.GGroupBool,
                mGroupBoolValue = CopyArray(value)
            };
        }

        public static GDictionaryValue FromGroupInt(int[] value)
        {
            return new GDictionaryValue
            {
                mValueType = EGValueType.GGroupInt,
                mGroupIntValue = CopyArray(value)
            };
        }

        public static GDictionaryValue FromGroupFloat(float[] value)
        {
            return new GDictionaryValue
            {
                mValueType = EGValueType.GGroupFloat,
                mGroupFloatValue = CopyArray(value)
            };
        }

        public static GDictionaryValue FromGroupString(string[] value)
        {
            return new GDictionaryValue
            {
                mValueType = EGValueType.GGroupString,
                mGroupStringValue = CopyArray(value)
            };
        }

        public static GDictionaryValue FromGroupTransform(List<Transform> value)
        {
            return new GDictionaryValue
            {
                mValueType = EGValueType.GGroupTransform,
                mGroupTransformValue = CopyList(value)
            };
        }

        public static GDictionaryValue FromGroupPoint(List<PointData> value)
        {
            return new GDictionaryValue
            {
                mValueType = EGValueType.GGroupPoint,
                mGroupPointValue = CopyList(value)
            };
        }

        public static GDictionaryValue FromGroupUnit(List<ActionEngine_Unit> value)
        {
            return new GDictionaryValue
            {
                mValueType = EGValueType.GGroupUnit,
                mGroupUnitValue = CopyList(value)
            };
        }

        public static GDictionaryValue Create<T>(T value)
        {
            Type type = typeof(T);
            if (type == typeof(bool)) return FromBool((bool)(object)value);
            if (type == typeof(int)) return FromInt((int)(object)value);
            if (type == typeof(float)) return FromFloat((float)(object)value);
            if (type == typeof(string)) return FromString((string)(object)value);
            if (type == typeof(byte)) return FromEnum((byte)(object)value);
            if (type == typeof(PointData)) return FromPoint((PointData)(object)value);
            if (type == typeof(Transform)) return FromTransform((Transform)(object)value);
            if (type == typeof(TargetUnit)) return FromUnit((TargetUnit)(object)value);
            if (type == typeof(bool[])) return FromGroupBool((bool[])(object)value);
            if (type == typeof(int[])) return FromGroupInt((int[])(object)value);
            if (type == typeof(float[])) return FromGroupFloat((float[])(object)value);
            if (type == typeof(string[])) return FromGroupString((string[])(object)value);
            if (type == typeof(List<Transform>)) return FromGroupTransform((List<Transform>)(object)value);
            if (type == typeof(List<PointData>)) return FromGroupPoint((List<PointData>)(object)value);
            if (type == typeof(List<ActionEngine_Unit>)) return FromGroupUnit((List<ActionEngine_Unit>)(object)value);

            throw GDictionaryTypeUtility.InvalidArgument(
                $"GDictionary不支持Value CLR类型[{type.FullName}]；GEnum Value必须使用Byte，GUnit Value必须使用TargetUnit");
        }

        public T Get<T>()
        {
            EGValueType expectedType = GetValueType<T>();
            Validate(expectedType);

            Type type = typeof(T);
            object value;
            switch (mValueType)
            {
                case EGValueType.GBool:
                    value = mBoolValue;
                    break;
                case EGValueType.GInt:
                    value = mIntValue;
                    break;
                case EGValueType.GFloat:
                    value = mFloatValue;
                    break;
                case EGValueType.GString:
                    value = mStringValue;
                    break;
                case EGValueType.GEnum:
                    value = mEnumValue;
                    break;
                case EGValueType.GPoint:
                    value = mPointValue;
                    break;
                case EGValueType.GTransform:
                    value = mTransformValue;
                    break;
                case EGValueType.GUnit:
                    value = mUnitValue;
                    break;
                case EGValueType.GGroupBool:
                    value = CopyArray(mGroupBoolValue);
                    break;
                case EGValueType.GGroupInt:
                    value = CopyArray(mGroupIntValue);
                    break;
                case EGValueType.GGroupFloat:
                    value = CopyArray(mGroupFloatValue);
                    break;
                case EGValueType.GGroupString:
                    value = CopyArray(mGroupStringValue);
                    break;
                case EGValueType.GGroupTransform:
                    value = CopyList(mGroupTransformValue);
                    break;
                case EGValueType.GGroupPoint:
                    value = CopyList(mGroupPointValue);
                    break;
                case EGValueType.GGroupUnit:
                    value = CopyList(mGroupUnitValue);
                    break;
                default:
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary Value类型非法[{mValueType}]");
            }
            return (T)value;
        }

        public void Validate(EGValueType expectedType)
        {
            GDictionaryTypeUtility.ValidateValueType(mValueType);
            if (mValueType != expectedType)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    $"GDictionary Value类型不匹配, 期望[{expectedType}], 实际[{mValueType}]");
            }
        }

        public bool ValueEquals(GDictionaryValue other)
        {
            if (other == null || mValueType != other.mValueType)
            {
                return false;
            }

            switch (mValueType)
            {
                case EGValueType.GBool:
                    return mBoolValue == other.mBoolValue;
                case EGValueType.GInt:
                    return mIntValue == other.mIntValue;
                case EGValueType.GFloat:
                    return mFloatValue.Equals(other.mFloatValue);
                case EGValueType.GString:
                    return string.Equals(
                        mStringValue,
                        other.mStringValue,
                        StringComparison.Ordinal);
                case EGValueType.GEnum:
                    return mEnumValue == other.mEnumValue;
                case EGValueType.GPoint:
                    return PointEquals(mPointValue, other.mPointValue);
                case EGValueType.GTransform:
                    return ReferenceEquals(mTransformValue, other.mTransformValue);
                case EGValueType.GUnit:
                    return ReferenceEquals(mUnitValue, other.mUnitValue);
                case EGValueType.GGroupBool:
                    return SequenceEquals(mGroupBoolValue, other.mGroupBoolValue);
                case EGValueType.GGroupInt:
                    return SequenceEquals(mGroupIntValue, other.mGroupIntValue);
                case EGValueType.GGroupFloat:
                    return SequenceEquals(mGroupFloatValue, other.mGroupFloatValue);
                case EGValueType.GGroupString:
                    return SequenceEquals(mGroupStringValue, other.mGroupStringValue);
                case EGValueType.GGroupTransform:
                    return ReferenceSequenceEquals(
                        mGroupTransformValue,
                        other.mGroupTransformValue);
                case EGValueType.GGroupPoint:
                    return PointSequenceEquals(
                        mGroupPointValue,
                        other.mGroupPointValue);
                case EGValueType.GGroupUnit:
                    return ReferenceSequenceEquals(
                        mGroupUnitValue,
                        other.mGroupUnitValue);
                default:
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary Value类型非法[{mValueType}]");
            }
        }

        internal GDictionaryValue CloneOwned()
        {
            switch (mValueType)
            {
                case EGValueType.GBool:
                    return FromBool(mBoolValue);
                case EGValueType.GInt:
                    return FromInt(mIntValue);
                case EGValueType.GFloat:
                    return FromFloat(mFloatValue);
                case EGValueType.GString:
                    return FromString(mStringValue);
                case EGValueType.GEnum:
                    return FromEnum(mEnumValue);
                case EGValueType.GPoint:
                    return FromPoint(mPointValue);
                case EGValueType.GTransform:
                    return FromTransform(mTransformValue);
                case EGValueType.GUnit:
                    return FromUnit(mUnitValue);
                case EGValueType.GGroupBool:
                    return FromGroupBool(mGroupBoolValue);
                case EGValueType.GGroupInt:
                    return FromGroupInt(mGroupIntValue);
                case EGValueType.GGroupFloat:
                    return FromGroupFloat(mGroupFloatValue);
                case EGValueType.GGroupString:
                    return FromGroupString(mGroupStringValue);
                case EGValueType.GGroupTransform:
                    return FromGroupTransform(mGroupTransformValue);
                case EGValueType.GGroupPoint:
                    return FromGroupPoint(mGroupPointValue);
                case EGValueType.GGroupUnit:
                    return FromGroupUnit(mGroupUnitValue);
                default:
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary Value类型非法[{mValueType}]");
            }
        }

        internal static EGValueType GetValueType<T>()
        {
            Type type = typeof(T);
            if (type == typeof(bool)) return EGValueType.GBool;
            if (type == typeof(int)) return EGValueType.GInt;
            if (type == typeof(float)) return EGValueType.GFloat;
            if (type == typeof(string)) return EGValueType.GString;
            if (type == typeof(byte)) return EGValueType.GEnum;
            if (type == typeof(PointData)) return EGValueType.GPoint;
            if (type == typeof(Transform)) return EGValueType.GTransform;
            if (type == typeof(TargetUnit)) return EGValueType.GUnit;
            if (type == typeof(bool[])) return EGValueType.GGroupBool;
            if (type == typeof(int[])) return EGValueType.GGroupInt;
            if (type == typeof(float[])) return EGValueType.GGroupFloat;
            if (type == typeof(string[])) return EGValueType.GGroupString;
            if (type == typeof(List<Transform>)) return EGValueType.GGroupTransform;
            if (type == typeof(List<PointData>)) return EGValueType.GGroupPoint;
            if (type == typeof(List<ActionEngine_Unit>)) return EGValueType.GGroupUnit;

            throw GDictionaryTypeUtility.InvalidArgument(
                $"GDictionary不支持Value CLR类型[{type.FullName}]；GEnum Value必须使用Byte，GUnit Value必须使用TargetUnit");
        }

        private static bool[] CopyArray(bool[] source)
        {
            if (source == null || source.Length == 0) return new bool[0];
            bool[] copy = new bool[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static int[] CopyArray(int[] source)
        {
            if (source == null || source.Length == 0) return new int[0];
            int[] copy = new int[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static float[] CopyArray(float[] source)
        {
            if (source == null || source.Length == 0) return new float[0];
            float[] copy = new float[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static string[] CopyArray(string[] source)
        {
            if (source == null || source.Length == 0) return new string[0];
            string[] copy = new string[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static List<Transform> CopyList(List<Transform> source)
        {
            if (source == null || source.Count == 0) return new List<Transform>();
            return new List<Transform>(source);
        }

        private static List<PointData> CopyList(List<PointData> source)
        {
            if (source == null || source.Count == 0) return new List<PointData>();
            return new List<PointData>(source);
        }

        private static List<ActionEngine_Unit> CopyList(List<ActionEngine_Unit> source)
        {
            if (source == null || source.Count == 0) return new List<ActionEngine_Unit>();
            return new List<ActionEngine_Unit>(source);
        }

        private static bool PointEquals(PointData left, PointData right)
        {
            return left.eVector_pos.x.Equals(right.eVector_pos.x) &&
                   left.eVector_pos.y.Equals(right.eVector_pos.y) &&
                   left.eVector_pos.z.Equals(right.eVector_pos.z) &&
                   left.eVector_rot.x.Equals(right.eVector_rot.x) &&
                   left.eVector_rot.y.Equals(right.eVector_rot.y) &&
                   left.eVector_rot.z.Equals(right.eVector_rot.z);
        }

        private static bool SequenceEquals(bool[] left, bool[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }

        private static bool SequenceEquals(int[] left, int[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }

        private static bool SequenceEquals(float[] left, float[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (!left[i].Equals(right[i])) return false;
            }
            return true;
        }

        private static bool SequenceEquals(string[] left, string[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static bool ReferenceSequenceEquals<T>(List<T> left, List<T> right)
            where T : class
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!ReferenceEquals(left[i], right[i])) return false;
            }
            return true;
        }

        private static bool PointSequenceEquals(
            List<PointData> left,
            List<PointData> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!PointEquals(left[i], right[i])) return false;
            }
            return true;
        }
    }

    [Serializable]
    public sealed class GDictionaryEntry
    {
        [SerializeField] private GDictionaryKey mKey;
        [SerializeField] private GDictionaryValue mValue;

        public GDictionaryKey Key => mKey;
        public GDictionaryValue Value => mValue;

        public GDictionaryEntry()
        {
        }

        public GDictionaryEntry(GDictionaryKey key, GDictionaryValue value)
        {
            mKey = key;
            mValue = value;
        }
    }

    [Serializable]
    public sealed class GDictionaryDefinition
    {
        [SerializeField] private GDictionarySignature mSignature;
        [SerializeField] private GDictionaryEntry[] mEntries;

        public GDictionarySignature Signature => mSignature;
        public GDictionaryEntry[] Entries => mEntries;

        public GDictionaryDefinition()
        {
            mEntries = new GDictionaryEntry[0];
        }

        public GDictionaryDefinition(GDictionarySignature signature, GDictionaryEntry[] entries)
        {
            mSignature = signature;
            mEntries = entries ?? new GDictionaryEntry[0];
        }

        public void Validate()
        {
            mSignature.Validate();
            if (mEntries == null)
            {
                throw GDictionaryTypeUtility.InvalidSignature(
                    "GDictionary默认Entries数组为空引用");
            }

            HashSet<GDictionaryKey> keys = new HashSet<GDictionaryKey>();
            for (int i = 0; i < mEntries.Length; i++)
            {
                GDictionaryEntry entry = mEntries[i];
                if (entry == null || entry.Value == null)
                {
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary默认Entry为空, Entry[{i}]");
                }

                entry.Key.Validate(mSignature.KeyType);
                entry.Value.Validate(mSignature.ValueType);
                if (!keys.Add(entry.Key))
                {
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary默认Key重复, Entry[{i}]");
                }
            }
        }

        public GDictionaryDefinition Clone()
        {
            Validate();
            GDictionaryEntry[] entries = new GDictionaryEntry[mEntries.Length];
            for (int i = 0; i < mEntries.Length; i++)
            {
                GDictionaryEntry entry = mEntries[i];
                entries[i] = new GDictionaryEntry(entry.Key, entry.Value.CloneOwned());
            }
            return new GDictionaryDefinition(mSignature, entries);
        }
    }

    internal sealed class GDictionaryStorage
    {
        private readonly GDictionarySignature mSignature;
        private readonly Dictionary<GDictionaryKey, GDictionaryValue> mValues;

        internal GDictionarySignature Signature => mSignature;
        internal int Count => mValues.Count;

        internal GDictionaryStorage(GDictionaryDefinition definition, ushort group, ushort id)
        {
            if (definition == null)
            {
                throw GDictionaryTypeUtility.InvalidSignature(
                    $"GDictionary定义为空, Group[{group}], ID[{id}]");
            }

            mSignature = definition.Signature;
            mSignature.Validate();

            GDictionaryEntry[] entries = definition.Entries;
            int capacity = entries == null ? 0 : entries.Length;
            mValues = new Dictionary<GDictionaryKey, GDictionaryValue>(capacity);
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                GDictionaryEntry entry = entries[i];
                if (entry == null || entry.Value == null)
                {
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary初始Entry为空, Group[{group}], ID[{id}], Entry[{i}]");
                }

                entry.Key.Validate(mSignature.KeyType);
                entry.Value.Validate(mSignature.ValueType);
                if (!mValues.TryAdd(entry.Key, entry.Value.CloneOwned()))
                {
                    throw GDictionaryTypeUtility.InvalidSignature(
                        $"GDictionary初始Key重复, Group[{group}], ID[{id}], Entry[{i}]");
                }
            }
        }

        internal void ValidateSignature(GDictionarySignature signature)
        {
            signature.Validate();
            if (signature != mSignature)
            {
                throw GDictionaryTypeUtility.InvalidSignature(
                    $"GDictionary签名不匹配, 定义[{mSignature}], 请求[{signature}]");
            }
        }

        internal bool TryGet(GDictionaryKey key, out GDictionaryValue value)
        {
            key.Validate(mSignature.KeyType);
            if (mValues.TryGetValue(key, out GDictionaryValue stored))
            {
                value = stored.CloneOwned();
                return true;
            }

            value = null;
            return false;
        }

        internal void Set(GDictionaryKey key, GDictionaryValue value)
        {
            if (value == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument("GDictionary禁止写入null Value容器");
            }

            key.Validate(mSignature.KeyType);
            value.Validate(mSignature.ValueType);
            GDictionaryValue ownedValue = value.CloneOwned();
            if (!mValues.TryAdd(key, ownedValue))
            {
                mValues[key] = ownedValue;
            }
        }

        internal bool Contains(GDictionaryKey key)
        {
            key.Validate(mSignature.KeyType);
            return mValues.ContainsKey(key);
        }

        internal bool ContainsValue(GDictionaryValue value)
        {
            if (value == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary ContainsValue输入禁止为null Value容器");
            }

            value.Validate(mSignature.ValueType);
            foreach (KeyValuePair<GDictionaryKey, GDictionaryValue> pair in mValues)
            {
                if (pair.Value.ValueEquals(value))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool Remove(GDictionaryKey key)
        {
            key.Validate(mSignature.KeyType);
            return mValues.Remove(key);
        }

        internal void Clear()
        {
            mValues.Clear();
        }
    }

    internal static class GDictionaryTypeUtility
    {
        internal static void ValidateSignature(GDictionarySignature signature)
        {
            ValidateKeyType(signature.KeyType);
            ValidateValueType(signature.ValueType);
        }

        internal static void ValidateKeyType(EGValueDictionaryKeyType keyType)
        {
            switch (keyType)
            {
                case EGValueDictionaryKeyType.Bool:
                case EGValueDictionaryKeyType.Int:
                case EGValueDictionaryKeyType.Float:
                case EGValueDictionaryKeyType.String:
                case EGValueDictionaryKeyType.Enum:
                    return;
                default:
                    throw InvalidSignature($"GDictionary不支持Key类型[{keyType}]");
            }
        }

        internal static void ValidateValueType(EGValueType valueType)
        {
            switch (valueType)
            {
                case EGValueType.GBool:
                case EGValueType.GInt:
                case EGValueType.GFloat:
                case EGValueType.GString:
                case EGValueType.GEnum:
                case EGValueType.GPoint:
                case EGValueType.GTransform:
                case EGValueType.GUnit:
                case EGValueType.GGroupBool:
                case EGValueType.GGroupInt:
                case EGValueType.GGroupFloat:
                case EGValueType.GGroupString:
                case EGValueType.GGroupTransform:
                case EGValueType.GGroupPoint:
                case EGValueType.GGroupUnit:
                    return;
                default:
                    throw InvalidSignature(
                        $"GDictionary不支持Value类型[{valueType}], 禁止嵌套字典");
            }
        }

        internal static InvalidOperationException InvalidSignature(string message)
        {
            EngineDebug.LogError(message);
            return new InvalidOperationException(message);
        }

        internal static ArgumentException InvalidArgument(string message)
        {
            EngineDebug.LogError(message);
            return new ArgumentException(message);
        }
    }

    [Serializable]
    public sealed class GDictionary : GValue
    {
        [SerializeField] private GDictionarySignature mSignature;

        public GDictionarySignature Signature => mSignature;

        public GDictionary()
        {
            mSignature = new GDictionarySignature(
                EGValueDictionaryKeyType.Bool,
                EGValueType.GBool);
            mType = true;
        }

        public GDictionary(GDictionarySignature signature)
        {
            signature.Validate();
            mSignature = signature;
            mType = true;
        }

#if UNITY_EDITOR
        public void SetSignatureEditor(GDictionarySignature signature)
        {
            signature.Validate();
            mSignature = signature;
        }
#endif

        public bool TryGet<TKey, TValue>(ActionStatePart part, TKey key, out TValue value)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.TryGetDictionaryValue(mValueGroupIndex, mValueIndex, key, out value);
        }

        public bool TryGet(
            ActionStatePart part,
            GDictionaryKey key,
            out GDictionaryValue value)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.TryGetDictionaryValue(mValueGroupIndex, mValueIndex, key, out value);
        }

        public void Set<TKey, TValue>(ActionStatePart part, TKey key, TValue value)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            pool.SetDictionaryValue(mValueGroupIndex, mValueIndex, key, value);
        }

        public void Set(ActionStatePart part, GDictionaryKey key, GDictionaryValue value)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            pool.SetDictionaryValue(mValueGroupIndex, mValueIndex, key, value);
        }

        public bool Contains<TKey>(ActionStatePart part, TKey key)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.DictionaryContainsKey(mValueGroupIndex, mValueIndex, key);
        }

        public bool Contains(ActionStatePart part, GDictionaryKey key)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.DictionaryContainsKey(mValueGroupIndex, mValueIndex, key);
        }

        public bool ContainsValue<TValue>(ActionStatePart part, TValue value)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.DictionaryContainsValue(mValueGroupIndex, mValueIndex, value);
        }

        public bool ContainsValue(ActionStatePart part, GDictionaryValue value)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.DictionaryContainsValue(mValueGroupIndex, mValueIndex, value);
        }

        public bool Remove<TKey>(ActionStatePart part, TKey key)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.RemoveDictionaryValue(mValueGroupIndex, mValueIndex, key);
        }

        public bool Remove(ActionStatePart part, GDictionaryKey key)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.RemoveDictionaryValue(mValueGroupIndex, mValueIndex, key);
        }

        public void Clear(ActionStatePart part)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            pool.ClearDictionary(mValueGroupIndex, mValueIndex);
        }

        public int Count(ActionStatePart part)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            return pool.GetDictionaryCount(mValueGroupIndex, mValueIndex);
        }

        public void Reset(ActionStatePart part)
        {
            GValuePool pool = GetPool(part);
            pool.ValidateDictionarySignature(mValueGroupIndex, mValueIndex, mSignature);
            pool.ResetDictionary(mValueGroupIndex, mValueIndex);
        }

        public (ushort, ushort) GetKey()
        {
            return (mValueGroupIndex, mValueIndex);
        }

        public override GValue Clone()
        {
#if UNITY_EDITOR
            GDictionary clone = new GDictionary(mSignature);
            clone.mType = mType;
            clone.mValueIndex = mValueIndex;
            clone.mValueGroupIndex = mValueGroupIndex;
            return clone;
#endif
            return this;
        }

        private static GValuePool GetPool(ActionStatePart part)
        {
            if (part == null || part.ActionStateMachine == null)
            {
                throw GDictionaryTypeUtility.InvalidArgument(
                    "GDictionary访问需要有效的ActionStatePart和ActionStateMachine");
            }
            return part.ActionStateMachine.GValuePool;
        }
    }
}
