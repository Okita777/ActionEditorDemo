using System.Collections.Generic;

namespace AsiSkillEditor.RunTime
{
    internal readonly struct MovementPolicyHandle
    {
        public readonly int Version;
        public readonly string OwnerStateId;

        public MovementPolicyHandle(int version, string ownerStateId)
        {
            Version = version;
            OwnerStateId = ownerStateId ?? string.Empty;
        }

        public bool IsValid => Version > 0;
    }

    /// <summary>
    /// 汇总活动状态提交的运动策略请求，并按层优先级与请求版本选出唯一结果。
    /// 它只负责策略所有权，不负责状态切换或执行实际移动。
    /// </summary>
    internal sealed class CharacterMovementPolicyController
    {
        private sealed class PolicyRequest
        {
            public int Version;
            public string OwnerStateId = string.Empty;
            public int Priority;
            public StateMovementProfile Profile;
        }

        private readonly Dictionary<int, PolicyRequest> _requests = new Dictionary<int, PolicyRequest>();
        private int _nextVersion;

        public StateMovementProfile Current { get; private set; } = StateMovementProfile.CreateDefault();

        public MovementPolicyHandle Submit(string ownerStateId, int priority, StateMovementProfile profile)
        {
            int version = ++_nextVersion;
            string normalizedOwnerStateId = ownerStateId ?? string.Empty;
            _requests[version] = new PolicyRequest
            {
                Version = version,
                OwnerStateId = normalizedOwnerStateId,
                Priority = priority,
                Profile = profile ?? StateMovementProfile.CreateDefault(),
            };
            Resolve();
            return new MovementPolicyHandle(version, normalizedOwnerStateId);
        }

        public bool Release(MovementPolicyHandle handle)
        {
            if (!handle.IsValid || !_requests.TryGetValue(handle.Version, out PolicyRequest request) ||
                request.OwnerStateId != handle.OwnerStateId)
            {
                return false;
            }

            _requests.Remove(handle.Version);
            Resolve();
            return true;
        }

        private void Resolve()
        {
            PolicyRequest winner = null;
            foreach (PolicyRequest request in _requests.Values)
            {
                if (winner == null || request.Priority > winner.Priority ||
                    (request.Priority == winner.Priority && request.Version > winner.Version))
                {
                    winner = request;
                }
            }

            Current = winner != null
                ? winner.Profile
                : StateMovementProfile.CreateDefault();
        }
    }

    internal sealed class StateLayerRuntime
    {
        public StateLayerType LayerType;
        public int Priority;
        public string DefaultStateId = string.Empty;
        public ActiveStateRuntime Current;
        public StateTransitionRequest PendingRequest;
    }

    public sealed class StateSharedControlContext
    {
        public bool AllowMoveInput = true;
        public bool AllowLocomotionDrive = true;
        public bool AllowRotationInput = true;
        public bool AllowDash = true;
        public bool AllowNextSkill = true;
        public bool UseRootMotion = true;
        public bool ForceLocomotionSafeState;
        public bool BlocksLocomotionAnimation;
    }

    internal sealed class StateLayerControlIntent
    {
        public StateLayerType LayerType;
        public string OwnerStateId = string.Empty;
        public bool LocksMoveInput;
        public bool LocksLocomotionDrive;
        public bool LocksRotationInput;
        public bool BlocksLocomotionAnimation;
        public bool ForcesLocomotionSafeState;
    }

    internal sealed class StateGateScope
    {
        public string ScopeId = string.Empty;
        public string OwnerStateId = string.Empty;
        public GateControlType GateType;
        public GateValueMode ValueMode;
        public bool Value;
    }

    internal sealed class StateGateAggregator
    {
        public Dictionary<string, StateGateScope> ActiveScopes = new Dictionary<string, StateGateScope>();
    }
}
