using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public static class ServerComponentSnapshotUtility
    {
        private const int MaxSerializationDepth = 16;
        private const BindingFlags DeclaredFieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const BindingFlags DeclaredPropertyFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        public static bool CanCapture(Component component, out string reason)
        {
            if (component is null)
            {
                reason = "组件为空";
                return false;
            }

            return CanCaptureType(component.GetType(), out reason);
        }

        public static bool CanCaptureType(Type componentType, out string reason)
        {
            if (componentType is null || !typeof(Component).IsAssignableFrom(componentType))
            {
                reason = "类型不是 Unity Component";
                return false;
            }

            if (typeof(Transform).IsAssignableFrom(componentType) ||
                typeof(TargetUnit).IsAssignableFrom(componentType))
            {
                reason = "Transform 与 ActionEngine 核心单位组件由创建流程负责";
                return false;
            }

            if (componentType.IsAbstract || componentType.ContainsGenericParameters)
            {
                reason = "抽象或开放泛型组件无法动态添加";
                return false;
            }

            foreach (FieldInfo field in GetSerializableFields(componentType, typeof(Component)))
            {
                if (IsSupportedValueType(field.FieldType))
                {
                    reason = string.Empty;
                    return true;
                }
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(componentType))
            {
                foreach (PropertyInfo property in GetSerializableProperties(componentType, typeof(Component)))
                {
                    if (IsSupportedValueType(property.PropertyType))
                    {
                        reason = string.Empty;
                        return true;
                    }
                }
            }

            reason = "没有可保存的值类型字段";
            return false;
        }

        public static bool TryCapture(
            Component component,
            int componentIndex,
            out ServerComponentSnapshot snapshot,
            out string reason)
        {
            snapshot = null;
            if (!CanCapture(component, out reason))
            {
                return false;
            }

            Type componentType = component.GetType();
            ServerComponentSnapshot capturedSnapshot = new ServerComponentSnapshot
            {
                ComponentTypeName = componentType.AssemblyQualifiedName,
                ComponentIndex = componentIndex,
                Fields = new List<ServerComponentFieldSnapshot>(),
            };

            HashSet<object> visitedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (FieldInfo field in GetSerializableFields(componentType, typeof(Component)))
            {
                if (!IsSupportedValueType(field.FieldType))
                {
                    continue;
                }

                try
                {
                    object fieldValue = field.GetValue(component);
                    if (!TryCaptureValue(fieldValue, field.FieldType, visitedObjects, 0, out ServerComponentValueSnapshot value))
                    {
                        continue;
                    }

                    capturedSnapshot.Fields.Add(CreateFieldSnapshot(field, value));
                }
                catch (Exception exception)
                {
                    EngineDebug.LogWarning(
                        $"[ServerComponentSnapshotUtility] 跳过字段 type=[{componentType.FullName}] field=[{field.Name}] reason=[{exception.Message}]");
                }
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(componentType))
            {
                foreach (PropertyInfo property in GetSerializableProperties(componentType, typeof(Component)))
                {
                    if (!IsSupportedValueType(property.PropertyType))
                    {
                        continue;
                    }

                    try
                    {
                        object propertyValue = property.GetValue(component);
                        if (!TryCaptureValue(propertyValue, property.PropertyType, visitedObjects, 0,
                                out ServerComponentValueSnapshot value))
                        {
                            continue;
                        }

                        capturedSnapshot.Fields.Add(CreatePropertySnapshot(property, value));
                    }
                    catch (Exception exception)
                    {
                        EngineDebug.LogWarning(
                            $"[ServerComponentSnapshotUtility] 跳过属性 type=[{componentType.FullName}] property=[{property.Name}] reason=[{exception.Message}]");
                    }
                }
            }

            if (capturedSnapshot.Fields.Count < 1)
            {
                reason = "组件字段均不受支持或读取失败";
                return false;
            }

            snapshot = capturedSnapshot;
            reason = string.Empty;
            return true;
        }

        public static void ApplySnapshots(GameObject target, List<ServerComponentSnapshot> snapshots)
        {
            if (target is null || snapshots is null || snapshots.Count < 1)
            {
                return;
            }

            List<IServerComponentSnapshotReceiver> receivers = new List<IServerComponentSnapshotReceiver>();
            for (int i = 0; i < snapshots.Count; i++)
            {
                ServerComponentSnapshot snapshot = snapshots[i];
                if (!TryApplySnapshot(target, snapshot, out Component component))
                {
                    continue;
                }

                if (component is IServerComponentSnapshotReceiver receiver)
                {
                    receivers.Add(receiver);
                }
            }

            for (int i = 0; i < receivers.Count; i++)
            {
                try
                {
                    receivers[i].OnServerComponentSnapshotApplied();
                }
                catch (Exception exception)
                {
                    EngineDebug.LogError(
                        $"[ServerComponentSnapshotUtility] 组件恢复回调失败 type=[{receivers[i].GetType().FullName}] reason=[{exception.Message}]");
                }
            }
        }

        public static void ApplyGameObjectSnapshot(
            GameObject target,
            ServerGameObjectSnapshot snapshot)
        {
            if (target is null || snapshot is null)
            {
                return;
            }

            target.layer = snapshot.Layer;
            if (string.IsNullOrEmpty(snapshot.Tag))
            {
                return;
            }

            try
            {
                target.tag = snapshot.Tag;
            }
            catch (UnityException exception)
            {
                EngineDebug.LogError(
                    $"[ServerComponentSnapshotUtility] 恢复 Tag 失败 object=[{target.name}] tag=[{snapshot.Tag}] reason=[{exception.Message}]");
            }
        }

        private static bool TryApplySnapshot(
            GameObject target,
            ServerComponentSnapshot snapshot,
            out Component component)
        {
            component = null;
            if (snapshot is null || string.IsNullOrEmpty(snapshot.ComponentTypeName))
            {
                EngineDebug.LogWarning("[ServerComponentSnapshotUtility] 跳过无组件类型的服务端快照");
                return false;
            }

            Type componentType = ResolveType(snapshot.ComponentTypeName);
            if (componentType is null || !typeof(Component).IsAssignableFrom(componentType) ||
                componentType.IsAbstract || componentType.ContainsGenericParameters)
            {
                EngineDebug.LogWarning(
                    $"[ServerComponentSnapshotUtility] 无法恢复组件 type=[{snapshot.ComponentTypeName}]");
                return false;
            }

            try
            {
                Component[] existingComponents = target.GetComponents(componentType);
                while (existingComponents.Length <= snapshot.ComponentIndex)
                {
                    target.AddComponent(componentType);
                    existingComponents = target.GetComponents(componentType);
                }

                component = existingComponents[snapshot.ComponentIndex];
                ApplyFields(component, snapshot.Fields);
                return true;
            }
            catch (Exception exception)
            {
                EngineDebug.LogError(
                    $"[ServerComponentSnapshotUtility] 添加或恢复组件失败 type=[{componentType.FullName}] index=[{snapshot.ComponentIndex}] reason=[{exception.Message}]");
                return false;
            }
        }

        private static void ApplyFields(Component component, List<ServerComponentFieldSnapshot> fields)
        {
            if (fields is null)
            {
                return;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                ServerComponentFieldSnapshot fieldSnapshot = fields[i];
                if (fieldSnapshot is null)
                {
                    continue;
                }

                if (fieldSnapshot.IsProperty)
                {
                    ApplyProperty(component, fieldSnapshot);
                    continue;
                }

                FieldInfo field = ResolveField(component.GetType(), fieldSnapshot);
                if (field is null || !IsUnitySerializedField(field) || !IsSupportedValueType(field.FieldType))
                {
                    EngineDebug.LogWarning(
                        $"[ServerComponentSnapshotUtility] 找不到可恢复字段 component=[{component.GetType().FullName}] field=[{fieldSnapshot.FieldName}]");
                    continue;
                }

                try
                {
                    object value = RestoreValue(fieldSnapshot.Value, field.FieldType, 0);
                    field.SetValue(component, value);
                }
                catch (Exception exception)
                {
                    EngineDebug.LogWarning(
                        $"[ServerComponentSnapshotUtility] 恢复字段失败 component=[{component.GetType().FullName}] field=[{field.Name}] reason=[{exception.Message}]");
                }
            }
        }

        private static void ApplyProperty(Component component, ServerComponentFieldSnapshot snapshot)
        {
            PropertyInfo property = ResolveProperty(component.GetType(), snapshot);
            if (property is null || !property.CanWrite || !IsSupportedValueType(property.PropertyType))
            {
                EngineDebug.LogWarning(
                    $"[ServerComponentSnapshotUtility] 找不到可恢复属性 component=[{component.GetType().FullName}] property=[{snapshot.FieldName}]");
                return;
            }

            try
            {
                object value = RestoreValue(snapshot.Value, property.PropertyType, 0);
                property.SetValue(component, value);
            }
            catch (Exception exception)
            {
                EngineDebug.LogWarning(
                    $"[ServerComponentSnapshotUtility] 恢复属性失败 component=[{component.GetType().FullName}] property=[{property.Name}] reason=[{exception.Message}]");
            }
        }

        private static ServerComponentFieldSnapshot CreateFieldSnapshot(
            FieldInfo field,
            ServerComponentValueSnapshot value)
        {
            return new ServerComponentFieldSnapshot
            {
                DeclaringTypeName = field.DeclaringType?.AssemblyQualifiedName ?? string.Empty,
                FieldName = field.Name,
                Value = value,
            };
        }

        private static ServerComponentFieldSnapshot CreatePropertySnapshot(
            PropertyInfo property,
            ServerComponentValueSnapshot value)
        {
            return new ServerComponentFieldSnapshot
            {
                DeclaringTypeName = property.DeclaringType?.AssemblyQualifiedName ?? string.Empty,
                FieldName = property.Name,
                IsProperty = true,
                Value = value,
            };
        }

        private static bool TryCaptureValue(
            object value,
            Type valueType,
            HashSet<object> visitedObjects,
            int depth,
            out ServerComponentValueSnapshot snapshot)
        {
            snapshot = null;
            if (depth > MaxSerializationDepth || !IsSupportedValueType(valueType))
            {
                return false;
            }

            if (value is null)
            {
                snapshot = new ServerComponentValueSnapshot { Kind = EServerComponentValueKind.Null };
                return true;
            }

            if (IsScalarType(valueType))
            {
                snapshot = new ServerComponentValueSnapshot
                {
                    Kind = EServerComponentValueKind.Scalar,
                    ScalarValue = ScalarToString(value, valueType),
                };
                return true;
            }

            if (valueType.IsEnum)
            {
                snapshot = new ServerComponentValueSnapshot
                {
                    Kind = EServerComponentValueKind.Enum,
                    ScalarValue = value.ToString(),
                };
                return true;
            }

            if (!valueType.IsValueType && !visitedObjects.Add(value))
            {
                return false;
            }

            try
            {
                if (valueType.IsArray)
                {
                    Array array = (Array)value;
                    Type elementType = valueType.GetElementType();
                    snapshot = new ServerComponentValueSnapshot
                    {
                        Kind = EServerComponentValueKind.Array,
                        Items = CaptureItems(array, elementType, visitedObjects, depth + 1),
                    };
                    return snapshot.Items is not null;
                }

                if (IsListType(valueType))
                {
                    IList list = (IList)value;
                    Type elementType = valueType.GetGenericArguments()[0];
                    snapshot = new ServerComponentValueSnapshot
                    {
                        Kind = EServerComponentValueKind.List,
                        Items = CaptureItems(list, elementType, visitedObjects, depth + 1),
                    };
                    return snapshot.Items is not null;
                }

                List<ServerComponentFieldSnapshot> fields = new List<ServerComponentFieldSnapshot>();
                foreach (FieldInfo field in GetSerializableFields(valueType, typeof(object)))
                {
                    if (!IsSupportedValueType(field.FieldType))
                    {
                        continue;
                    }

                    object fieldValue = field.GetValue(value);
                    if (TryCaptureValue(fieldValue, field.FieldType, visitedObjects, depth + 1,
                            out ServerComponentValueSnapshot childValue))
                    {
                        fields.Add(CreateFieldSnapshot(field, childValue));
                    }
                }

                snapshot = new ServerComponentValueSnapshot
                {
                    Kind = EServerComponentValueKind.Object,
                    Fields = fields,
                };
                return true;
            }
            finally
            {
                if (!valueType.IsValueType)
                {
                    visitedObjects.Remove(value);
                }
            }
        }

        private static List<ServerComponentValueSnapshot> CaptureItems(
            IEnumerable source,
            Type elementType,
            HashSet<object> visitedObjects,
            int depth)
        {
            List<ServerComponentValueSnapshot> items = new List<ServerComponentValueSnapshot>();
            foreach (object item in source)
            {
                if (!TryCaptureValue(item, elementType, visitedObjects, depth, out ServerComponentValueSnapshot itemValue))
                {
                    return null;
                }

                items.Add(itemValue);
            }

            return items;
        }

        private static object RestoreValue(ServerComponentValueSnapshot snapshot, Type valueType, int depth)
        {
            if (snapshot is null || depth > MaxSerializationDepth)
            {
                return GetDefaultValue(valueType);
            }

            switch (snapshot.Kind)
            {
                case EServerComponentValueKind.Null:
                    return GetDefaultValue(valueType);
                case EServerComponentValueKind.Scalar:
                    return StringToScalar(snapshot.ScalarValue, valueType);
                case EServerComponentValueKind.Enum:
                    return Enum.Parse(valueType, snapshot.ScalarValue);
                case EServerComponentValueKind.Array:
                    return RestoreArray(snapshot.Items, valueType, depth + 1);
                case EServerComponentValueKind.List:
                    return RestoreList(snapshot.Items, valueType, depth + 1);
                case EServerComponentValueKind.Object:
                    return RestoreObject(snapshot.Fields, valueType, depth + 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(snapshot.Kind), snapshot.Kind, "未知服务端组件值类型");
            }
        }

        private static Array RestoreArray(
            List<ServerComponentValueSnapshot> items,
            Type arrayType,
            int depth)
        {
            Type elementType = arrayType.GetElementType();
            int count = items?.Count ?? 0;
            Array array = Array.CreateInstance(elementType, count);
            for (int i = 0; i < count; i++)
            {
                array.SetValue(RestoreValue(items[i], elementType, depth), i);
            }

            return array;
        }

        private static object RestoreList(
            List<ServerComponentValueSnapshot> items,
            Type listType,
            int depth)
        {
            Type elementType = listType.GetGenericArguments()[0];
            Type concreteType = listType.IsAbstract || listType.IsInterface
                ? typeof(List<>).MakeGenericType(elementType)
                : listType;
            IList list = (IList)Activator.CreateInstance(concreteType, true);
            int count = items?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                list.Add(RestoreValue(items[i], elementType, depth));
            }

            return list;
        }

        private static object RestoreObject(
            List<ServerComponentFieldSnapshot> fields,
            Type objectType,
            int depth)
        {
            object instance;
            try
            {
                instance = Activator.CreateInstance(objectType, true);
            }
            catch
            {
                instance = FormatterServices.GetUninitializedObject(objectType);
            }

            if (fields is null)
            {
                return instance;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                ServerComponentFieldSnapshot fieldSnapshot = fields[i];
                FieldInfo field = ResolveField(objectType, fieldSnapshot);
                if (field is null || !IsUnitySerializedField(field) || !IsSupportedValueType(field.FieldType))
                {
                    continue;
                }

                object value = RestoreValue(fieldSnapshot.Value, field.FieldType, depth);
                field.SetValue(instance, value);
            }

            return instance;
        }

        private static FieldInfo ResolveField(Type ownerType, ServerComponentFieldSnapshot snapshot)
        {
            Type declaringType = ResolveType(snapshot.DeclaringTypeName);
            if (declaringType is not null && declaringType.IsAssignableFrom(ownerType))
            {
                return declaringType.GetField(snapshot.FieldName, DeclaredFieldFlags);
            }

            Type currentType = ownerType;
            while (currentType is not null && currentType != typeof(object))
            {
                FieldInfo field = currentType.GetField(snapshot.FieldName, DeclaredFieldFlags);
                if (field is not null)
                {
                    return field;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        private static PropertyInfo ResolveProperty(Type ownerType, ServerComponentFieldSnapshot snapshot)
        {
            Type declaringType = ResolveType(snapshot.DeclaringTypeName);
            if (declaringType is not null && declaringType.IsAssignableFrom(ownerType))
            {
                return declaringType.GetProperty(snapshot.FieldName, DeclaredPropertyFlags);
            }

            Type currentType = ownerType;
            while (currentType is not null && currentType != typeof(Component) && currentType != typeof(object))
            {
                PropertyInfo property = currentType.GetProperty(snapshot.FieldName, DeclaredPropertyFlags);
                if (property is not null)
                {
                    return property;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        private static Type ResolveType(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return null;
            }

            Type type = Type.GetType(assemblyQualifiedName, false);
            if (type is not null)
            {
                return type;
            }

            string fullTypeName = assemblyQualifiedName.Split(',')[0].Trim();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(fullTypeName, false);
                if (type is not null)
                {
                    return type;
                }
            }

            return null;
        }

        private static IEnumerable<FieldInfo> GetSerializableFields(Type type, Type stopType)
        {
            Type currentType = type;
            while (currentType is not null && currentType != stopType && currentType != typeof(object))
            {
                FieldInfo[] fields = currentType.GetFields(DeclaredFieldFlags);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (IsUnitySerializedField(fields[i]))
                    {
                        yield return fields[i];
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        private static IEnumerable<PropertyInfo> GetSerializableProperties(Type type, Type stopType)
        {
            Type currentType = type;
            while (currentType is not null && currentType != stopType && currentType != typeof(object))
            {
                PropertyInfo[] properties = currentType.GetProperties(DeclaredPropertyFlags);
                for (int i = 0; i < properties.Length; i++)
                {
                    PropertyInfo property = properties[i];
                    if (property.CanRead && property.CanWrite &&
                        property.GetIndexParameters().Length == 0 &&
                        property.GetMethod is not null &&
                        property.SetMethod is not null &&
                        property.GetMethod.IsPublic &&
                        property.SetMethod.IsPublic)
                    {
                        yield return property;
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        private static bool IsUnitySerializedField(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly || field.IsNotSerialized)
            {
                return false;
            }

            if (field.IsDefined(typeof(SerializeReference), true))
            {
                return false;
            }

            return field.IsPublic || field.IsDefined(typeof(SerializeField), true);
        }

        private static bool IsSupportedValueType(Type type)
        {
            if (type is null || typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                type.IsPointer || type == typeof(IntPtr) || type == typeof(UIntPtr))
            {
                return false;
            }

            if (IsScalarType(type) || type.IsEnum)
            {
                return true;
            }

            if (type.IsArray)
            {
                return type.GetArrayRank() == 1 && IsSupportedValueType(type.GetElementType());
            }

            if (IsListType(type))
            {
                return IsSupportedValueType(type.GetGenericArguments()[0]);
            }

            return type.IsSerializable && !type.IsAbstract && !type.ContainsGenericParameters;
        }

        private static bool IsListType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static bool IsScalarType(Type type)
        {
            return type == typeof(string) || type == typeof(bool) || type == typeof(char) ||
                   type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double);
        }

        private static string ScalarToString(object value, Type type)
        {
            if (type == typeof(string))
            {
                return (string)value;
            }

            if (type == typeof(bool))
            {
                return (bool)value ? "1" : "0";
            }

            if (type == typeof(char))
            {
                return ((int)(char)value).ToString(CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static object StringToScalar(string value, Type type)
        {
            if (type == typeof(string))
            {
                return value;
            }

            if (type == typeof(bool))
            {
                return value == "1";
            }

            if (type == typeof(char))
            {
                return (char)int.Parse(value, CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object left, object right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(object value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }
    }
}
