using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Profiling;

namespace AsiTimeLine.RunTime
{
    public enum NavigationStableUnitDomain : byte
    {
        ClientRole = 1,
        ClientMonsterAoi = 2,
        ServerMonsterAoi = 3,
        ServerPlayerRole = 4,
        PcgPlacement = 5,
        AuthoringPreview = 6,
    }

    public static class NavigationStableUnitId
    {
        private const int DomainShift = 60;
        private const ulong PayloadMask = (1UL << DomainShift) - 1UL;
        private const byte MaxDomain =
            (byte)NavigationStableUnitDomain.AuthoringPreview;

        public static ulong FromPositiveInt64(
            NavigationStableUnitDomain domain,
            long sourceId)
        {
            if (sourceId <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceId));
            }

            return Compose(domain, (ulong)sourceId);
        }

        public static ulong FromPositiveInt64Pair(
            NavigationStableUnitDomain domain,
            long primarySourceId,
            long secondarySourceId)
        {
            if (primarySourceId <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(primarySourceId));
            }
            if (secondarySourceId <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(secondarySourceId));
            }

            const ulong offsetBasis = 14695981039346656037UL;
            ulong hash = offsetBasis;
            HashUInt64(ref hash, (ulong)primarySourceId);
            HashUInt64(ref hash, (ulong)secondarySourceId);
            return FromSourceHash(domain, hash);
        }

        public static ulong FromWorldScopedPositiveInt64(
            NavigationStableUnitDomain domain,
            long worldId,
            long sourceId)
        {
            if (worldId <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(worldId));
            }
            if (sourceId <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceId));
            }

            const ulong offsetBasis = 14695981039346656037UL;
            ulong hash = offsetBasis;
            HashUInt64(ref hash, (ulong)worldId);
            HashUInt64(ref hash, (ulong)sourceId);
            return FromSourceHash(domain, hash);
        }

        public static ulong FromStableStringAndPositiveInt64(
            NavigationStableUnitDomain domain,
            string primarySourceId,
            long secondarySourceId)
        {
            ValidateStableString(primarySourceId, nameof(primarySourceId));
            if (secondarySourceId <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(secondarySourceId));
            }

            const ulong offsetBasis = 14695981039346656037UL;
            ulong hash = offsetBasis;
            HashStableString(ref hash, primarySourceId);
            HashUInt64(ref hash, (ulong)secondarySourceId);
            return FromSourceHash(domain, hash);
        }

        public static ulong FromWorldScopedStableString(
            NavigationStableUnitDomain domain,
            long worldId,
            string sourceId)
        {
            if (worldId <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(worldId));
            }
            ValidateStableString(sourceId, nameof(sourceId));

            const ulong offsetBasis = 14695981039346656037UL;
            ulong hash = offsetBasis;
            HashUInt64(ref hash, (ulong)worldId);
            HashStableString(ref hash, sourceId);
            return FromSourceHash(domain, hash);
        }

        public static NavigationStableUnitDomain GetDomain(ulong stableUnitId)
        {
            Validate(stableUnitId);
            return (NavigationStableUnitDomain)(stableUnitId >> DomainShift);
        }

        public static ulong GetSourcePayload(ulong stableUnitId)
        {
            Validate(stableUnitId);
            return stableUnitId & PayloadMask;
        }

        public static void Validate(ulong stableUnitId)
        {
            byte domain = (byte)(stableUnitId >> DomainShift);
            ulong payload = stableUnitId & PayloadMask;
            if (domain == 0 || domain > MaxDomain || payload == 0UL)
            {
                throw new ArgumentOutOfRangeException(nameof(stableUnitId));
            }
        }

        private static ulong Compose(
            NavigationStableUnitDomain domain,
            ulong payload)
        {
            byte domainValue = (byte)domain;
            if (domainValue == 0 || domainValue > MaxDomain)
            {
                throw new ArgumentOutOfRangeException(nameof(domain));
            }
            if (payload == 0UL || payload > PayloadMask)
            {
                throw new ArgumentOutOfRangeException(nameof(payload));
            }

            return ((ulong)domainValue << DomainShift) | payload;
        }

        private static ulong FromSourceHash(
            NavigationStableUnitDomain domain,
            ulong sourceHash)
        {
            ulong payload = sourceHash & PayloadMask;
            if (payload == 0UL)
            {
                payload = 1UL;
            }
            return Compose(domain, payload);
        }

        private static void ValidateStableString(
            string sourceId,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(sourceId) ||
                !string.Equals(
                    sourceId,
                    sourceId.Trim(),
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Stable source ID must be non-blank and exact.",
                    parameterName);
            }
        }

        private static void HashStableString(ref ulong hash, string value)
        {
            HashUInt64(ref hash, (ulong)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                HashUInt64(ref hash, value[i]);
            }
        }

        private static void HashUInt64(ref ulong hash, ulong value)
        {
            const ulong prime = 1099511628211UL;
            unchecked
            {
                for (int shift = 0; shift < 64; shift += 8)
                {
                    hash ^= (byte)(value >> shift);
                    hash *= prime;
                }
            }
        }
    }

    public enum NavigationQueryPriority : byte
    {
        Normal = 0,
        Player = 1,
        Stuck = 2,
    }

    public enum NavigationQueryApi : byte
    {
        SamplePosition = 0,
        CalculatePath = 1,
        Raycast = 2,
        FindClosestEdge = 3,
        Corners = 4,
        Candidates = 5,
    }

    public interface INavigationQueryWorkItem
    {
        bool ExecuteNavigationQuery(ulong ownerLease);
    }

    public readonly struct NavigationQueryCounters
    {
        internal NavigationQueryCounters(in MutableCounters counters)
        {
            Requested = counters.Requested;
            Coalesced = counters.Coalesced;
            Skipped = counters.Skipped;
            Executed = counters.Executed;
            Failed = counters.Failed;
            Canceled = counters.Canceled;
            Superseded = counters.Superseded;
            StaleOwnerRejected = counters.StaleOwnerRejected;
            StaleRevisionRejected = counters.StaleRevisionRejected;
            SamplePosition = counters.SamplePosition;
            CalculatePath = counters.CalculatePath;
            Raycast = counters.Raycast;
            FindClosestEdge = counters.FindClosestEdge;
            Corners = counters.Corners;
            Candidates = counters.Candidates;
        }

        public ulong Requested { get; }
        public ulong Coalesced { get; }
        public ulong Skipped { get; }
        public ulong Executed { get; }
        public ulong Failed { get; }
        public ulong Canceled { get; }
        public ulong Superseded { get; }
        public ulong StaleOwnerRejected { get; }
        public ulong StaleRevisionRejected { get; }
        public ulong SamplePosition { get; }
        public ulong CalculatePath { get; }
        public ulong Raycast { get; }
        public ulong FindClosestEdge { get; }
        public ulong Corners { get; }
        public ulong Candidates { get; }
    }

    public readonly struct NavigationQueryTelemetrySnapshot
    {
        internal NavigationQueryTelemetrySnapshot(
            int maxExecutionsPerFrame,
            int activeOwnerCount,
            int pendingCount,
            ulong currentFrame,
            ulong navigationSequence,
            ulong maxBacklogFrames,
            in MutableCounters cumulative,
            in MutableCounters currentFrameCounters)
        {
            MaxExecutionsPerFrame = maxExecutionsPerFrame;
            ActiveOwnerCount = activeOwnerCount;
            PendingCount = pendingCount;
            CurrentFrame = currentFrame;
            NavigationSequence = navigationSequence;
            MaxBacklogFrames = maxBacklogFrames;
            Cumulative = new NavigationQueryCounters(cumulative);
            CurrentFrameCounters = new NavigationQueryCounters(
                currentFrameCounters);
        }

        public int MaxExecutionsPerFrame { get; }
        public int ActiveOwnerCount { get; }
        public int PendingCount { get; }
        public ulong CurrentFrame { get; }
        public ulong NavigationSequence { get; }
        public ulong MaxBacklogFrames { get; }
        public NavigationQueryCounters Cumulative { get; }
        public NavigationQueryCounters CurrentFrameCounters { get; }
    }

    internal struct MutableCounters
    {
        public ulong Requested;
        public ulong Coalesced;
        public ulong Skipped;
        public ulong Executed;
        public ulong Failed;
        public ulong Canceled;
        public ulong Superseded;
        public ulong StaleOwnerRejected;
        public ulong StaleRevisionRejected;
        public ulong SamplePosition;
        public ulong CalculatePath;
        public ulong Raycast;
        public ulong FindClosestEdge;
        public ulong Corners;
        public ulong Candidates;
    }

    /// <summary>
    /// Main-thread queue that drains path work in priority, backlog and stable-ID
    /// order. The queue owns ordering; components never execute granted work inline.
    /// </summary>
    public sealed class DeterministicNavigationQueryBudget
    {
        private sealed class Registration
        {
            public ulong StableUnitId;
            public ulong OwnerLease;
            public INavigationQueryWorkItem WorkItem;
            public NavigationQueryPriority Priority;
            public bool Pending;
            public bool AwaitingResult;
            public ulong FirstPendingFrame;
        }

        private sealed class CandidateComparer : IComparer<Registration>
        {
            public static readonly CandidateComparer Instance =
                new CandidateComparer();

            public int Compare(Registration left, Registration right)
            {
                int priority = right.Priority.CompareTo(left.Priority);
                if (priority != 0)
                {
                    return priority;
                }

                int pendingFrame = left.FirstPendingFrame.CompareTo(
                    right.FirstPendingFrame);
                if (pendingFrame != 0)
                {
                    return pendingFrame;
                }

                return left.StableUnitId.CompareTo(right.StableUnitId);
            }
        }

        private readonly Dictionary<ulong, Registration> registrations =
            new Dictionary<ulong, Registration>(128);
        private readonly List<Registration> candidates =
            new List<Registration>(128);
        private int maxExecutionsPerFrame;
        private int pendingCount;
        private ulong nextOwnerLease;
        private ulong currentFrame;
        private bool hasCurrentFrame;
        private ulong navigationSequence;
        private bool hasNavigationSequence;
        private ulong maxBacklogFrames;
        private MutableCounters cumulative;
        private MutableCounters currentFrameCounters;

        public DeterministicNavigationQueryBudget(int maxExecutionsPerFrame)
        {
            ConfigureMaxExecutionsPerFrame(maxExecutionsPerFrame);
        }

        public int MaxExecutionsPerFrame => maxExecutionsPerFrame;

        public void ConfigureMaxExecutionsPerFrame(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            maxExecutionsPerFrame = value;
        }

        public void BeginFrame(ulong frame)
        {
            if (hasCurrentFrame && currentFrame == frame)
            {
                return;
            }

            currentFrame = frame;
            hasCurrentFrame = true;
            currentFrameCounters = default;
        }

        public void SetNavigationSequence(ulong value)
        {
            if (hasNavigationSequence && navigationSequence == value)
            {
                return;
            }

            navigationSequence = value;
            hasNavigationSequence = true;
            foreach (KeyValuePair<ulong, Registration> pair in registrations)
            {
                CancelPending(pair.Value);
            }
        }

        public ulong Register(
            ulong stableUnitId,
            INavigationQueryWorkItem workItem)
        {
            if (stableUnitId == 0UL)
            {
                throw new ArgumentOutOfRangeException(nameof(stableUnitId));
            }
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            ulong ownerLease = NextNonZero(ref nextOwnerLease);
            if (registrations.TryGetValue(
                    stableUnitId,
                    out Registration existing))
            {
                if (existing.AwaitingResult)
                {
                    throw new InvalidOperationException(
                        "Navigation query ownership changed during execution.");
                }
                CancelPending(existing);
                existing.OwnerLease = ownerLease;
                existing.WorkItem = workItem;
                existing.Priority = NavigationQueryPriority.Normal;
                Increment(ref cumulative.Superseded);
                Increment(ref currentFrameCounters.Superseded);
                return ownerLease;
            }

            registrations.Add(
                stableUnitId,
                new Registration
                {
                    StableUnitId = stableUnitId,
                    OwnerLease = ownerLease,
                    WorkItem = workItem,
                    Priority = NavigationQueryPriority.Normal,
                });
            return ownerLease;
        }

        public bool IsOwner(ulong stableUnitId, ulong ownerLease)
        {
            return TryGetOwned(stableUnitId, ownerLease, out _);
        }

        public bool Unregister(ulong stableUnitId, ulong ownerLease)
        {
            if (!TryGetOwned(stableUnitId, ownerLease, out Registration owned))
            {
                RecordStaleOwner();
                return false;
            }
            if (owned.AwaitingResult)
            {
                throw new InvalidOperationException(
                    "Navigation query owner unregistered during execution.");
            }

            CancelPending(owned);
            registrations.Remove(stableUnitId);
            return true;
        }

        public bool InvalidateStableUnit(ulong stableUnitId)
        {
            if (!registrations.TryGetValue(
                    stableUnitId,
                    out Registration registration))
            {
                return false;
            }
            if (registration.AwaitingResult)
            {
                throw new InvalidOperationException(
                    "Navigation stable identity changed during execution.");
            }

            CancelPending(registration);
            registrations.Remove(stableUnitId);
            return true;
        }

        public bool Cancel(ulong stableUnitId, ulong ownerLease)
        {
            if (!TryGetOwned(stableUnitId, ownerLease, out Registration owned))
            {
                RecordStaleOwner();
                return false;
            }
            if (owned.AwaitingResult)
            {
                throw new InvalidOperationException(
                    "Navigation query owner canceled during execution.");
            }

            return CancelPending(owned);
        }

        public bool Queue(
            ulong stableUnitId,
            ulong ownerLease,
            NavigationQueryPriority priority,
            ulong requestNavigationSequence,
            ulong frame)
        {
            BeginFrame(frame);
            if ((byte)priority > (byte)NavigationQueryPriority.Stuck)
            {
                throw new ArgumentOutOfRangeException(nameof(priority));
            }
            if (!hasNavigationSequence ||
                requestNavigationSequence != navigationSequence)
            {
                Increment(ref cumulative.StaleRevisionRejected);
                Increment(ref currentFrameCounters.StaleRevisionRejected);
                return false;
            }
            if (!TryGetOwned(stableUnitId, ownerLease, out Registration owned))
            {
                RecordStaleOwner();
                return false;
            }
            if (owned.AwaitingResult)
            {
                throw new InvalidOperationException(
                    "Navigation work was queued during its own execution.");
            }

            owned.Priority = priority;
            if (owned.Pending)
            {
                Increment(ref cumulative.Coalesced);
                Increment(ref currentFrameCounters.Coalesced);
                return true;
            }

            owned.Pending = true;
            owned.FirstPendingFrame = frame;
            pendingCount++;
            Increment(ref cumulative.Requested);
            Increment(ref currentFrameCounters.Requested);
            return true;
        }

        public int Drain(ulong frame, ulong authoritativeNavigationSequence)
        {
            BeginFrame(frame);
            SetNavigationSequence(authoritativeNavigationSequence);
            candidates.Clear();
            foreach (KeyValuePair<ulong, Registration> pair in registrations)
            {
                Registration registration = pair.Value;
                if (!registration.Pending)
                {
                    continue;
                }

                candidates.Add(registration);
                UpdateMaxBacklog(BacklogAge(
                    frame,
                    registration.FirstPendingFrame));
            }

            candidates.Sort(CandidateComparer.Instance);
            int remainingExecutions =
                currentFrameCounters.Executed >=
                (ulong)maxExecutionsPerFrame
                    ? 0
                    : maxExecutionsPerFrame -
                      (int)currentFrameCounters.Executed;
            int selectedCount = Math.Min(
                remainingExecutions,
                candidates.Count);
            int skippedCount = candidates.Count - selectedCount;
            if (skippedCount > 0)
            {
                Add(ref cumulative.Skipped, (ulong)skippedCount);
                Add(ref currentFrameCounters.Skipped, (ulong)skippedCount);
            }

            int executedCount = 0;
            for (int i = 0; i < selectedCount; i++)
            {
                Registration registration = candidates[i];
                if (!registration.Pending ||
                    !registrations.TryGetValue(
                        registration.StableUnitId,
                        out Registration current) ||
                    !ReferenceEquals(registration, current))
                {
                    continue;
                }

                registration.Pending = false;
                registration.AwaitingResult = true;
                pendingCount--;
                executedCount++;
                Increment(ref cumulative.Executed);
                Increment(ref currentFrameCounters.Executed);
                bool success = false;
                try
                {
                    success = registration.WorkItem.ExecuteNavigationQuery(
                        registration.OwnerLease);
                }
                finally
                {
                    registration.AwaitingResult = false;
                    if (!success)
                    {
                        Increment(ref cumulative.Failed);
                        Increment(ref currentFrameCounters.Failed);
                    }
                }
            }
            return executedCount;
        }

        public void Shutdown()
        {
            foreach (KeyValuePair<ulong, Registration> pair in registrations)
            {
                if (pair.Value.AwaitingResult)
                {
                    throw new InvalidOperationException(
                        "Navigation query scheduler shut down during execution.");
                }
                CancelPending(pair.Value);
            }
            registrations.Clear();
            candidates.Clear();
            pendingCount = 0;
            hasNavigationSequence = false;
            navigationSequence = 0UL;
        }

        internal void Reset(int maxExecutions)
        {
            Shutdown();
            ConfigureMaxExecutionsPerFrame(maxExecutions);
            nextOwnerLease = 0UL;
            currentFrame = 0UL;
            hasCurrentFrame = false;
            navigationSequence = 0UL;
            hasNavigationSequence = false;
            maxBacklogFrames = 0UL;
            cumulative = default;
            currentFrameCounters = default;
        }

        public void RecordApi(
            NavigationQueryApi api,
            ulong frame,
            int count = 1)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (count == 0)
            {
                return;
            }

            BeginFrame(frame);
            AddApiCount(ref cumulative, api, (ulong)count);
            AddApiCount(ref currentFrameCounters, api, (ulong)count);
        }

        public NavigationQueryTelemetrySnapshot CaptureTelemetry()
        {
            return new NavigationQueryTelemetrySnapshot(
                maxExecutionsPerFrame,
                registrations.Count,
                pendingCount,
                hasCurrentFrame ? currentFrame : 0UL,
                hasNavigationSequence ? navigationSequence : 0UL,
                maxBacklogFrames,
                cumulative,
                currentFrameCounters);
        }

        private bool CancelPending(Registration registration)
        {
            if (!registration.Pending)
            {
                return false;
            }

            registration.Pending = false;
            pendingCount--;
            Increment(ref cumulative.Canceled);
            Increment(ref currentFrameCounters.Canceled);
            return true;
        }

        private bool TryGetOwned(
            ulong stableUnitId,
            ulong ownerLease,
            out Registration registration)
        {
            return registrations.TryGetValue(stableUnitId, out registration) &&
                   registration.OwnerLease == ownerLease &&
                   ownerLease != 0UL;
        }

        private void RecordStaleOwner()
        {
            Increment(ref cumulative.StaleOwnerRejected);
            Increment(ref currentFrameCounters.StaleOwnerRejected);
        }

        private void UpdateMaxBacklog(ulong value)
        {
            if (value > maxBacklogFrames)
            {
                maxBacklogFrames = value;
            }
        }

        private static ulong BacklogAge(ulong frame, ulong firstFrame)
        {
            return frame >= firstFrame ? frame - firstFrame : 0UL;
        }

        private static void AddApiCount(
            ref MutableCounters counters,
            NavigationQueryApi api,
            ulong count)
        {
            switch (api)
            {
                case NavigationQueryApi.SamplePosition:
                    Add(ref counters.SamplePosition, count);
                    break;
                case NavigationQueryApi.CalculatePath:
                    Add(ref counters.CalculatePath, count);
                    break;
                case NavigationQueryApi.Raycast:
                    Add(ref counters.Raycast, count);
                    break;
                case NavigationQueryApi.FindClosestEdge:
                    Add(ref counters.FindClosestEdge, count);
                    break;
                case NavigationQueryApi.Corners:
                    Add(ref counters.Corners, count);
                    break;
                case NavigationQueryApi.Candidates:
                    Add(ref counters.Candidates, count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(api));
            }
        }

        private static ulong NextNonZero(ref ulong value)
        {
            unchecked
            {
                value++;
            }
            if (value == 0UL)
            {
                value = 1UL;
            }
            return value;
        }

        private static void Increment(ref ulong value)
        {
            Add(ref value, 1UL);
        }

        private static void Add(ref ulong value, ulong amount)
        {
            ulong remaining = ulong.MaxValue - value;
            value = amount > remaining ? ulong.MaxValue : value + amount;
        }
    }

    public static class NavigationQueryApiRuntime
    {
        private static class Markers
        {
            static Markers()
            {
            }

            internal static readonly ProfilerMarker SamplePosition =
                new ProfilerMarker("Navigation.Query.SamplePosition");
            internal static readonly ProfilerMarker CalculatePath =
                new ProfilerMarker("Navigation.Query.CalculatePath");
            internal static readonly ProfilerMarker Raycast =
                new ProfilerMarker("Navigation.Query.Raycast");
            internal static readonly ProfilerMarker FindClosestEdge =
                new ProfilerMarker("Navigation.Query.FindClosestEdge");
        }

        public static bool SamplePosition(
            Vector3 position,
            out NavMeshHit hit,
            float maxDistance,
            int areaMask)
        {
            if (!NavigationRevisionRuntime.IsQueryReady)
            {
                hit = default;
                return false;
            }
            NavigationQueryBudgetRuntime.RecordApi(
                NavigationQueryApi.SamplePosition);
            using (Markers.SamplePosition.Auto())
            {
                return NavMesh.SamplePosition(
                    position,
                    out hit,
                    maxDistance,
                    areaMask);
            }
        }

        public static bool CalculatePath(
            Vector3 source,
            Vector3 target,
            int areaMask,
            NavMeshPath path)
        {
            if (!NavigationRevisionRuntime.IsQueryReady)
            {
                if (path != null)
                {
                    path.ClearCorners();
                }
                return false;
            }
            NavigationQueryBudgetRuntime.RecordApi(
                NavigationQueryApi.CalculatePath);
            using (Markers.CalculatePath.Auto())
            {
                return NavMesh.CalculatePath(source, target, areaMask, path);
            }
        }

        public static bool Raycast(
            Vector3 source,
            Vector3 target,
            out NavMeshHit hit,
            int areaMask)
        {
            if (!NavigationRevisionRuntime.IsQueryReady)
            {
                // NavMesh.Raycast returns true for a blocked segment. Treat an
                // unavailable authoritative mesh as blocked, never as clear.
                hit = default;
                return true;
            }
            NavigationQueryBudgetRuntime.RecordApi(
                NavigationQueryApi.Raycast);
            using (Markers.Raycast.Auto())
            {
                return NavMesh.Raycast(source, target, out hit, areaMask);
            }
        }

        public static bool FindClosestEdge(
            Vector3 position,
            out NavMeshHit hit,
            int areaMask)
        {
            if (!NavigationRevisionRuntime.IsQueryReady)
            {
                hit = default;
                return false;
            }
            NavigationQueryBudgetRuntime.RecordApi(
                NavigationQueryApi.FindClosestEdge);
            using (Markers.FindClosestEdge.Auto())
            {
                return NavMesh.FindClosestEdge(position, out hit, areaMask);
            }
        }
    }

    public sealed class DeterministicNavigationFrameClock
    {
        private string externalOwnerId;
        private ulong externalOwnerLease;
        private ulong nextOwnerLease;
        private ulong externalFrame;

        public bool IsExternal => externalOwnerLease != 0UL;
        public ulong ExternalFrame => externalFrame;

        public ulong ClaimExternal(string ownerId)
        {
            string exactOwnerId = RequireExactOwnerId(ownerId);
            ValidateExternalClaim(exactOwnerId);
            if (externalOwnerLease != 0UL)
            {
                return externalOwnerLease;
            }

            externalOwnerId = exactOwnerId;
            externalOwnerLease = NextNonZero(ref nextOwnerLease);
            externalFrame = 0UL;
            return externalOwnerLease;
        }

        public void AdvanceExternal(ulong ownerLease, ulong frame)
        {
            RequireOwner(ownerLease);
            if (frame == 0UL)
            {
                throw new ArgumentOutOfRangeException(nameof(frame));
            }
            if (externalFrame == ulong.MaxValue ||
                frame != externalFrame + 1UL)
            {
                throw new InvalidOperationException(
                    "External navigation frame must advance by exactly one authoritative tick.");
            }

            externalFrame = frame;
        }

        public void ReleaseExternal(ulong ownerLease)
        {
            RequireOwner(ownerLease);
            externalOwnerId = null;
            externalOwnerLease = 0UL;
            externalFrame = 0UL;
        }

        public ulong Resolve(ulong fallbackFrame)
        {
            return IsExternal ? externalFrame : fallbackFrame;
        }

        public void ValidateExternalClaim(string ownerId)
        {
            string exactOwnerId = RequireExactOwnerId(ownerId);
            if (externalOwnerLease != 0UL &&
                !string.Equals(
                    externalOwnerId,
                    exactOwnerId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Navigation frame clock is already owned by '{externalOwnerId}'.");
            }
        }

        internal void Reset()
        {
            externalOwnerId = null;
            externalOwnerLease = 0UL;
            nextOwnerLease = 0UL;
            externalFrame = 0UL;
        }

        private void RequireOwner(ulong ownerLease)
        {
            if (ownerLease == 0UL || ownerLease != externalOwnerLease)
            {
                throw new InvalidOperationException(
                    "Navigation frame clock lease is not current.");
            }
        }

        private static string RequireExactOwnerId(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId) ||
                !string.Equals(ownerId, ownerId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A non-blank, already-normalized clock owner id is required.",
                    nameof(ownerId));
            }
            return ownerId;
        }

        private static ulong NextNonZero(ref ulong value)
        {
            unchecked
            {
                value++;
            }
            if (value == 0UL)
            {
                value = 1UL;
            }
            return value;
        }
    }

    public static class NavigationQueryBudgetRuntime
    {
        public const int DefaultMaxExecutionsPerFrame = 16;

        private static class Markers
        {
            static Markers()
            {
            }

            internal static readonly ProfilerMarker Drain =
                new ProfilerMarker("Navigation.QueryBudget.Drain");
        }

        private static readonly DeterministicNavigationQueryBudget Budget =
            new DeterministicNavigationQueryBudget(
                DefaultMaxExecutionsPerFrame);
        private static readonly DeterministicNavigationFrameClock FrameClock =
            new DeterministicNavigationFrameClock();

        public static int MaxExecutionsPerFrame =>
            Budget.MaxExecutionsPerFrame;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Budget.Reset(DefaultMaxExecutionsPerFrame);
            FrameClock.Reset();
        }

        public static void ConfigureMaxExecutionsPerFrame(int value)
        {
            Budget.ConfigureMaxExecutionsPerFrame(value);
        }

        /// <summary>
        /// Claims the process-wide frame clock for an external fixed-tick driver.
        /// Repeating the claim with the same owner is idempotent; another owner fails.
        /// </summary>
        public static ulong ClaimExternalFrameClock(string ownerId)
        {
            string exactOwnerId = RequireExactOwnerId(ownerId);
            ValidateExternalFrameClockClaim(exactOwnerId);
            ulong ownerLease = FrameClock.ClaimExternal(exactOwnerId);
            SynchronizeAuthoritativeState();
            return ownerLease;
        }

        public static void ValidateExternalFrameClockClaim(string ownerId)
        {
            string exactOwnerId = RequireExactOwnerId(ownerId);
            FrameClock.ValidateExternalClaim(exactOwnerId);
            if (!FrameClock.IsExternal)
            {
                NavigationQueryTelemetrySnapshot snapshot =
                    Budget.CaptureTelemetry();
                if (snapshot.ActiveOwnerCount != 0 || snapshot.PendingCount != 0)
                {
                    throw new InvalidOperationException(
                        "External navigation clock must be claimed before units register queries.");
                }
            }
        }

        /// <summary>Advances the claimed external clock once per authoritative tick.</summary>
        public static void AdvanceExternalFrame(ulong ownerLease)
        {
            ulong currentFrame = FrameClock.ExternalFrame;
            if (currentFrame == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "External navigation frame clock exhausted its ulong range.");
            }
            FrameClock.AdvanceExternal(ownerLease, currentFrame + 1UL);
            SynchronizeAuthoritativeState();
        }

        public static ulong Register(
            ulong stableUnitId,
            INavigationQueryWorkItem workItem)
        {
            SynchronizeAuthoritativeState();
            return Budget.Register(stableUnitId, workItem);
        }

        public static bool IsOwner(ulong stableUnitId, ulong ownerLease)
        {
            return Budget.IsOwner(stableUnitId, ownerLease);
        }

        public static bool Unregister(ulong stableUnitId, ulong ownerLease)
        {
            SynchronizeAuthoritativeState();
            return Budget.Unregister(stableUnitId, ownerLease);
        }

        public static bool InvalidateStableUnit(ulong stableUnitId)
        {
            SynchronizeAuthoritativeState();
            return Budget.InvalidateStableUnit(stableUnitId);
        }

        public static bool Cancel(ulong stableUnitId, ulong ownerLease)
        {
            SynchronizeAuthoritativeState();
            return Budget.Cancel(stableUnitId, ownerLease);
        }

        public static bool Queue(
            ulong stableUnitId,
            ulong ownerLease,
            NavigationQueryPriority priority,
            ulong requestNavigationSequence)
        {
            SynchronizeAuthoritativeState();
            return Budget.Queue(
                stableUnitId,
                ownerLease,
                priority,
                requestNavigationSequence,
                CurrentFrame);
        }

        public static int Drain()
        {
            using (Markers.Drain.Auto())
            {
                return Budget.Drain(
                    CurrentFrame,
                    NavigationRevisionRuntime.ChangeSequence);
            }
        }

        public static void Shutdown()
        {
            SynchronizeAuthoritativeState();
            Budget.Shutdown();
        }

        public static void RecordApi(
            NavigationQueryApi api,
            int count = 1)
        {
            Budget.RecordApi(api, CurrentFrame, count);
        }

        public static NavigationQueryTelemetrySnapshot CaptureTelemetry()
        {
            return Budget.CaptureTelemetry();
        }

        private static void SynchronizeAuthoritativeState()
        {
            Budget.BeginFrame(CurrentFrame);
            Budget.SetNavigationSequence(
                NavigationRevisionRuntime.ChangeSequence);
        }

        private static string RequireExactOwnerId(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId) ||
                !string.Equals(ownerId, ownerId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A non-blank, already-normalized clock owner id is required.",
                    nameof(ownerId));
            }
            return ownerId;
        }

        private static ulong CurrentFrame =>
            FrameClock.Resolve(unchecked((uint)Time.frameCount));
    }
}
