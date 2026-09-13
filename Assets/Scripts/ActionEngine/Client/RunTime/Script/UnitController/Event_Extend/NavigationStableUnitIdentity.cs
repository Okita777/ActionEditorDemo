using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsiActionEngine.RunTime;

namespace AsiTimeLine.RunTime
{
    public static class NavigationStableUnitIdentity
    {
        private sealed class UnitReferenceComparer :
            IEqualityComparer<ActionEngine_Unit>
        {
            public static readonly UnitReferenceComparer Instance =
                new UnitReferenceComparer();

            public bool Equals(ActionEngine_Unit left, ActionEngine_Unit right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(ActionEngine_Unit unit)
            {
                return RuntimeHelpers.GetHashCode(unit);
            }
        }

        private static readonly Dictionary<ActionEngine_Unit, ulong>
            StableIdByUnit = new Dictionary<ActionEngine_Unit, ulong>(
                UnitReferenceComparer.Instance);
        private static readonly Dictionary<ulong, ActionEngine_Unit>
            UnitByStableId = new Dictionary<ulong, ActionEngine_Unit>();

        public static void Bind(
            ActionEngine_Unit unit,
            ulong stableUnitId)
        {
            if (ReferenceEquals(unit, null))
            {
                throw new ArgumentNullException(nameof(unit));
            }
            NavigationStableUnitId.Validate(stableUnitId);

            if (StableIdByUnit.TryGetValue(unit, out ulong currentId))
            {
                if (currentId == stableUnitId)
                {
                    return;
                }
                throw new InvalidOperationException(
                    $"Navigation stable identity cannot be rebound from {currentId} to {stableUnitId}.");
            }

            if (UnitByStableId.TryGetValue(
                    stableUnitId,
                    out ActionEngine_Unit existingUnit))
            {
                throw new InvalidOperationException(
                    $"Navigation stable unit ID is already bound: {stableUnitId}, existing={existingUnit.name}.");
            }

            StableIdByUnit.Add(unit, stableUnitId);
            UnitByStableId.Add(stableUnitId, unit);
        }

        public static ulong Require(ActionEngine_Unit unit)
        {
            if (ReferenceEquals(unit, null))
            {
                throw new ArgumentNullException(nameof(unit));
            }
            if (!StableIdByUnit.TryGetValue(unit, out ulong stableUnitId))
            {
                throw new InvalidOperationException(
                    $"ActionEngine unit has no navigation stable identity: {unit.name}.");
            }
            return stableUnitId;
        }

        public static bool Clear(ActionEngine_Unit unit)
        {
            if (ReferenceEquals(unit, null) ||
                !StableIdByUnit.TryGetValue(unit, out ulong stableUnitId))
            {
                return false;
            }

            Release(unit, stableUnitId);
            return true;
        }

        public static void Shutdown()
        {
            StableIdByUnit.Clear();
            UnitByStableId.Clear();
        }

#if UNITY_EDITOR
        public static long CreateAuthoringSourceId()
        {
            const ulong sourceMask = (1UL << 60) - 1UL;
            ulong sourceId;
            do
            {
                byte[] bytes = Guid.NewGuid().ToByteArray();
                sourceId = BitConverter.ToUInt64(bytes, 0) & sourceMask;
            }
            while (sourceId == 0UL);
            return (long)sourceId;
        }
#endif

        private static void Release(
            ActionEngine_Unit unit,
            ulong stableUnitId)
        {
            if (!UnitByStableId.TryGetValue(
                    stableUnitId,
                    out ActionEngine_Unit registeredUnit) ||
                !ReferenceEquals(unit, registeredUnit))
            {
                throw new InvalidOperationException(
                    $"Navigation stable identity registry is inconsistent: {stableUnitId}.");
            }

            NavigationQueryBudgetRuntime.InvalidateStableUnit(stableUnitId);
            UnitByStableId.Remove(stableUnitId);
            StableIdByUnit.Remove(unit);
        }
    }
}
