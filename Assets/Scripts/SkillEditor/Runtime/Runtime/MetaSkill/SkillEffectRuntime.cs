using System;
using System.Collections.Generic;
using ActionEditor.TagSystem;
using SkillEditor.Preview;

namespace AsiSkillEditor.RunTime
{
    public sealed class SkillEffectRuntime : ISkillEffectExecutor
    {
        private const string ActionTagSourceId = "SkillEffectRuntime.ActionTags";
        private static readonly Dictionary<SkillEffectConfig, LoadedSkillEffectGraph> s_graphCache = new Dictionary<SkillEffectConfig, LoadedSkillEffectGraph>();

        public SkillEffectResult Execute(SkillEffectConfig config, SkillContext context)
        {
            if (config == null || context == null || string.IsNullOrEmpty(config.RootNodeId) || config.Nodes == null || config.Nodes.Count == 0)
            {
                return SkillEffectResult.None;
            }

            LoadedSkillEffectGraph graph = LoadGraph(config);
            SkillEffectResult previousResult = GetLastEffectContext(context);
            SkillEffectResult result = new SkillEffectResult
            {
                SkillRuntimeId = context.SkillFlowContext != null ? context.SkillFlowContext.SkillRuntimeId : string.Empty,
                SkillId = context.SkillConfig != null ? context.SkillConfig.SkillId : string.Empty,
                MetaSkillId = context.CurrentMetaSkillConfig != null ? context.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                MetaSkillNodeId = context.CurrentMetaSkillContext != null ? context.CurrentMetaSkillContext.MetaSkillNodeId : string.Empty,
                EffectId = config.EffectId ?? string.Empty,
                Caster = context.Caster,
                PrimaryTarget = context.PrimaryTarget,
                HasValue = true,
                Succeeded = true,
            };

            result = graph.Root.Execute(context, previousResult, result);
            if (context.CurrentMetaSkillContext != null)
            {
                context.CurrentMetaSkillContext.CurrentEffectContext = result;
                context.CurrentMetaSkillContext.LastEffectContext = result;
                context.CurrentMetaSkillContext.HasExecuted |= result.HasExecuted;
                context.CurrentMetaSkillContext.Succeeded &= result.Succeeded;
                MergeAffectedTargets(context.CurrentMetaSkillContext.AffectedTargets, result.AffectedTargets);
                context.CurrentMetaSkillContext.DataContext.Merge(result.DataContext);
            }

            MergeAffectedTargets(context.AffectedTargets, result.AffectedTargets);
            context.DataContext.Merge(result.DataContext);
            return result;
        }

        private static SkillEffectResult GetLastEffectContext(SkillContext context)
        {
            if (context == null)
            {
                return SkillEffectResult.None;
            }

            if (context.CurrentMetaSkillContext != null && context.CurrentMetaSkillContext.LastEffectContext != null)
            {
                return context.CurrentMetaSkillContext.LastEffectContext;
            }

            if (context.LastMetaSkillContext != null && context.LastMetaSkillContext.LastEffectContext != null)
            {
                return context.LastMetaSkillContext.LastEffectContext;
            }

            return SkillEffectResult.None;
        }

        public void Preload(SkillEffectConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.RootNodeId) || config.Nodes == null || config.Nodes.Count == 0)
            {
                return;
            }

            LoadGraph(config);
        }

        private static LoadedSkillEffectGraph LoadGraph(SkillEffectConfig config)
        {
            if (!s_graphCache.TryGetValue(config, out LoadedSkillEffectGraph graph) || graph == null)
            {
                graph = BuildGraph(config);
                s_graphCache[config] = graph;
            }

            return graph;
        }

        private static LoadedSkillEffectGraph BuildGraph(SkillEffectConfig config)
        {
            Dictionary<string, SkillEffectNodeConfig> nodeLookup = BuildLookup(config.Nodes);
            LoadedSkillEffectNode root = BuildNode(config.RootNodeId, nodeLookup);
            return new LoadedSkillEffectGraph(config, root);
        }

        private static Dictionary<string, SkillEffectNodeConfig> BuildLookup(List<SkillEffectNodeConfig> nodes)
        {
            Dictionary<string, SkillEffectNodeConfig> lookup = new Dictionary<string, SkillEffectNodeConfig>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                SkillEffectNodeConfig node = nodes[i];
                if (node != null && !string.IsNullOrEmpty(node.NodeId))
                {
                    lookup[node.NodeId] = node;
                }
            }

            return lookup;
        }

        private static LoadedSkillEffectNode BuildNode(string nodeId, Dictionary<string, SkillEffectNodeConfig> nodeLookup)
        {
            if (!nodeLookup.TryGetValue(nodeId, out SkillEffectNodeConfig node) || node == null)
            {
                return new InvalidLoadedSkillEffectNode(nodeId ?? string.Empty);
            }

            switch (node.NodeType)
            {
                case SkillEffectNodeType.Sequence:
                    return new LoadedSequenceSkillEffectNode(node, BuildChildren(node, nodeLookup));

                case SkillEffectNodeType.Condition:
                    return new LoadedConditionSkillEffectNode(node, BuildConditionChildren(node, nodeLookup));

                case SkillEffectNodeType.Action:
                    return new LoadedActionSkillEffectNode(node, node.Action != null && node.Action.Data != null ? SkillActionRuntimeFactory.CreateReusable(node.Action) : null);

                default:
                    return new InvalidLoadedSkillEffectNode(node.NodeId ?? string.Empty);
            }
        }

        private static List<LoadedSkillEffectNode> BuildChildren(SkillEffectNodeConfig node, Dictionary<string, SkillEffectNodeConfig> nodeLookup)
        {
            List<LoadedSkillEffectNode> children = new List<LoadedSkillEffectNode>();
            if (node == null || node.Children == null)
            {
                return children;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                children.Add(BuildNode(node.Children[i], nodeLookup));
            }

            return children;
        }

        private static LoadedConditionChildren BuildConditionChildren(SkillEffectNodeConfig node, Dictionary<string, SkillEffectNodeConfig> nodeLookup)
        {
            LoadedSkillEffectNode passedNode = node != null && node.Children != null && node.Children.Count > 0
                ? BuildNode(node.Children[0], nodeLookup)
                : null;
            LoadedSkillEffectNode failedNode = node != null && node.Children != null && node.Children.Count > 1
                ? BuildNode(node.Children[1], nodeLookup)
                : null;
            SkillConditionRuntimeBase runtime = node != null && node.Condition != null && node.Condition.Data != null
                ? SkillConditionRuntimeFactory.CreateReusable(node.Condition)
                : null;
            return new LoadedConditionChildren(runtime, passedNode, failedNode);
        }

        private abstract class LoadedSkillEffectNode
        {
            protected LoadedSkillEffectNode(SkillEffectNodeConfig node)
            {
                Node = node;
            }

            protected SkillEffectNodeConfig Node { get; }

            public SkillEffectResult Execute(SkillContext context, SkillEffectResult lastResult, SkillEffectResult effectResult)
            {
                if (Node == null)
                {
                    return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
                }

                EmitTrace(context, Node, "EffectNodeEnter", "Effect node entered.");
                try
                {
                    return ExecuteCore(context, lastResult, effectResult);
                }
                catch (Exception exception)
                {
                    SkillEffectResult failedResult = SkillEffectResult.Fail(SkillEffectFailureKind.ExecutionException);
                    EmitTrace(context, Node, "EffectNodeExit", exception.Message);
                    return failedResult;
                }
            }

            protected abstract SkillEffectResult ExecuteCore(SkillContext context, SkillEffectResult lastResult, SkillEffectResult effectResult);
        }

        private sealed class InvalidLoadedSkillEffectNode : LoadedSkillEffectNode
        {
            public InvalidLoadedSkillEffectNode(string nodeId)
                : base(new SkillEffectNodeConfig
                {
                    NodeId = nodeId,
                    NodeType = SkillEffectNodeType.Action,
                })
            {
            }

            protected override SkillEffectResult ExecuteCore(SkillContext context, SkillEffectResult lastResult, SkillEffectResult effectResult)
            {
                EmitTrace(context, Node, "EffectNodeExit", SkillEffectFailureKind.InvalidData.ToString());
                return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
            }
        }

        private sealed class LoadedSequenceSkillEffectNode : LoadedSkillEffectNode
        {
            private readonly List<LoadedSkillEffectNode> _children;

            public LoadedSequenceSkillEffectNode(SkillEffectNodeConfig node, List<LoadedSkillEffectNode> children)
                : base(node)
            {
                _children = children ?? new List<LoadedSkillEffectNode>();
            }

            protected override SkillEffectResult ExecuteCore(SkillContext context, SkillEffectResult lastResult, SkillEffectResult effectResult)
            {
                if (_children.Count == 0)
                {
                    EmitTrace(context, Node, "EffectNodeExit", "Empty sequence.");
                    return effectResult;
                }

                SkillEffectResult currentResult = lastResult;
                for (int i = 0; i < _children.Count; i++)
                {
                    currentResult = _children[i].Execute(context, currentResult, effectResult);
                    if (!currentResult.Succeeded)
                    {
                        EmitTrace(context, Node, "EffectNodeExit", currentResult.FailureKind.ToString());
                        return currentResult;
                    }
                }

                EmitTrace(context, Node, "EffectNodeExit", string.Empty);
                return effectResult;
            }
        }

        private sealed class LoadedConditionChildren
        {
            public LoadedConditionChildren(SkillConditionRuntimeBase runtime, LoadedSkillEffectNode passedNode, LoadedSkillEffectNode failedNode)
            {
                Runtime = runtime;
                PassedNode = passedNode;
                FailedNode = failedNode;
            }

            public SkillConditionRuntimeBase Runtime { get; }

            public LoadedSkillEffectNode PassedNode { get; }

            public LoadedSkillEffectNode FailedNode { get; }
        }

        private sealed class LoadedConditionSkillEffectNode : LoadedSkillEffectNode
        {
            private readonly LoadedConditionChildren _children;

            public LoadedConditionSkillEffectNode(SkillEffectNodeConfig node, LoadedConditionChildren children)
                : base(node)
            {
                _children = children;
            }

            protected override SkillEffectResult ExecuteCore(SkillContext context, SkillEffectResult lastResult, SkillEffectResult effectResult)
            {
                bool passed = _children != null && _children.Runtime != null && _children.Runtime.EvaluateWithContext(context, lastResult);
                if (passed)
                {
                    SkillEffectResult passedResult = _children != null && _children.PassedNode != null
                        ? _children.PassedNode.Execute(context, lastResult, effectResult)
                        : effectResult;
                    EmitTrace(context, Node, "EffectNodeExit", string.Empty);
                    return passedResult;
                }

                SkillEffectResult failedResult = _children != null && _children.FailedNode != null
                    ? _children.FailedNode.Execute(context, lastResult, effectResult)
                    : SkillEffectResult.Fail(SkillEffectFailureKind.ConditionFailed);
                EmitTrace(context, Node, "EffectNodeExit", failedResult.Succeeded ? string.Empty : failedResult.FailureKind.ToString());
                return failedResult;
            }
        }

        private sealed class LoadedActionSkillEffectNode : LoadedSkillEffectNode
        {
            private readonly SkillActionRuntimeBase _runtime;

            public LoadedActionSkillEffectNode(SkillEffectNodeConfig node, SkillActionRuntimeBase runtime)
                : base(node)
            {
                _runtime = runtime;
            }

            protected override SkillEffectResult ExecuteCore(SkillContext context, SkillEffectResult lastResult, SkillEffectResult effectResult)
            {
                if (Node == null || Node.Action == null || Node.Action.Data == null || _runtime == null)
                {
                    EmitTrace(context, Node, "EffectNodeExit", SkillEffectFailureKind.InvalidData.ToString());
                    return SkillEffectResult.Fail(SkillEffectFailureKind.InvalidData);
                }

                ApplyTags(context.TagQueryService, Node.Action, Node.Action.Tags, ActionTagSourceId);
                try
                {
                    SkillEffectResult actionResult = _runtime.ExecuteWithContext(context, lastResult) ?? SkillEffectResult.None;
                    effectResult.SourceNodeId = Node.NodeId ?? string.Empty;
                    effectResult.CurrentActionContext = actionResult.LastActionContext;
                    effectResult.Merge(actionResult);
                    EmitTrace(context, Node, "EffectNodeExit", actionResult.Succeeded ? string.Empty : actionResult.FailureKind.ToString());
                    return actionResult.Succeeded ? effectResult : actionResult;
                }
                finally
                {
                    RemoveTags(context.TagQueryService, Node.Action, Node.Action.Tags, ActionTagSourceId);
                }
            }
        }

        private sealed class LoadedSkillEffectGraph
        {
            public LoadedSkillEffectGraph(SkillEffectConfig config, LoadedSkillEffectNode root)
            {
                Config = config;
                Root = root;
            }

            public SkillEffectConfig Config { get; }

            public LoadedSkillEffectNode Root { get; }
        }

        private static void ApplyTags(ITagQueryService tagQueryService, object carrier, TagContainer container, string sourceId)
        {
            if (carrier == null || container == null || container.Tags == null)
            {
                return;
            }

            RuntimeTagContainer runtimeTags = ResolveCarrierTagContainer(carrier);
            if (runtimeTags == null)
            {
                return;
            }

            for (int i = 0; i < container.Tags.Count; i++)
            {
                string tag = container.Tags[i];
                if (!string.IsNullOrEmpty(tag))
                {
                    runtimeTags.AddTag(tag, 1, sourceId);
                }
            }
        }

        private static void RemoveTags(ITagQueryService tagQueryService, object carrier, TagContainer container, string sourceId)
        {
            if (carrier == null || container == null || container.Tags == null)
            {
                return;
            }

            RuntimeTagContainer runtimeTags = ResolveCarrierTagContainer(carrier);
            if (runtimeTags == null)
            {
                return;
            }

            for (int i = 0; i < container.Tags.Count; i++)
            {
                string tag = container.Tags[i];
                if (!string.IsNullOrEmpty(tag))
                {
                    runtimeTags.RemoveTag(tag, 1, sourceId);
                }
            }
        }

        private static RuntimeTagContainer ResolveCarrierTagContainer(object carrier)
        {
            switch (carrier)
            {
                case RuntimeTagContainer runtimeTagContainer:
                    return runtimeTagContainer;
                case IRuntimeTagContainerOwner runtimeTagOwner:
                    return runtimeTagOwner.RuntimeTags;
                default:
                    return null;
            }
        }

        private static void EmitTrace(SkillContext context, SkillEffectNodeConfig node, string traceType, string message)
        {
            if (context == null || node == null)
            {
                return;
            }

            context.DebugLastEffectNodeId = node.NodeId;
            SkillRuntimeDebugBus.PublishTrace(context, new SkillRuntimeTraceEvent
            {
                TraceType = traceType,
                NodeId = node.NodeId,
                MetaSkillId = context.CurrentMetaSkillConfig != null ? context.CurrentMetaSkillConfig.MetaSkillId : string.Empty,
                Time = context.DebugTimelineTime,
                Message = message,
            });
        }

        private static void MergeAffectedTargets(List<GameUnit> targetList, List<GameUnit> source)
        {
            if (targetList == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                GameUnit unit = source[i];
                if (unit == null || targetList.Contains(unit))
                {
                    continue;
                }

                targetList.Add(unit);
            }
        }
    }
}
