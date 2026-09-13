using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [Serializable]
    public sealed class ServerGameObjectSnapshot
    {
        [SerializeField] private int m_Layer;
        [SerializeField] private string m_Tag = "Untagged";

        public int Layer
        {
            get => m_Layer;
            set => m_Layer = value;
        }

        public string Tag
        {
            get => m_Tag;
            set => m_Tag = value;
        }
    }

    [Serializable]
    public sealed class ServerComponentSnapshot
    {
        [SerializeField] private string m_ComponentTypeName = string.Empty;
        [SerializeField] private int m_ComponentIndex;
        [SerializeField] private List<ServerComponentFieldSnapshot> m_Fields = new List<ServerComponentFieldSnapshot>();

        public string ComponentTypeName
        {
            get => m_ComponentTypeName;
            set => m_ComponentTypeName = value;
        }

        public int ComponentIndex
        {
            get => m_ComponentIndex;
            set => m_ComponentIndex = value;
        }

        public List<ServerComponentFieldSnapshot> Fields
        {
            get => m_Fields;
            set => m_Fields = value;
        }
    }

    [Serializable]
    public sealed class ServerComponentFieldSnapshot
    {
        [SerializeField] private string m_DeclaringTypeName = string.Empty;
        [SerializeField] private string m_FieldName = string.Empty;
        [OptionalField]
        [SerializeField] private bool m_IsProperty;
        private ServerComponentValueSnapshot m_Value = new ServerComponentValueSnapshot();

        public string DeclaringTypeName
        {
            get => m_DeclaringTypeName;
            set => m_DeclaringTypeName = value;
        }

        public string FieldName
        {
            get => m_FieldName;
            set => m_FieldName = value;
        }

        public bool IsProperty
        {
            get => m_IsProperty;
            set => m_IsProperty = value;
        }

        public ServerComponentValueSnapshot Value
        {
            get => m_Value;
            set => m_Value = value;
        }
    }

    [Serializable]
    public sealed class ServerComponentValueSnapshot
    {
        [SerializeField] private EServerComponentValueKind m_Kind;
        [SerializeField] private string m_ScalarValue = string.Empty;
        private List<ServerComponentValueSnapshot> m_Items = new List<ServerComponentValueSnapshot>();
        private List<ServerComponentFieldSnapshot> m_Fields = new List<ServerComponentFieldSnapshot>();

        public EServerComponentValueKind Kind
        {
            get => m_Kind;
            set => m_Kind = value;
        }

        public string ScalarValue
        {
            get => m_ScalarValue;
            set => m_ScalarValue = value;
        }

        public List<ServerComponentValueSnapshot> Items
        {
            get => m_Items;
            set => m_Items = value;
        }

        public List<ServerComponentFieldSnapshot> Fields
        {
            get => m_Fields;
            set => m_Fields = value;
        }
    }


    [Serializable]
    public sealed class ServerComponentFlatSnapshot
    {
        [SerializeField] private string m_ComponentTypeName = string.Empty;
        [SerializeField] private int m_ComponentIndex;
        [SerializeField] private int m_FieldStart;
        [SerializeField] private int m_FieldCount;
        [SerializeField] private List<ServerComponentFlatField> m_Fields = new List<ServerComponentFlatField>();
        [SerializeField] private List<ServerComponentFlatValue> m_Values = new List<ServerComponentFlatValue>();

        public string ComponentTypeName { get => m_ComponentTypeName; set => m_ComponentTypeName = value; }
        public int ComponentIndex { get => m_ComponentIndex; set => m_ComponentIndex = value; }
        public int FieldStart { get => m_FieldStart; set => m_FieldStart = value; }
        public int FieldCount { get => m_FieldCount; set => m_FieldCount = value; }
        public List<ServerComponentFlatField> Fields { get => m_Fields; set => m_Fields = value; }
        public List<ServerComponentFlatValue> Values { get => m_Values; set => m_Values = value; }
    }

    [Serializable]
    public sealed class ServerComponentFlatField
    {
        [SerializeField] private string m_DeclaringTypeName = string.Empty;
        [SerializeField] private string m_FieldName = string.Empty;
        [SerializeField] private bool m_IsProperty;
        [SerializeField] private int m_ValueIndex = -1;

        public string DeclaringTypeName { get => m_DeclaringTypeName; set => m_DeclaringTypeName = value; }
        public string FieldName { get => m_FieldName; set => m_FieldName = value; }
        public bool IsProperty { get => m_IsProperty; set => m_IsProperty = value; }
        public int ValueIndex { get => m_ValueIndex; set => m_ValueIndex = value; }
    }

    [Serializable]
    public sealed class ServerComponentFlatValue
    {
        [SerializeField] private EServerComponentValueKind m_Kind;
        [SerializeField] private string m_ScalarValue = string.Empty;
        [SerializeField] private int m_ItemStart;
        [SerializeField] private int m_ItemCount;
        [SerializeField] private int m_FieldStart;
        [SerializeField] private int m_FieldCount;

        public EServerComponentValueKind Kind { get => m_Kind; set => m_Kind = value; }
        public string ScalarValue { get => m_ScalarValue; set => m_ScalarValue = value; }
        public int ItemStart { get => m_ItemStart; set => m_ItemStart = value; }
        public int ItemCount { get => m_ItemCount; set => m_ItemCount = value; }
        public int FieldStart { get => m_FieldStart; set => m_FieldStart = value; }
        public int FieldCount { get => m_FieldCount; set => m_FieldCount = value; }
    }

    public static class ServerComponentSnapshotCodec
    {
        private const int MaxDepth = 64;

        public static List<ServerComponentFlatSnapshot> Encode(List<ServerComponentSnapshot> snapshots)
        {
            List<ServerComponentFlatSnapshot> result = new List<ServerComponentFlatSnapshot>();
            if (snapshots == null) return result;
            for (int i = 0; i < snapshots.Count; i++)
            {
                ServerComponentSnapshot source = snapshots[i];
                if (source == null) continue;
                ServerComponentFlatSnapshot flat = new ServerComponentFlatSnapshot
                {
                    ComponentTypeName = source.ComponentTypeName,
                    ComponentIndex = source.ComponentIndex,
                };
                flat.FieldStart = ReserveFields(source.Fields, flat, 0);
                flat.FieldCount = source.Fields == null ? 0 : source.Fields.Count;
                result.Add(flat);
            }
            return result;
        }

        public static List<ServerComponentSnapshot> Decode(List<ServerComponentFlatSnapshot> snapshots)
        {
            List<ServerComponentSnapshot> result = new List<ServerComponentSnapshot>();
            if (snapshots == null) return result;
            for (int i = 0; i < snapshots.Count; i++)
            {
                ServerComponentFlatSnapshot flat = snapshots[i];
                if (flat == null) continue;
                result.Add(new ServerComponentSnapshot
                {
                    ComponentTypeName = flat.ComponentTypeName,
                    ComponentIndex = flat.ComponentIndex,
                    Fields = DecodeFields(flat, flat.FieldStart, flat.FieldCount, 0),
                });
            }
            return result;
        }

        private static int ReserveFields(
            List<ServerComponentFieldSnapshot> source,
            ServerComponentFlatSnapshot flat,
            int depth)
        {
            int start = flat.Fields.Count;
            if (source == null || depth > MaxDepth) return start;
            for (int i = 0; i < source.Count; i++) flat.Fields.Add(new ServerComponentFlatField());
            for (int i = 0; i < source.Count; i++)
            {
                ServerComponentFieldSnapshot field = source[i];
                if (field == null) continue;
                ServerComponentFlatField encoded = flat.Fields[start + i];
                encoded.DeclaringTypeName = field.DeclaringTypeName;
                encoded.FieldName = field.FieldName;
                encoded.IsProperty = field.IsProperty;
                encoded.ValueIndex = AppendValue(field.Value, flat, depth + 1);
            }
            return start;
        }

        private static int AppendValue(
            ServerComponentValueSnapshot source,
            ServerComponentFlatSnapshot flat,
            int depth)
        {
            if (source == null || depth > MaxDepth) return -1;
            int index = flat.Values.Count;
            flat.Values.Add(new ServerComponentFlatValue());
            FillValue(index, source, flat, depth);
            return index;
        }

        private static void FillValue(
            int index,
            ServerComponentValueSnapshot source,
            ServerComponentFlatSnapshot flat,
            int depth)
        {
            ServerComponentFlatValue encoded = flat.Values[index];
            encoded.Kind = source.Kind;
            encoded.ScalarValue = source.ScalarValue;
            if (source.Items != null && depth < MaxDepth)
            {
                encoded.ItemStart = flat.Values.Count;
                encoded.ItemCount = source.Items.Count;
                for (int i = 0; i < source.Items.Count; i++) flat.Values.Add(new ServerComponentFlatValue());
                for (int i = 0; i < source.Items.Count; i++)
                {
                    ServerComponentValueSnapshot item = source.Items[i];
                    if (item != null) FillValue(encoded.ItemStart + i, item, flat, depth + 1);
                }
            }
            encoded.FieldStart = ReserveFields(source.Fields, flat, depth + 1);
            encoded.FieldCount = source.Fields == null || depth >= MaxDepth ? 0 : source.Fields.Count;
        }

        private static List<ServerComponentFieldSnapshot> DecodeFields(
            ServerComponentFlatSnapshot flat, int start, int count, int depth)
        {
            List<ServerComponentFieldSnapshot> result = new List<ServerComponentFieldSnapshot>();
            if (depth > MaxDepth || flat.Fields == null) return result;
            int end = Math.Min(start + count, flat.Fields.Count);
            for (int i = Math.Max(0, start); i < end; i++)
            {
                ServerComponentFlatField field = flat.Fields[i];
                if (field == null) continue;
                result.Add(new ServerComponentFieldSnapshot
                {
                    DeclaringTypeName = field.DeclaringTypeName,
                    FieldName = field.FieldName,
                    IsProperty = field.IsProperty,
                    Value = DecodeValue(flat, field.ValueIndex, depth + 1),
                });
            }
            return result;
        }

        private static ServerComponentValueSnapshot DecodeValue(
            ServerComponentFlatSnapshot flat, int index, int depth)
        {
            if (depth > MaxDepth || flat.Values == null || index < 0 || index >= flat.Values.Count)
                return null;
            ServerComponentFlatValue value = flat.Values[index];
            if (value == null) return null;
            ServerComponentValueSnapshot result = new ServerComponentValueSnapshot
            {
                Kind = value.Kind,
                ScalarValue = value.ScalarValue,
                Fields = DecodeFields(flat, value.FieldStart, value.FieldCount, depth),
                Items = new List<ServerComponentValueSnapshot>(),
            };
            int end = Math.Min(value.ItemStart + value.ItemCount, flat.Values.Count);
            for (int i = Math.Max(0, value.ItemStart); i < end; i++)
                result.Items.Add(DecodeValue(flat, i, depth + 1));
            return result;
        }
    }

    public enum EServerComponentValueKind
    {
        Null,
        Scalar,
        Enum,
        Array,
        List,
        Object,
    }

    /// <summary>
    /// 配置恢复完成回调。依赖配置值的初始化应放在这里，而不是 Awake。
    /// </summary>
    public interface IServerComponentSnapshotReceiver
    {
        void OnServerComponentSnapshotApplied();
    }
}
