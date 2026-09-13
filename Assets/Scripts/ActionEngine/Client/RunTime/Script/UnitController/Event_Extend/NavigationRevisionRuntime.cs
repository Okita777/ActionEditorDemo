using System;

namespace AsiTimeLine.RunTime
{
    /// <summary>
    /// Active gameplay navigation identity. The Game layer maps its world-specific
    /// contract into this ActionEngine-neutral value at stage boundaries.
    /// </summary>
    public readonly struct NavigationRuntimeRevision
    {
        public NavigationRuntimeRevision(
            string worldInstanceStableId,
            string scopeStableId,
            ulong navCommitSequence,
            ulong scopeGeneration,
            ulong activeAreaEpoch,
            string navigationSignature)
        {
            WorldInstanceStableId = RequireExactId(
                worldInstanceStableId,
                nameof(worldInstanceStableId));
            ScopeStableId = RequireExactId(scopeStableId, nameof(scopeStableId));
            if (navCommitSequence == 0UL)
                throw new ArgumentOutOfRangeException(nameof(navCommitSequence));
            if (scopeGeneration == 0UL)
                throw new ArgumentOutOfRangeException(nameof(scopeGeneration));
            NavigationSignature = RequireExactId(
                navigationSignature,
                nameof(navigationSignature));
            NavCommitSequence = navCommitSequence;
            ScopeGeneration = scopeGeneration;
            ActiveAreaEpoch = activeAreaEpoch;
        }

        public string WorldInstanceStableId { get; }
        public string ScopeStableId { get; }
        public ulong NavCommitSequence { get; }
        public ulong ScopeGeneration { get; }
        public ulong ActiveAreaEpoch { get; }
        public string NavigationSignature { get; }

        public bool SameIdentityAndContent(in NavigationRuntimeRevision other)
        {
            return NavCommitSequence == other.NavCommitSequence &&
                   ScopeGeneration == other.ScopeGeneration &&
                   ActiveAreaEpoch == other.ActiveAreaEpoch &&
                   string.Equals(
                       WorldInstanceStableId,
                       other.WorldInstanceStableId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       ScopeStableId,
                       other.ScopeStableId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       NavigationSignature,
                       other.NavigationSignature,
                       StringComparison.Ordinal);
        }

        private static string RequireExactId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A non-blank, already-normalized value is required.",
                    parameterName);
            }
            return value;
        }
    }

    /// <summary>
    /// One active client gameplay scope. This is deliberately not a per-world DS
    /// registry; overlapping authoritative DS worlds require an isolated query context.
    /// </summary>
    public static class NavigationRevisionRuntime
    {
        private static NavigationRuntimeRevision current;
        private static bool hasCurrent;
        private static string authorityWorldInstanceStableId;
        private static string authorityOwnerId;
        private static bool authorityBound;
        private static ulong changeSequence;

        public static ulong ChangeSequence => changeSequence;
        public static bool AuthorityBound => authorityBound;
        public static bool IsQueryReady => !authorityBound || hasCurrent;

        public static bool BindAuthority(
            string worldInstanceStableId,
            string ownerId)
        {
            string exactWorldId = RequireExactId(
                worldInstanceStableId,
                nameof(worldInstanceStableId));
            string exactOwnerId = RequireExactId(ownerId, nameof(ownerId));
            if (authorityBound &&
                string.Equals(
                    authorityWorldInstanceStableId,
                    exactWorldId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    authorityOwnerId,
                    exactOwnerId,
                    StringComparison.Ordinal))
            {
                return false;
            }
            current = default;
            hasCurrent = false;
            authorityWorldInstanceStableId = exactWorldId;
            authorityOwnerId = exactOwnerId;
            authorityBound = true;
            changeSequence = NextNonZero(changeSequence);
            return true;
        }

        public static bool TryGetCurrent(out NavigationRuntimeRevision revision)
        {
            revision = current;
            return hasCurrent;
        }

        public static bool Commit(
            in NavigationRuntimeRevision revision,
            string ownerId)
        {
            RequireAuthorityOwner(ownerId);
            if (!string.Equals(
                    authorityWorldInstanceStableId,
                    revision.WorldInstanceStableId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Navigation revision does not belong to the bound authority world.");
            }
            if (hasCurrent && current.SameIdentityAndContent(revision))
            {
                return false;
            }
            if (hasCurrent && IsStaleOrConflictingCommit(revision))
            {
                return false;
            }
            current = revision;
            hasCurrent = true;
            changeSequence = NextNonZero(changeSequence);
            return true;
        }

        public static bool Clear(
            in NavigationRuntimeRevision revision,
            string ownerId)
        {
            RequireAuthorityOwner(ownerId);
            if (!string.Equals(
                    authorityWorldInstanceStableId,
                    revision.WorldInstanceStableId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Navigation revision does not belong to the bound authority world.");
            }
            if (!hasCurrent ||
                !string.Equals(
                    current.ScopeStableId,
                    revision.ScopeStableId,
                    StringComparison.Ordinal))
            {
                return false;
            }
            if (revision.ActiveAreaEpoch != current.ActiveAreaEpoch ||
                revision.NavCommitSequence <= current.NavCommitSequence ||
                revision.ScopeGeneration <= current.ScopeGeneration)
            {
                return false;
            }
            if (!string.Equals(
                    revision.NavigationSignature,
                    current.NavigationSignature,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Navigation removal conflicts with the committed revision identity.");
            }
            current = default;
            hasCurrent = false;
            changeSequence = NextNonZero(changeSequence);
            return true;
        }

        public static bool UnbindAuthority(
            string worldInstanceStableId,
            string ownerId)
        {
            if (!IsAuthorityOwner(worldInstanceStableId, ownerId))
            {
                return false;
            }
            current = default;
            hasCurrent = false;
            authorityWorldInstanceStableId = null;
            authorityOwnerId = null;
            authorityBound = false;
            changeSequence = NextNonZero(changeSequence);
            return true;
        }

        public static bool SuspendAuthority(
            string worldInstanceStableId,
            string ownerId)
        {
            if (!IsAuthorityOwner(worldInstanceStableId, ownerId) || !hasCurrent)
            {
                return false;
            }
            current = default;
            hasCurrent = false;
            changeSequence = NextNonZero(changeSequence);
            return true;
        }

        public static void ClearAll()
        {
            if (!hasCurrent && !authorityBound)
            {
                return;
            }
            current = default;
            hasCurrent = false;
            authorityWorldInstanceStableId = null;
            authorityOwnerId = null;
            authorityBound = false;
            changeSequence = NextNonZero(changeSequence);
        }

        private static string RequireExactId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A non-blank, already-normalized value is required.",
                    parameterName);
            }
            return value;
        }

        private static bool IsAuthorityOwner(
            string worldInstanceStableId,
            string ownerId)
        {
            return authorityBound &&
                   string.Equals(
                       authorityWorldInstanceStableId,
                       worldInstanceStableId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       authorityOwnerId,
                       ownerId,
                       StringComparison.Ordinal);
        }

        private static void RequireAuthorityOwner(string ownerId)
        {
            string exactOwnerId = RequireExactId(ownerId, nameof(ownerId));
            if (!authorityBound ||
                !string.Equals(
                    authorityOwnerId,
                    exactOwnerId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Navigation authority owner is not current.");
            }
        }

        private static bool IsStaleOrConflictingCommit(
            in NavigationRuntimeRevision revision)
        {
            if (revision.ActiveAreaEpoch < current.ActiveAreaEpoch)
            {
                return true;
            }
            if (revision.ActiveAreaEpoch > current.ActiveAreaEpoch)
            {
                return false;
            }
            if (!string.Equals(
                    revision.ScopeStableId,
                    current.ScopeStableId,
                    StringComparison.Ordinal))
            {
                return true;
            }
            if (revision.ScopeGeneration < current.ScopeGeneration ||
                revision.NavCommitSequence < current.NavCommitSequence)
            {
                return true;
            }
            if (revision.ScopeGeneration == current.ScopeGeneration)
            {
                if (string.Equals(
                        revision.NavigationSignature,
                        current.NavigationSignature,
                        StringComparison.Ordinal))
                {
                    return true;
                }
                throw new InvalidOperationException(
                    "Navigation scope generation conflicts with committed content.");
            }
            if (revision.NavCommitSequence == current.NavCommitSequence)
            {
                throw new InvalidOperationException(
                    "Navigation commit sequence was reused for a new scope generation.");
            }
            return false;
        }

        private static ulong NextNonZero(ulong value)
        {
            unchecked
            {
                value++;
            }
            return value == 0UL ? 1UL : value;
        }
    }
}
