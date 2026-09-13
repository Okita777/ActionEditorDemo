using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.AI;

namespace AsiTimeLine.RunTime
{
    /// <summary> NavMesh静态逻辑：路径计算与目标点可通行性检测 </summary>
    public class Ex_NavMesh : StaticActionLogics
    {
        private NavMeshPath mNavMeshPath = new NavMeshPath();  // 路径计算结果复用，避免GC
        private NavMeshHit NavMeshHit = new NavMeshHit();      // SamplePosition结果缓存(无out重载用)
        private static readonly float[] CornerSearchRingScales = { 0.4f, 0.8f, 1.2f, 1.6f, 2.0f };
        private const float DefaultTargetBackoffDistance = 0.2f;
        private const float BackoffRatio = 0.35f;
        private const float MinSegmentLength = 0.05f;
        private const float SampleDistance = 0.2f;
        private const float DefaultAgentRadius = 0.35f;
        private const float CandidateSampleRadiusScale = 0.45f;
        private const float MinCandidateSampleRadius = 0.12f;
        private const float ClearanceThresholdRatio = 0.35f;
        private const float LaneOffsetRatio = 0.5f;
        private const float RelaxedLaneOffsetRatio = 0.35f;
        private const float MinLaneOffset = 0.05f;
        private const float ShortTurnSegmentFactor = 3.0f;
        private const int CornerSearchDirectionCount = 16;
        private const float CornerScoreRawDistanceWeight = 2.5f;
        private const float CornerScoreClearanceWeight = 0.15f;
        private const float CornerScoreEscapeWeight = 1.35f;
        private const float CornerScoreNextWeight = 0.85f;
        private const float EscapeProbeLateralScale = 2.35f;
        private const float EscapeProbeForwardScale = 1.1f;
        private const float EscapeSelfSampleRadius = 1.0f;
        private const float DebugDrawLife = 2.0f;
        private const float DirectReachSkipClearance = 0.5f;
        private const float MidpointClearanceCheckRatio = 0.1f;
        private const int SegmentClearanceSamples = 3;
        private byte PathfindInterrupt​Type = 0;
        public void SetPathfindInterrupt​Type(GEnum gEnum)
        {
            if(gEnum.mSerValue!= PathfindInterrupt​Type)
            {
                PathfindInterrupt​Type = gEnum.mSerValue;
            }
        }
        /// <summary> 计算起点到终点的NavMesh路径，逆序写入_gGroupPoint(终点→起点) </summary>
        public bool SetPath(
            GGroupPoint _gGroupPoint,
            PointData _startPos,
            PointData _targetPos,
            int areaMask = NavMesh.AllAreas,
            float targetBackoffDistance = DefaultTargetBackoffDistance)
        {
            List<PointData> _pointData = _gGroupPoint.GetValue(StateMachine.FirstStatePart);
            _pointData.Clear();

            Vector3 effectiveStart = _startPos.pos;
            if (!NavigationQueryApiRuntime.CalculatePath(effectiveStart, _targetPos.pos, areaMask, mNavMeshPath))
            {
                if (NavigationQueryApiRuntime.SamplePosition(effectiveStart, out NavMeshHit startSnap, 1.0f, areaMask))
                {
                    effectiveStart = startSnap.position;
                    if (!NavigationQueryApiRuntime.CalculatePath(effectiveStart, _targetPos.pos, areaMask, mNavMeshPath))
                    {
                        EngineDebug.Log($"[PathCorner] SetPath CalculatePath failed unit={GetUnitDebugName()} start={_startPos.pos} target={_targetPos.pos}");
                        return false;
                    }
                }
                else
                {
                    EngineDebug.Log($"[PathCorner] SetPath CalculatePath failed unit={GetUnitDebugName()} start={_startPos.pos} target={_targetPos.pos}");
                    return false;
                }
            }
            if (mNavMeshPath.status == NavMeshPathStatus.PathInvalid)
            {
                EngineDebug.Log($"[PathCorner] SetPath PathInvalid unit={GetUnitDebugName()} start={_startPos.pos} target={_targetPos.pos}");
                return false;
            }

            Vector3[] pathPoints = EngineResourcesManager.Instance.Arr_Vector3;
            int pathGroupConst = mNavMeshPath.GetCornersNonAlloc(pathPoints);
            if (pathGroupConst < 2)
            {
                EngineDebug.Log($"[PathCorner] SetPath no corners unit={GetUnitDebugName()} cornerCount={pathGroupConst} start={_startPos.pos} target={_targetPos.pos}");
                return false;
            }

            float agentRadius = GetPathAgentRadius();
            float _safeOffset = Mathf.Max(targetBackoffDistance, 0f);
            Vector3 _targetPosSafe = GetSafeTargetPos(pathPoints, pathGroupConst, _targetPos.pos, _safeOffset, areaMask);
            _pointData.Add(new PointData(_targetPosSafe, _targetPos.rot));
            Vector3 _lastPos = _targetPosSafe;
            for (int i = pathGroupConst - 2; i > 0; i--)
            {
                Vector3 cornerPos = ResolveCornerPosition(pathPoints[i - 1], pathPoints[i], _lastPos, agentRadius, areaMask, i,
                    pathGroupConst - 2);
                Vector3 _dir = _lastPos - cornerPos;
                if (_dir.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                Quaternion _rot = Quaternion.LookRotation(_dir);
                _lastPos = cornerPos;
                _pointData.Add(new PointData(cornerPos, _rot));
            }

            ValidateAdjacentSegments(_pointData, agentRadius, areaMask);
            ValidateStartToFirstWaypoint(_pointData, effectiveStart, agentRadius, areaMask);
            LogFinalPath(_pointData, effectiveStart);
            return true;
        }

        private void ValidateAdjacentSegments(List<PointData> pointData, float agentRadius, int areaMask)
        {
            if (pointData.Count < 2)
                return;

            int maxPasses = 3;
            for (int pass = 0; pass < maxPasses; pass++)
            {
                bool anyFixed = false;
                for (int i = pointData.Count - 1; i > 0; i--)
                {
                    Vector3 from = pointData[i].pos;
                    Vector3 to = pointData[i - 1].pos;
                    if (!IsSegmentDirectlyReachable(from, to, agentRadius, areaMask))
                    {
                        EngineDebug.Log(
                            $"[PathCorner] adjacent-segment-blocked unit={GetUnitDebugName()} pass={pass} fromIdx={i} toIdx={i - 1} from={from} to={to} radius={agentRadius:F2}");
                        if (TryFindIntermediatePoint(from, to, agentRadius, areaMask, out Vector3 midPoint))
                        {
                            Vector3 dir = to - midPoint;
                            Quaternion rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : pointData[i - 1].rot;
                            pointData.Insert(i, new PointData(midPoint, rot));
                            EngineDebug.Log(
                                $"[PathCorner] inserted-midpoint unit={GetUnitDebugName()} at={midPoint} between={from} and={to}");
                            anyFixed = true;
                        }
                    }
                }
                if (!anyFixed)
                    break;
            }
        }

        private bool TryFindIntermediatePoint(Vector3 from, Vector3 to, float agentRadius, int areaMask, out Vector3 midPoint)
        {
            Vector3 center = (from + to) * 0.5f;
            Vector3 flatDir = to - from;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude < MinSegmentLength * MinSegmentLength)
            {
                midPoint = center;
                return false;
            }

            Vector3 lateral = Vector3.Cross(Vector3.up, flatDir.normalized);
            float sampleRadius = Mathf.Max(agentRadius * CandidateSampleRadiusScale, MinCandidateSampleRadius);

            float bestScore = float.MinValue;
            bool found = false;
            midPoint = center;

            Vector3[] probePositions = { center, Vector3.Lerp(from, to, 0.33f), Vector3.Lerp(from, to, 0.67f) };
            float[] lateralOffsets = { 0.8f, -0.8f, 1.2f, -1.2f, 1.6f, -1.6f, 2.0f, -2.0f };

            for (int p = 0; p < probePositions.Length; p++)
            {
                for (int i = 0; i < lateralOffsets.Length; i++)
                {
                    Vector3 candidate = probePositions[p] + lateral * (agentRadius * lateralOffsets[i]);
                    if (!TrySampleNavPosition(candidate, sampleRadius, areaMask, out Vector3 sampled))
                        continue;

                    if (!IsSegmentDirectlyReachable(from, sampled, agentRadius, areaMask))
                        continue;
                    if (!IsSegmentDirectlyReachable(sampled, to, agentRadius, areaMask))
                        continue;

                    if (!TryGetEdgeClearance(sampled, areaMask, out float clearance))
                        continue;

                    float toTargetGain = Vector3.Dot(sampled - from, (to - from).normalized);
                    float score = clearance * 2.0f + toTargetGain * 1.0f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        midPoint = sampled;
                        found = true;
                    }
                }
            }
            return found;
        }

        private void LogFinalPath(List<PointData> pointData, Vector3 selfPos)
        {
            if (pointData.Count < 1)
                return;

            string pathStr = $"self={selfPos}";
            for (int i = pointData.Count - 1; i >= 0; i--)
            {
                pathStr += $" -> wp{pointData.Count - 1 - i}={pointData[i].pos}";
            }
            EngineDebug.Log($"[PathCorner] final-path unit={GetUnitDebugName()} count={pointData.Count} {pathStr}");
        }

        private void ValidateStartToFirstWaypoint(List<PointData> pointData, Vector3 selfPos, float agentRadius, int areaMask)
        {
            if (pointData.Count < 1)
                return;

            int lastIdx = pointData.Count - 1;
            Vector3 firstWp = pointData[lastIdx].pos;

            if (IsSegmentDirectlyReachable(selfPos, firstWp, agentRadius, areaMask))
            {
                EngineDebug.Log($"[PathCorner] self-to-wp0 OK unit={GetUnitDebugName()} self={selfPos} wp0={firstWp}");
                return;
            }

            EngineDebug.Log($"[PathCorner] self-to-wp0 BLOCKED unit={GetUnitDebugName()} self={selfPos} wp0={firstWp}");

            if (TryFindIntermediatePoint(selfPos, firstWp, agentRadius, areaMask, out Vector3 midPoint))
            {
                Vector3 dir = firstWp - midPoint;
                Quaternion rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : pointData[lastIdx].rot;
                pointData.Add(new PointData(midPoint, rot));
                EngineDebug.Log(
                    $"[PathCorner] self-to-wp0 inserted midpoint unit={GetUnitDebugName()} mid={midPoint} self={selfPos} wp0={firstWp}");
                return;
            }

            for (int i = lastIdx - 1; i >= 0; i--)
            {
                if (IsSegmentDirectlyReachable(selfPos, pointData[i].pos, agentRadius, areaMask))
                {
                    int removeCount = lastIdx - i;
                    EngineDebug.Log(
                        $"[PathCorner] self-to-wp0 skip to reachable unit={GetUnitDebugName()} removed={removeCount} selfPos={selfPos} skippedTo={pointData[i].pos}");
                    pointData.RemoveRange(i + 1, removeCount);
                    return;
                }
            }

            EngineDebug.Log($"[PathCorner] self-to-wp0 no reachable wp found unit={GetUnitDebugName()} self={selfPos}");
        }

        private bool IsSegmentDirectlyReachable(Vector3 fromPos, Vector3 toPos, float agentRadius, int areaMask)
        {
            Vector3 flatOffset = toPos - fromPos;
            flatOffset.y = 0f;
            if (flatOffset.sqrMagnitude < MinSegmentLength * MinSegmentLength)
                return true;

            if (NavigationQueryApiRuntime.Raycast(fromPos, toPos, out NavMeshHit _, areaMask))
                return false;

            Vector3 laneNormal = Vector3.Cross(Vector3.up, flatOffset.normalized);
            float laneOffset = Mathf.Max(agentRadius * LaneOffsetRatio, MinLaneOffset);
            float sampleRadius = Mathf.Max(agentRadius * CandidateSampleRadiusScale, MinCandidateSampleRadius);

            Vector3 fromLeft = fromPos + laneNormal * laneOffset;
            Vector3 toLeft = toPos + laneNormal * laneOffset;
            Vector3 fromRight = fromPos - laneNormal * laneOffset;
            Vector3 toRight = toPos - laneNormal * laneOffset;

            bool leftOk = TrySampleNavPosition(fromLeft, sampleRadius, areaMask, out Vector3 slFrom) &&
                           TrySampleNavPosition(toLeft, sampleRadius, areaMask, out Vector3 slTo) &&
                           !NavigationQueryApiRuntime.Raycast(slFrom, slTo, out NavMeshHit _, areaMask);

            bool rightOk = TrySampleNavPosition(fromRight, sampleRadius, areaMask, out Vector3 srFrom) &&
                            TrySampleNavPosition(toRight, sampleRadius, areaMask, out Vector3 srTo) &&
                            !NavigationQueryApiRuntime.Raycast(srFrom, srTo, out NavMeshHit _, areaMask);

            if (!leftOk && !rightOk)
                return false;

            float minClearance = Mathf.Max(agentRadius * MidpointClearanceCheckRatio, MinLaneOffset);
            for (int s = 1; s <= SegmentClearanceSamples; s++)
            {
                float t = s / (float)(SegmentClearanceSamples + 1);
                Vector3 midPoint = Vector3.Lerp(fromPos, toPos, t);
                if (TryGetEdgeClearance(midPoint, areaMask, out float clearance) && clearance < minClearance)
                    return false;
            }

            return true;
        }

        private Vector3 ResolveCornerPosition(
            Vector3 prevPos,
            Vector3 rawCornerPos,
            Vector3 nextPos,
            float agentRadius,
            int areaMask,
            int cornerIndex,
            int totalCornerCount)
        {
            bool useRelaxedTurnCheck = ShouldUseRelaxedTurnCheck(prevPos, rawCornerPos, nextPos, agentRadius, totalCornerCount);
            if (IsCornerWalkableWithRadius(prevPos, rawCornerPos, nextPos, agentRadius, areaMask, useRelaxedTurnCheck,
                    out float rawClearance, out string rawReason))
            {
                return rawCornerPos;
            }

            int sampleMissCount = 0;
            int clearanceFailCount = 0;
            int segmentFailCount = 0;
            Dictionary<string, int> failReasonCounter = new Dictionary<string, int>(8);
            bool hasCandidate = false;
            Vector3 bestCandidate = rawCornerPos;
            float bestScore = float.MaxValue;
            float bestClearance = 0f;
            Vector3 preferredDir = GetPreferredSearchDirection(prevPos, rawCornerPos, nextPos);
            float sampleRadius = Mathf.Max(agentRadius * CandidateSampleRadiusScale, MinCandidateSampleRadius);

            for (int ringIndex = 0; ringIndex < CornerSearchRingScales.Length; ringIndex++)
            {
                float searchDistance = Mathf.Max(agentRadius * CornerSearchRingScales[ringIndex], MinLaneOffset);
                for (int dirIndex = 0; dirIndex < CornerSearchDirectionCount; dirIndex++)
                {
                    Vector3 searchDir = GetSearchDirection(preferredDir, dirIndex);
                    Vector3 candidateRawPos = rawCornerPos + searchDir * searchDistance;
                    if (!TrySampleNavPosition(candidateRawPos, sampleRadius, areaMask, out Vector3 candidatePos))
                    {
                        sampleMissCount++;
                        continue;
                    }

                    if (!IsCornerWalkableWithRadius(prevPos, candidatePos, nextPos, agentRadius, areaMask, useRelaxedTurnCheck,
                            out float candidateClearance, out string candidateReason))
                    {
                        AddFailReason(failReasonCounter, candidateReason);
                        if (candidateReason.StartsWith("clearance"))
                        {
                            clearanceFailCount++;
                        }
                        else
                        {
                            segmentFailCount++;
                        }
                        continue;
                    }

                    float score = EvaluateCornerCandidateScore(rawCornerPos, candidatePos, nextPos, preferredDir, candidateClearance);
                    if (score < bestScore)
                    {
                        hasCandidate = true;
                        bestCandidate = candidatePos;
                        bestScore = score;
                        bestClearance = candidateClearance;
                    }
                }
            }

            string failReasonSummary = BuildFailReasonSummary(failReasonCounter);
            if (hasCandidate)
            {
                EngineDebug.Log(
                    $"[PathCorner] resolve success unit={GetUnitDebugName()} idx={cornerIndex}/{totalCornerCount} mode={(useRelaxedTurnCheck ? "relaxed" : "strict")} raw={rawCornerPos} resolved={bestCandidate} radius={agentRadius:F2} rawClearance={rawClearance:F2} newClearance={bestClearance:F2} rawReason={rawReason} sampleMiss={sampleMissCount} clearanceFail={clearanceFailCount} segmentFail={segmentFailCount} failReasons={failReasonSummary}");
#if UNITY_EDITOR
                EngineDebug.DrawSphere(rawCornerPos, agentRadius, Color.red, DebugDrawLife);
                EngineDebug.DrawSphere(bestCandidate, agentRadius, Color.green, DebugDrawLife);
                EngineDebug.DrawLine(prevPos, bestCandidate, Color.green, DebugDrawLife);
                EngineDebug.DrawLine(bestCandidate, nextPos, Color.green, DebugDrawLife);
#endif
                return bestCandidate;
            }

            EngineDebug.Log(
                $"[PathCorner] resolve failed unit={GetUnitDebugName()} idx={cornerIndex}/{totalCornerCount} mode={(useRelaxedTurnCheck ? "relaxed" : "strict")} raw={rawCornerPos} prev={prevPos} next={nextPos} radius={agentRadius:F2} rawClearance={rawClearance:F2} rawReason={rawReason} sampleMiss={sampleMissCount} clearanceFail={clearanceFailCount} segmentFail={segmentFailCount} failReasons={failReasonSummary}");
#if UNITY_EDITOR
            EngineDebug.DrawSphere(rawCornerPos, agentRadius, Color.yellow, DebugDrawLife);
            EngineDebug.DrawLine(prevPos, rawCornerPos, Color.yellow, DebugDrawLife);
            EngineDebug.DrawLine(rawCornerPos, nextPos, Color.yellow, DebugDrawLife);
#endif
            return rawCornerPos;
        }

        public bool IsSegmentReachableForAgent(Vector3 fromPos, Vector3 toPos, int areaMask)
        {
            return IsSegmentDirectlyReachable(fromPos, toPos, GetPathAgentRadius(), areaMask);
        }

        public float GetAgentRadius() => GetPathAgentRadius();

        public bool TryGetAggressiveEscapePoint(Vector3 selfPos, Vector3 blockedWp, Vector3 targetPos, int areaMask, out Vector3 escapePos)
        {
            float agentRadius = GetPathAgentRadius();
            float sampleRadius = Mathf.Max(agentRadius * CandidateSampleRadiusScale, MinCandidateSampleRadius);
            float lateralDistance = Mathf.Max(agentRadius * EscapeProbeLateralScale, 0.8f);
            float forwardDistance = Mathf.Max(agentRadius * EscapeProbeForwardScale, 0.35f);
            float minEscapeClearance = Mathf.Max(agentRadius * MidpointClearanceCheckRatio, MinLaneOffset);

            Vector3 toTarget = targetPos - selfPos;
            toTarget.y = 0f;
            Vector3 toBlocked = blockedWp - selfPos;
            toBlocked.y = 0f;

            Vector3 targetDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized :
                toBlocked.sqrMagnitude > 0.0001f ? toBlocked.normalized : Vector3.forward;
            Vector3 forwardDir = toBlocked.sqrMagnitude > 0.0001f ? toBlocked.normalized : targetDir;
            Vector3 lateralDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

            if (!TrySampleNavPosition(selfPos, EscapeSelfSampleRadius, areaMask, out Vector3 selfNavPos))
            {
                escapePos = selfPos;
                return false;
            }

            Vector3[] candidateOffsets =
            {
                targetDir * lateralDistance,
                targetDir * lateralDistance * 1.5f,
                lateralDir * lateralDistance + targetDir * forwardDistance,
                -lateralDir * lateralDistance + targetDir * forwardDistance,
                lateralDir * lateralDistance * 0.7f + targetDir * lateralDistance,
                -lateralDir * lateralDistance * 0.7f + targetDir * lateralDistance,
                lateralDir * lateralDistance,
                -lateralDir * lateralDistance,
                lateralDir * lateralDistance * 1.35f + targetDir * forwardDistance * 0.5f,
                -lateralDir * lateralDistance * 1.35f + targetDir * forwardDistance * 0.5f,
            };

            bool hasCandidate = false;
            float bestScore = float.MinValue;
            Vector3 bestCandidate = selfNavPos;
            for (int i = 0; i < candidateOffsets.Length; i++)
            {
                Vector3 candidateRawPos = selfNavPos + candidateOffsets[i];
                if (!TrySampleNavPosition(candidateRawPos, sampleRadius, areaMask, out Vector3 candidatePos))
                    continue;

                if (NavigationQueryApiRuntime.Raycast(selfNavPos, candidatePos, out NavMeshHit _, areaMask))
                    continue;

                if (!TryGetEdgeClearance(candidatePos, areaMask, out float clearance))
                    continue;

                if (clearance < minEscapeClearance)
                    continue;

                float targetGain = Vector3.Dot(candidatePos - selfNavPos, targetDir);
                float score = clearance * 1.5f + targetGain * 3.0f;
                if (targetGain < -0.1f)
                    score -= 5.0f;

                if (score > bestScore)
                {
                    hasCandidate = true;
                    bestScore = score;
                    bestCandidate = candidatePos;
                }
            }

            escapePos = bestCandidate;
            return hasCandidate;
        }

        private bool IsCornerWalkableWithRadius(
            Vector3 prevPos,
            Vector3 cornerPos,
            Vector3 nextPos,
            float agentRadius,
            int areaMask,
            bool useRelaxedTurnCheck,
            out float clearance,
            out string reason)
        {
            if (!TryGetEdgeClearance(cornerPos, areaMask, out clearance))
            {
                reason = "clearance-sample-miss";
                return false;
            }

            float clearanceThreshold = Mathf.Max(agentRadius * ClearanceThresholdRatio, MinLaneOffset);
            if (clearance < clearanceThreshold)
            {
                reason = $"clearance-low({clearance:F2}<{clearanceThreshold:F2})";
                return false;
            }

            if (!IsSegmentWalkableWithRadius(prevPos, cornerPos, agentRadius, areaMask, useRelaxedTurnCheck, out string prevReason))
            {
                reason = $"prev-{prevReason}";
                return false;
            }

            if (!IsSegmentWalkableWithRadius(cornerPos, nextPos, agentRadius, areaMask, useRelaxedTurnCheck, out string nextReason))
            {
                reason = $"next-{nextReason}";
                return false;
            }

            reason = "ok";
            return true;
        }

        private bool IsSegmentWalkableWithRadius(
            Vector3 fromPos,
            Vector3 toPos,
            float agentRadius,
            int areaMask,
            bool useRelaxedTurnCheck,
            out string reason)
        {
            Vector3 flatOffset = toPos - fromPos;
            flatOffset.y = 0f;
            if (flatOffset.sqrMagnitude < MinSegmentLength * MinSegmentLength)
            {
                reason = "short";
                return true;
            }

            Vector3 laneNormal = Vector3.Cross(Vector3.up, flatOffset.normalized);
            float laneOffsetRatio = useRelaxedTurnCheck ? RelaxedLaneOffsetRatio : LaneOffsetRatio;
            float laneOffset = Mathf.Min(Mathf.Max(agentRadius * laneOffsetRatio, MinLaneOffset), flatOffset.magnitude * 0.25f);
            float sampleRadius = Mathf.Max(agentRadius * CandidateSampleRadiusScale, MinCandidateSampleRadius);

            if (!TryCheckSegmentLane(fromPos, toPos, Vector3.zero, sampleRadius, areaMask, 0, out string centerReason))
            {
                reason = centerReason;
                return false;
            }

            if (!useRelaxedTurnCheck)
            {
                if (!TryCheckSegmentLane(fromPos, toPos, laneNormal * laneOffset, sampleRadius, areaMask, 1, out string leftReason))
                {
                    reason = leftReason;
                    return false;
                }

                if (!TryCheckSegmentLane(fromPos, toPos, -laneNormal * laneOffset, sampleRadius, areaMask, 2, out string rightReason))
                {
                    reason = rightReason;
                    return false;
                }

                reason = "ok";
                return true;
            }

            bool leftOk = TryCheckSegmentLane(fromPos, toPos, laneNormal * laneOffset, sampleRadius, areaMask, 1, out string leftRelaxReason);
            bool rightOk = TryCheckSegmentLane(fromPos, toPos, -laneNormal * laneOffset, sampleRadius, areaMask, 2, out string rightRelaxReason);
            if (leftOk || rightOk)
            {
                reason = leftOk && rightOk ? "turn-ok-both-side" : leftOk ? "turn-ok-left" : "turn-ok-right";
                return true;
            }

            reason = $"turn-side-blocked({leftRelaxReason}|{rightRelaxReason})";
            return false;
        }

        private bool TryCheckSegmentLane(
            Vector3 fromPos,
            Vector3 toPos,
            Vector3 offset,
            float sampleRadius,
            int areaMask,
            int laneIndex,
            out string reason)
        {
            if (!TrySampleNavPosition(fromPos + offset, sampleRadius, areaMask, out Vector3 laneFrom))
            {
                reason = $"sample-from-lane{laneIndex}";
                return false;
            }

            if (!TrySampleNavPosition(toPos + offset, sampleRadius, areaMask, out Vector3 laneTo))
            {
                reason = $"sample-to-lane{laneIndex}";
                return false;
            }

            if (NavigationQueryApiRuntime.Raycast(laneFrom, laneTo, out NavMeshHit hit, areaMask))
            {
                reason = $"raycast-lane{laneIndex}@{hit.position}";
                return false;
            }

            reason = "ok";
            return true;
        }

        private bool ShouldUseRelaxedTurnCheck(Vector3 prevPos, Vector3 cornerPos, Vector3 nextPos, float agentRadius, int totalCornerCount)
        {
            if (totalCornerCount <= 1)
            {
                return true;
            }

            float prevDistance = Vector3.Distance(prevPos, cornerPos);
            float nextDistance = Vector3.Distance(cornerPos, nextPos);
            float shortThreshold = Mathf.Max(agentRadius * ShortTurnSegmentFactor, agentRadius + MinLaneOffset);
            return prevDistance < shortThreshold || nextDistance < shortThreshold;
        }

        private void AddFailReason(Dictionary<string, int> failReasonCounter, string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return;
            }

            if (failReasonCounter.TryGetValue(reason, out int count))
            {
                failReasonCounter[reason] = count + 1;
                return;
            }

            failReasonCounter.Add(reason, 1);
        }

        private string BuildFailReasonSummary(Dictionary<string, int> failReasonCounter)
        {
            if (failReasonCounter.Count == 0)
            {
                return "none";
            }

            List<KeyValuePair<string, int>> sortedReasons = new List<KeyValuePair<string, int>>(failReasonCounter);
            sortedReasons.Sort((a, b) =>
            {
                int countCompare = b.Value.CompareTo(a.Value);
                return countCompare != 0 ? countCompare : string.CompareOrdinal(a.Key, b.Key);
            });

            int limit = Mathf.Min(4, sortedReasons.Count);
            string summary = string.Empty;
            for (int i = 0; i < limit; i++)
            {
                if (i > 0)
                {
                    summary += ";";
                }
                summary += $"{sortedReasons[i].Key}*{sortedReasons[i].Value}";
            }

            if (sortedReasons.Count > limit)
            {
                summary += $";others={sortedReasons.Count - limit}";
            }

            return summary;
        }

        private float EvaluateCornerCandidateScore(
            Vector3 rawCornerPos,
            Vector3 candidatePos,
            Vector3 nextPos,
            Vector3 preferredDir,
            float candidateClearance)
        {
            Vector3 delta = candidatePos - rawCornerPos;
            delta.y = 0f;

            Vector3 toNext = nextPos - rawCornerPos;
            toNext.y = 0f;
            Vector3 nextDir = toNext.sqrMagnitude > 0.0001f ? toNext.normalized : preferredDir;

            float rawDistancePenalty = delta.sqrMagnitude * CornerScoreRawDistanceWeight;
            float escapeReward = Vector3.Dot(delta, preferredDir) * CornerScoreEscapeWeight;
            float nextReward = Vector3.Dot(delta, nextDir) * CornerScoreNextWeight;
            float clearanceReward = candidateClearance * CornerScoreClearanceWeight;
            return rawDistancePenalty - clearanceReward - escapeReward - nextReward;
        }

        private bool TryGetEdgeClearance(Vector3 pos, int areaMask, out float clearance)
        {
            if (NavigationQueryApiRuntime.FindClosestEdge(pos, out NavMeshHit hit, areaMask))
            {
                clearance = hit.distance;
                return true;
            }

            clearance = 0f;
            return false;
        }

        private bool TrySampleNavPosition(Vector3 rawPos, float sampleRadius, int areaMask, out Vector3 sampledPos)
        {
            if (NavigationQueryApiRuntime.SamplePosition(rawPos, out NavMeshHit hit, sampleRadius, areaMask))
            {
                sampledPos = hit.position;
                return true;
            }

            sampledPos = rawPos;
            return false;
        }

        private Vector3 GetPreferredSearchDirection(Vector3 prevPos, Vector3 cornerPos, Vector3 nextPos)
        {
            Vector3 toPrev = prevPos - cornerPos;
            Vector3 toNext = nextPos - cornerPos;
            toPrev.y = 0f;
            toNext.y = 0f;

            Vector3 preferred = toPrev.normalized + toNext.normalized;
            if (preferred.sqrMagnitude > 0.0001f)
            {
                return preferred.normalized;
            }

            Vector3 fallback = nextPos - prevPos;
            fallback.y = 0f;
            if (fallback.sqrMagnitude > 0.0001f)
            {
                return fallback.normalized;
            }

            return Vector3.forward;
        }

        private Vector3 GetSearchDirection(Vector3 preferredDir, int dirIndex)
        {
            float angle = 360f / CornerSearchDirectionCount * dirIndex;
            Vector3 searchDir = Quaternion.Euler(0f, angle, 0f) * preferredDir;
            searchDir.y = 0f;
            return searchDir.sqrMagnitude > 0.0001f ? searchDir.normalized : Vector3.forward;
        }

        private float GetPathAgentRadius()
        {
            if (StateMachine?.CurUnit == null)
            {
                return DefaultAgentRadius;
            }

            Transform rootTarget = StateMachine.CurUnit.RootTarget;
            if (rootTarget != null && rootTarget.TryGetComponent(out CharacterController rootController))
            {
                return Mathf.Max(rootController.radius + rootController.skinWidth, DefaultAgentRadius);
            }

            if (StateMachine.CurUnit.TryGetComponent(out CharacterController selfController))
            {
                return Mathf.Max(selfController.radius + selfController.skinWidth, DefaultAgentRadius);
            }

            CharacterController childController = StateMachine.CurUnit.GetComponentInChildren<CharacterController>();
            if (childController != null)
            {
                return Mathf.Max(childController.radius + childController.skinWidth, DefaultAgentRadius);
            }

            return DefaultAgentRadius;
        }

        private string GetUnitDebugName()
        {
            return StateMachine?.CurUnit != null ? StateMachine.CurUnit.gameObject.name : "null";
        }

        private Vector3 GetSafeTargetPos(Vector3[] pathPoints, int cornerCount, Vector3 rawTarget, float targetBackoffDistance, int areaMask)
        {
            if (targetBackoffDistance <= 0f || cornerCount < 2)
            {
                return rawTarget;
            }

            Vector3 prev = pathPoints[cornerCount - 2];
            Vector3 toTarget = rawTarget - prev;
            float segmentLen = toTarget.magnitude;
            if (segmentLen < MinSegmentLength)
            {
                return rawTarget;
            }

            float inset = Mathf.Min(targetBackoffDistance, segmentLen * BackoffRatio);
            Vector3 candidate = rawTarget - toTarget / segmentLen * inset;
            if (NavigationQueryApiRuntime.SamplePosition(candidate, out NavMeshHit hit, SampleDistance, areaMask))
            {
                return hit.position;
            }
            return rawTarget;
        }

        /// <summary> 检测目标点是否在NavMesh可采样范围内(无out版本) </summary>
        public bool CheckPointToNavMash(Vector3 _targetPos, float _maxDistance, int areaMask = NavMesh.AllAreas)
        {
            if (NavigationQueryApiRuntime.SamplePosition(_targetPos, out NavMeshHit, _maxDistance, areaMask))
            {
                return true;
            }
            return false;
        }

        /// <summary> 检测目标点是否在NavMesh可采样范围内，成功时返回投影后的位置 </summary>
        public bool CheckPointToNavMash(Vector3 _targetPos, float _maxDistance, out NavMeshHit _navMeshHit, int areaMask = NavMesh.AllAreas)
        {
            if (NavigationQueryApiRuntime.SamplePosition(_targetPos, out _navMeshHit, _maxDistance, areaMask))
            {
                return true;
            }

            //if (NavMesh.GetAreaNames().Length > 0)
            //{
            //    string names = $"找到了<color=ffcc00>{NavMesh.GetAreaNames().Length}</color>个导航网格 MaxDistance[{_maxDistance}]";
            //    for (int i = 0; i < NavMesh.GetAreaNames().Length; i++)
            //    {
            //        string item = NavMesh.GetAreaNames()[i];
            //        names += $"\n导航网格名称: {item} Cost[{NavMesh.GetAreaCost(i)}]";
            //    }
            //    //foreach (var item in NavMesh.GetAreaNames())
            //    //{
            //    //    names += $"\n导航网格名称: {item}";
            //    //}
            //    //NavMesh.
            //    EngineDebug.LogError(names);
            //}
            //else
            //{
            //    EngineDebug.LogError($"未找到任何导航网格");
            //}

            //EngineDebug.LogError($"<color=#ff0000>没有找到寻路网格</color>[{_targetPos}]");
            //EngineDebug.DrawSphere(_targetPos, 2.2f, Color.black, 10);
            return false;  // 目标点超出NavMesh或不可达
        }
    }
}
