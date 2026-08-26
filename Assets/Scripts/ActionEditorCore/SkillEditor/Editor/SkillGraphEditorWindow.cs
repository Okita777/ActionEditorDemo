using System;
using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor.Editor
{
    public sealed class SkillGraphEditorWindow : EditorWindow
    {
        private const string EntryNodeId = "__entry__";
        private const string ExitNodeId = "__exit__";
        private const float LayerPanelWidth = 180f;
        private static readonly Rect DefaultEntryRect = new Rect(360f, 40f, 220f, 70f);
        private static readonly Rect DefaultExitRect = new Rect(360f, 460f, 220f, 70f);

        private SkillResourceFileEntry _entry;
        private SkillConfig _config;
        private int _selectedLayerIndex;
        private bool _isRebuilding;

        private SkillGraphView _graphView;
        private IMGUIContainer _layerPanel;

        private readonly Dictionary<string, MetaSkillNodeView> _nodeViews = new Dictionary<string, MetaSkillNodeView>();
        private readonly Dictionary<string, SkillEventEdgeView> _edgeViews = new Dictionary<string, SkillEventEdgeView>();

        internal static void OpenForEntry(SkillResourceFileEntry entry)
        {
            SkillGraphEditorWindow window = GetWindow<SkillGraphEditorWindow>();
            window.titleContent = new GUIContent("SkillEditor");
            window.minSize = new Vector2(920f, 760f);
            window.Bind(entry);
            window.Show();
        }

        private void Bind(SkillResourceFileEntry entry)
        {
            _entry = entry;
            _config = entry != null ? entry.Config as SkillConfig : null;
            EnsureDefaultLayer();

            if (_graphView == null)
            {
                BuildUi();
            }

            RefreshAll();
            SkillEditorInspectorWindow.OpenSkill(_entry);
        }

        private void OnEnable()
        {
            if (_graphView == null)
            {
                BuildUi();
            }
            RefreshAll();
        }

        private void BuildUi()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Row;
            rootVisualElement.style.flexGrow = 1f;

            _layerPanel = new IMGUIContainer(DrawLayerPanel);
            _layerPanel.style.width = LayerPanelWidth;
            _layerPanel.style.minWidth = LayerPanelWidth;
            _layerPanel.style.flexShrink = 0f;
            rootVisualElement.Add(_layerPanel);

            _graphView = new SkillGraphView(this);
            _graphView.style.flexGrow = 1f;
            _graphView.graphViewChanged = OnGraphViewChanged;
            rootVisualElement.Add(_graphView);
        }

        private void OnSelectionChanged(List<ISelectable> selection)
        {
            if (_entry == null)
            {
                return;
            }

            if (selection == null || selection.Count == 0)
            {
                SkillEditorInspectorWindow.OpenSkill(_entry);
                return;
            }

            for (int i = 0; i < selection.Count; i++)
            {
                if (selection[i] is MetaSkillNodeView nodeView)
                {
                    SkillEditorInspectorWindow.OpenSkillNodeSelection(_entry, nodeView.Config, RefreshAll);
                    return;
                }

                if (selection[i] is SkillEventEdgeView edgeView)
                {
                    SkillEditorInspectorWindow.OpenSkillEventSelection(_entry, edgeView.Config, RefreshAll);
                    return;
                }
            }

            SkillEditorInspectorWindow.OpenSkill(_entry);
        }

        private void RefreshAll()
        {
            EnsureDefaultLayer();
            if (_graphView != null)
            {
                RebuildGraph();
            }

            _layerPanel?.MarkDirtyRepaint();
        }

        private void EnsureDefaultLayer()
        {
            if (_config == null)
            {
                return;
            }

            if (_config.Layers == null)
            {
                _config.Layers = new List<SkillLayerConfig>();
            }

            if (_config.Layers.Count == 0)
            {
                _config.Layers.Add(new SkillLayerConfig());
                MarkDirty();
            }

            _selectedLayerIndex = Mathf.Clamp(_selectedLayerIndex, 0, _config.Layers.Count - 1);
        }

        private SkillLayerConfig GetSelectedLayer()
        {
            if (_config == null || _config.Layers == null || _config.Layers.Count == 0)
            {
                return null;
            }

            return _config.Layers[Mathf.Clamp(_selectedLayerIndex, 0, _config.Layers.Count - 1)];
        }

        private void DrawLayerPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(LayerPanelWidth - 8f), GUILayout.ExpandHeight(true));

            if (_config == null)
            {
                EditorGUILayout.HelpBox("当前没有选中的 Skill。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(42f), GUILayout.Height(42f)))
            {
                AddLayer();
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _config.Layers.Count; i++)
            {
                SkillLayerConfig layer = _config.Layers[i];
                string label = string.IsNullOrEmpty(layer.DisplayName) ? $"Layer{i + 1}" : layer.DisplayName;
                if (GUILayout.Toggle(_selectedLayerIndex == i, label, "Button", GUILayout.Height(42f)))
                {
                    if (_selectedLayerIndex != i)
                    {
                        _selectedLayerIndex = i;
                        SkillEditorInspectorWindow.OpenSkill(_entry);
                        RefreshAll();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void AddLayer()
        {
            if (_config == null)
            {
                return;
            }

            SkillLayerConfig layer = new SkillLayerConfig
            {
                LayerIndex = _config.Layers.Count,
                DisplayName = $"Layer{_config.Layers.Count + 1}"
            };
            _config.Layers.Add(layer);
            _selectedLayerIndex = _config.Layers.Count - 1;
            MarkDirty();
            SkillEditorInspectorWindow.OpenSkill(_entry);
            RefreshAll();
        }

        private void RebuildGraph()
        {
            if (_graphView == null)
            {
                return;
            }

            _isRebuilding = true;
            try
            {
                _graphView.ClearGraph();
                _nodeViews.Clear();
                _edgeViews.Clear();

                SkillLayerConfig layer = GetSelectedLayer();
                if (layer == null)
                {
                    return;
                }

                EnsureSpecialNodePositions(layer);

                SkillEntryNodeView entryView = new SkillEntryNodeView();
                entryView.SetPosition(new Rect(layer.EntryEditorPositionX, layer.EntryEditorPositionY, DefaultEntryRect.width, DefaultEntryRect.height));
                _graphView.AddElement(entryView);

                SkillExitNodeView exitView = new SkillExitNodeView();
                exitView.SetPosition(new Rect(layer.ExitEditorPositionX, layer.ExitEditorPositionY, DefaultExitRect.width, DefaultExitRect.height));
                _graphView.AddElement(exitView);

                if (layer.MetaSkillNodes == null)
                {
                    layer.MetaSkillNodes = new List<MetaSkillNodeConfig>();
                }

                if (layer.SkillEvents == null)
                {
                    layer.SkillEvents = new List<SkillEventConfig>();
                }

                for (int i = 0; i < layer.MetaSkillNodes.Count; i++)
                {
                    MetaSkillNodeConfig nodeConfig = layer.MetaSkillNodes[i];
                    if (nodeConfig == null || string.IsNullOrEmpty(nodeConfig.NodeId))
                    {
                        continue;
                    }

                    MetaSkillNodeView nodeView = new MetaSkillNodeView(this, nodeConfig);
                    Rect position = nodeConfig.HasEditorPosition
                        ? new Rect(nodeConfig.EditorPositionX, nodeConfig.EditorPositionY, 220f, 110f)
                        : new Rect(240f + i * 240f, 180f, 220f, 110f);
                    if (!nodeConfig.HasEditorPosition)
                    {
                        nodeConfig.HasEditorPosition = true;
                        nodeConfig.EditorPositionX = position.x;
                        nodeConfig.EditorPositionY = position.y;
                    }
                    nodeView.SetPosition(position);
                    _nodeViews[nodeConfig.NodeId] = nodeView;
                    _graphView.AddElement(nodeView);
                }

                for (int i = 0; i < layer.SkillEvents.Count; i++)
                {
                    SkillEventConfig eventConfig = layer.SkillEvents[i];
                    if (eventConfig == null || string.IsNullOrEmpty(eventConfig.EventId))
                    {
                        continue;
                    }

                    Port outputPort = ResolveOutputPort(eventConfig.FromNodeId, entryView);
                    Port inputPort = ResolveInputPort(eventConfig.ToNodeId, exitView);
                    if (outputPort == null || inputPort == null)
                    {
                        continue;
                    }

                    SkillEventEdgeView edgeView = new SkillEventEdgeView(eventConfig);
                    edgeView.output = outputPort;
                    edgeView.input = inputPort;
                    edgeView.output.Connect(edgeView);
                    edgeView.input.Connect(edgeView);
                    _edgeViews[eventConfig.EventId] = edgeView;
                    _graphView.AddElement(edgeView);
                    edgeView.RefreshLabel();
                }
            }
            finally
            {
                _isRebuilding = false;
            }
        }

        private Port ResolveOutputPort(string fromNodeId, SkillEntryNodeView entryView)
        {
            if (fromNodeId == EntryNodeId)
            {
                return entryView.OutputPort;
            }

            return _nodeViews.TryGetValue(fromNodeId, out MetaSkillNodeView nodeView) ? nodeView.OutputPort : null;
        }

        private Port ResolveInputPort(string toNodeId, SkillExitNodeView exitView)
        {
            if (toNodeId == ExitNodeId)
            {
                return exitView.InputPort;
            }

            return _nodeViews.TryGetValue(toNodeId, out MetaSkillNodeView nodeView) ? nodeView.InputPort : null;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isRebuilding)
            {
                return change;
            }

            SkillLayerConfig layer = GetSelectedLayer();
            if (layer == null)
            {
                return change;
            }

            if (change.elementsToRemove != null)
            {
                for (int i = 0; i < change.elementsToRemove.Count; i++)
                {
                    if (change.elementsToRemove[i] is MetaSkillNodeView nodeView)
                    {
                        RemoveNode(layer, nodeView.Config.NodeId);
                    }
                    else if (change.elementsToRemove[i] is SkillEventEdgeView edgeView)
                    {
                        RemoveEdge(layer, edgeView.Config.EventId);
                    }
                }
            }

            if (change.movedElements != null)
            {
                for (int i = 0; i < change.movedElements.Count; i++)
                {
                    if (change.movedElements[i] is MetaSkillNodeView nodeView)
                    {
                        Rect rect = nodeView.GetPosition();
                        nodeView.Config.HasEditorPosition = true;
                        nodeView.Config.EditorPositionX = rect.x;
                        nodeView.Config.EditorPositionY = rect.y;
                        MarkDirty();
                    }
                    else if (change.movedElements[i] is SkillEntryNodeView entryView)
                    {
                        Rect rect = entryView.GetPosition();
                        layer.HasEntryEditorPosition = true;
                        layer.EntryEditorPositionX = rect.x;
                        layer.EntryEditorPositionY = rect.y;
                        MarkDirty();
                    }
                    else if (change.movedElements[i] is SkillExitNodeView exitView)
                    {
                        Rect rect = exitView.GetPosition();
                        layer.HasExitEditorPosition = true;
                        layer.ExitEditorPositionX = rect.x;
                        layer.ExitEditorPositionY = rect.y;
                        MarkDirty();
                    }
                }
            }

            if (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
            {
                List<Edge> pendingEdges = new List<Edge>(change.edgesToCreate);
                change.edgesToCreate.Clear();

                for (int i = 0; i < pendingEdges.Count; i++)
                {
                    Edge edge = pendingEdges[i];
                    if (edge.output == null || edge.input == null)
                    {
                        continue;
                    }

                    SkillEventConfig eventConfig = new SkillEventConfig
                    {
                        FromNodeId = ResolveNodeId(edge.output.node),
                        ToNodeId = ResolveNodeId(edge.input.node),
                    };
                    if (eventConfig.FromNodeId != EntryNodeId)
                    {
                        eventConfig.Events.Clear();
                    }
                    layer.SkillEvents.Add(eventConfig);

                    SkillEventEdgeView edgeView = new SkillEventEdgeView(eventConfig)
                    {
                        output = edge.output,
                        input = edge.input
                    };
                    edgeView.output.Connect(edgeView);
                    edgeView.input.Connect(edgeView);
                    _edgeViews[eventConfig.EventId] = edgeView;
                    _graphView.AddElement(edgeView);
                    edgeView.RefreshLabel();
                    MarkDirty();
                }
            }

            return change;
        }

        private void RemoveNode(SkillLayerConfig layer, string nodeId)
        {
            if (layer == null || string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            layer.MetaSkillNodes.RemoveAll(node => node != null && node.NodeId == nodeId);
            layer.SkillEvents.RemoveAll(edge => edge != null && (edge.FromNodeId == nodeId || edge.ToNodeId == nodeId));
            _nodeViews.Remove(nodeId);
            MarkDirty();
        }

        private void RemoveEdge(SkillLayerConfig layer, string eventId)
        {
            if (layer == null || string.IsNullOrEmpty(eventId))
            {
                return;
            }

            layer.SkillEvents.RemoveAll(edge => edge != null && edge.EventId == eventId);
            _edgeViews.Remove(eventId);
            MarkDirty();
        }

        private string ResolveNodeId(Node node)
        {
            switch (node)
            {
                case SkillEntryNodeView _:
                    return EntryNodeId;
                case SkillExitNodeView _:
                    return ExitNodeId;
                case MetaSkillNodeView nodeView:
                    return nodeView.Config.NodeId;
                default:
                    return string.Empty;
            }
        }

        internal void CreateMetaSkillNode(Vector2 position)
        {
            SkillLayerConfig layer = GetSelectedLayer();
            if (layer == null)
            {
                return;
            }

            MetaSkillNodeConfig nodeConfig = new MetaSkillNodeConfig
            {
                DisplayName = $"元技能{layer.MetaSkillNodes.Count + 1}",
                HasEditorPosition = true,
                EditorPositionX = position.x,
                EditorPositionY = position.y,
            };
            layer.MetaSkillNodes.Add(nodeConfig);
            MarkDirty();
            SkillEditorInspectorWindow.OpenSkillNodeSelection(_entry, nodeConfig, RefreshAll);
            RefreshAll();
        }

        private void MarkDirty()
        {
            if (_entry != null)
            {
                SkillResourceRepository.MarkDirty(_entry);
            }
        }

        private static void EnsureSpecialNodePositions(SkillLayerConfig layer)
        {
            if (layer == null)
            {
                return;
            }

            if (!layer.HasEntryEditorPosition)
            {
                layer.HasEntryEditorPosition = true;
                layer.EntryEditorPositionX = DefaultEntryRect.x;
                layer.EntryEditorPositionY = DefaultEntryRect.y;
            }

            if (!layer.HasExitEditorPosition)
            {
                layer.HasExitEditorPosition = true;
                layer.ExitEditorPositionX = DefaultExitRect.x;
                layer.ExitEditorPositionY = DefaultExitRect.y;
            }
        }

        private sealed class SkillGraphView : GraphView
        {
            private readonly SkillGraphEditorWindow _window;

            public SkillGraphView(SkillGraphEditorWindow window)
            {
                _window = window;
                Insert(0, new GridBackground());
                style.flexGrow = 1f;
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());
            }

            public override void AddToSelection(ISelectable selectable)
            {
                base.AddToSelection(selectable);
                _window.OnSelectionChanged(selection);
            }

            public override void RemoveFromSelection(ISelectable selectable)
            {
                base.RemoveFromSelection(selectable);
                _window.OnSelectionChanged(selection);
            }

            public override void ClearSelection()
            {
                base.ClearSelection();
                _window.OnSelectionChanged(selection);
            }

            public void ClearGraph()
            {
                List<GraphElement> elements = new List<GraphElement>();
                foreach (GraphElement element in graphElements)
                {
                    elements.Add(element);
                }

                DeleteElements(elements);
            }

            public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            {
                List<Port> result = new List<Port>();
                foreach (Port port in ports)
                {
                    if (port == startPort || port.direction == startPort.direction || port.node == startPort.node)
                    {
                        continue;
                    }

                    if (startPort.node is SkillExitNodeView || port.node is SkillEntryNodeView)
                    {
                        continue;
                    }

                    result.Add(port);
                }

                return result;
            }

            public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                base.BuildContextualMenu(evt);
                Vector2 position = contentViewContainer.WorldToLocal(evt.mousePosition);
                evt.menu.AppendAction("创建 MetaSkillNode", _ => _window.CreateMetaSkillNode(position));
            }
        }

        private sealed class SkillEntryNodeView : Node
        {
            public Port OutputPort { get; }

            public SkillEntryNodeView()
            {
                title = "Entry";
                capabilities &= ~Capabilities.Deletable;
                capabilities &= ~Capabilities.Copiable;

                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                OutputPort.portName = string.Empty;
                outputContainer.Add(OutputPort);
                RefreshExpandedState();
                RefreshPorts();
            }
        }

        private sealed class SkillExitNodeView : Node
        {
            public Port InputPort { get; }

            public SkillExitNodeView()
            {
                title = "Exit";
                capabilities &= ~Capabilities.Deletable;
                capabilities &= ~Capabilities.Copiable;

                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                InputPort.portName = string.Empty;
                inputContainer.Add(InputPort);
                RefreshExpandedState();
                RefreshPorts();
            }
        }

        private sealed class MetaSkillNodeView : Node
        {
            public MetaSkillNodeConfig Config { get; }
            public Port InputPort { get; }
            public Port OutputPort { get; }

            public MetaSkillNodeView(SkillGraphEditorWindow window, MetaSkillNodeConfig config)
            {
                Config = config;

                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                InputPort.portName = string.Empty;
                inputContainer.Add(InputPort);

                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                OutputPort.portName = string.Empty;
                outputContainer.Add(OutputPort);

                RefreshTitle();
                RefreshExpandedState();
                RefreshPorts();
            }

            public void RefreshTitle()
            {
                title = string.IsNullOrEmpty(Config.DisplayName) ? "MetaSkillNode" : Config.DisplayName;
            }
        }

        private sealed class SkillEventEdgeView : Edge
        {
            private readonly Label _label;

            public SkillEventConfig Config { get; }

            public SkillEventEdgeView(SkillEventConfig config)
            {
                Config = config;
                _label = new Label();
                _label.style.position = Position.Absolute;
                _label.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
                _label.style.paddingLeft = 6f;
                _label.style.paddingRight = 6f;
                _label.style.paddingTop = 2f;
                _label.style.paddingBottom = 2f;
                _label.style.color = new Color(0.92f, 0.92f, 0.92f);
                Add(_label);
                schedule.Execute(UpdateLabelPosition).Every(100);
            }

            public void RefreshLabel()
            {
                _label.text = BuildSummary(Config);
                UpdateLabelPosition();
            }

            private void UpdateLabelPosition()
            {
                if (output == null || input == null)
                {
                    return;
                }

                Vector2 start = output.worldBound.center;
                Vector2 end = input.worldBound.center;
                Vector2 mid = (start + end) * 0.5f;
                Vector2 local = parent != null ? parent.WorldToLocal(mid) : mid;
                _label.style.left = local.x - 60f;
                _label.style.top = local.y - 12f;
            }

            private static string BuildSummary(SkillEventConfig config)
            {
                if (config == null)
                {
                    return "Event";
                }

                if (config.Events == null || config.Events.Count == 0)
                {
                    return BuildDefaultSummary(config.FromNodeId);
                }

                List<string> parts = new List<string>();
                for (int i = 0; i < config.Events.Count; i++)
                {
                    SkillEventEntryConfig entry = config.Events[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    string item = BuildEventLabel(entry.EventType);
                    if (!string.IsNullOrEmpty(entry.Argument))
                    {
                        item += $"({entry.Argument})";
                    }
                    parts.Add(item);
                }

                if (parts.Count == 0)
                {
                    return BuildDefaultSummary(config.FromNodeId);
                }

                string separator = config.EventMode == SkillConditionMode.Any ? " | " : " & ";
                return string.Join(separator, parts);
            }

            private static string BuildDefaultSummary(string fromNodeId)
            {
                return fromNodeId == EntryNodeId ? "castSkill_short" : "onMetaSkillEnd";
            }

            private static string BuildEventLabel(SkillEventType eventType)
            {
                switch (eventType)
                {
                    case SkillEventType.CastSkillShort:
                        return "castSkill_short";
                    case SkillEventType.CastSkillLong:
                        return "castSkill_long";
                    case SkillEventType.OnMetaSkillEnd:
                        return "onMetaSkillEnd";
                    case SkillEventType.OnInterrupted:
                        return "onInterrupted";
                    default:
                        return eventType.ToString();
                }
            }
        }
    }
}