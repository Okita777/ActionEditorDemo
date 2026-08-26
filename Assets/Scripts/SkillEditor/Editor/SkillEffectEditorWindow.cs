using System;
using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor.Editor
{
    public sealed class SkillEffectEditorWindow : EditorWindow
    {
        private const float SidebarWidth = 280f;
        private const float NodeWidth = 220f;
        private const float NodeHeight = 96f;
        private const string PassPortKey = "pass";
        private const string FailPortKey = "fail";
        private const string EntryNodeId = "__entry__";

        private SkillResourceFileEntry _entry;
        private SkillEffectConfig _effectConfig;
        private Action _onModified;
        private string _targetTitle = "Effects";
        private string _selectedNodeId = string.Empty;
        private bool _isRebuilding;

        private Label _titleLabel;
        private Label _summaryLabel;
        private Button _createRootSequenceButton;
        private Button _createRootConditionButton;
        private Button _createRootActionButton;
        private Button _clearButton;
        private EffectGraphView _graphView;
        private readonly Dictionary<string, EffectGraphNode> _nodeViews = new Dictionary<string, EffectGraphNode>();

        internal static void OpenForEffect(SkillResourceFileEntry entry, SkillEffectConfig effectConfig, string targetTitle, Action onModified = null)
        {
            SkillEffectEditorWindow window = GetWindow<SkillEffectEditorWindow>();
            window.titleContent = new GUIContent("EffectsEditor");
            window.minSize = new Vector2(1100f, 700f);
            window.Bind(entry, effectConfig, targetTitle, onModified);
            window.Show();
        }

        internal static string BuildSummary(SkillEffectConfig effectConfig)
        {
            if (effectConfig == null || effectConfig.Nodes == null || effectConfig.Nodes.Count == 0 || string.IsNullOrEmpty(effectConfig.RootNodeId))
            {
                return "空效果树";
            }

            return $"Root={effectConfig.RootNodeId}  Nodes={effectConfig.Nodes.Count}";
        }

        private void CreateGUI()
        {
            BuildUi();
            RefreshWindow();
        }

        private void OnEnable()
        {
            if (_graphView == null)
            {
                BuildUi();
            }

            RefreshWindow();
        }

        private void Bind(SkillResourceFileEntry entry, SkillEffectConfig effectConfig, string targetTitle, Action onModified)
        {
            _entry = entry;
            _effectConfig = effectConfig ?? new SkillEffectConfig();
            _targetTitle = string.IsNullOrEmpty(targetTitle) ? "Effects" : targetTitle;
            _onModified = onModified;
            EnsureConfig();

            if (string.IsNullOrEmpty(_selectedNodeId) && !string.IsNullOrEmpty(_effectConfig.RootNodeId))
            {
                _selectedNodeId = _effectConfig.RootNodeId;
            }

            if (_graphView == null)
            {
                BuildUi();
            }

            RefreshWindow();
        }

        private void BuildUi()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Row;
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);

            VisualElement sidebar = new VisualElement();
            sidebar.style.width = SidebarWidth;
            sidebar.style.minWidth = SidebarWidth;
            sidebar.style.flexShrink = 0f;
            sidebar.style.flexDirection = FlexDirection.Column;
            sidebar.style.backgroundColor = new Color(0.19f, 0.19f, 0.19f);
            sidebar.style.borderRightWidth = 1f;
            sidebar.style.borderRightColor = new Color(0.12f, 0.12f, 0.12f);
            sidebar.style.paddingLeft = 10f;
            sidebar.style.paddingRight = 10f;
            sidebar.style.paddingTop = 10f;
            sidebar.style.paddingBottom = 10f;

            _titleLabel = new Label(_targetTitle);
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.fontSize = 15f;
            _titleLabel.style.marginBottom = 4f;
            sidebar.Add(_titleLabel);

            _summaryLabel = new Label();
            _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            _summaryLabel.style.color = new Color(0.82f, 0.82f, 0.82f);
            _summaryLabel.style.marginBottom = 12f;
            sidebar.Add(_summaryLabel);

            Label logicLabel = new Label("Logic");
            logicLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            logicLabel.style.marginBottom = 6f;
            sidebar.Add(logicLabel);

            _createRootSequenceButton = CreateSidebarButton("Sequence", () => CreateStandaloneNode(SkillEffectNodeType.Sequence));
            _createRootConditionButton = CreateSidebarButton("Condition", () => CreateStandaloneNode(SkillEffectNodeType.Condition));
            _createRootActionButton = CreateSidebarButton("Action", () => CreateStandaloneNode(SkillEffectNodeType.Action));
            _clearButton = CreateSidebarButton("清空", ClearGraph);
            _clearButton.style.marginTop = 8f;

            sidebar.Add(_createRootSequenceButton);
            sidebar.Add(_createRootConditionButton);
            sidebar.Add(_createRootActionButton);
            sidebar.Add(_clearButton);

            rootVisualElement.Add(sidebar);

            _graphView = new EffectGraphView(this);
            _graphView.style.flexGrow = 1f;
            _graphView.graphViewChanged = OnGraphViewChanged;
            rootVisualElement.Add(_graphView);
        }

        private Button CreateSidebarButton(string text, Action onClick)
        {
            Button button = new Button(() => onClick?.Invoke()) { text = text };
            button.style.height = 32f;
            button.style.marginBottom = 6f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            return button;
        }

        private void RefreshWindow()
        {
            EnsureConfig();
            if (_titleLabel != null) _titleLabel.text = _targetTitle;
            if (_summaryLabel != null) _summaryLabel.text = BuildSummary(_effectConfig);

            bool empty = _effectConfig == null || _effectConfig.Nodes == null || _effectConfig.Nodes.Count == 0;
            _createRootSequenceButton?.SetEnabled(true);
            _createRootConditionButton?.SetEnabled(true);
            _createRootActionButton?.SetEnabled(true);
            _clearButton?.SetEnabled(!empty);

            RebuildGraph();
        }

        private void RebuildGraph()
        {
            if (_graphView == null || _effectConfig == null)
            {
                return;
            }

            _isRebuilding = true;
            try
            {
                _nodeViews.Clear();
                _graphView.ClearGraph();
                EntryGraphNode entryView = new EntryGraphNode(new Rect(40f, 40f, NodeWidth, 64f));
                _graphView.AddElement(entryView);

                for (int i = 0; i < _effectConfig.Nodes.Count; i++)
                {
                    SkillEffectNodeConfig node = _effectConfig.Nodes[i];
                    if (node == null || string.IsNullOrEmpty(node.NodeId))
                    {
                        continue;
                    }

                    Rect rect = GetNodeRect(node, i);
                    EffectGraphNode nodeView = new EffectGraphNode(this, node, rect);
                    _nodeViews[node.NodeId] = nodeView;
                    _graphView.AddElement(nodeView);
                }

                for (int i = 0; i < _effectConfig.Nodes.Count; i++)
                {
                    SkillEffectNodeConfig parent = _effectConfig.Nodes[i];
                    if (parent == null || parent.Children == null)
                    {
                        continue;
                    }

                    if (!_nodeViews.TryGetValue(parent.NodeId, out EffectGraphNode parentView))
                    {
                        continue;
                    }

                    for (int childIndex = 0; childIndex < parent.Children.Count; childIndex++)
                    {
                        string childId = parent.Children[childIndex];
                        if (string.IsNullOrEmpty(childId) || !_nodeViews.TryGetValue(childId, out EffectGraphNode childView))
                        {
                            continue;
                        }

                        Port outputPort = parentView.GetOutputPort(childIndex);
                        if (outputPort != null && childView.InputPort != null)
                        {
                            _graphView.AddElement(outputPort.ConnectTo(childView.InputPort));
                        }
                    }
                }

                if (!string.IsNullOrEmpty(_effectConfig.RootNodeId) &&
                    _nodeViews.TryGetValue(_effectConfig.RootNodeId, out EffectGraphNode rootView) &&
                    entryView.OutputPort != null &&
                    rootView.InputPort != null)
                {
                    _graphView.AddElement(entryView.OutputPort.ConnectTo(rootView.InputPort));
                }
            }
            finally
            {
                _isRebuilding = false;
            }
        }

        private Rect GetNodeRect(SkillEffectNodeConfig node, int fallbackIndex)
        {
            if (node != null && node.HasEditorPosition)
            {
                return new Rect(node.EditorPositionX, node.EditorPositionY, NodeWidth, NodeHeight);
            }

            Vector2 position = GetDefaultNodePosition(fallbackIndex);
            if (node != null)
            {
                node.HasEditorPosition = true;
                node.EditorPositionX = position.x;
                node.EditorPositionY = position.y;
            }

            return new Rect(position.x, position.y, NodeWidth, NodeHeight);
        }

        private static Vector2 GetDefaultNodePosition(int index)
        {
            return new Vector2(320f + (index % 4) * 40f, 80f + index * 32f);
        }

        private static bool UpdateNodePosition(SkillEffectNodeConfig node, Rect position)
        {
            if (node == null)
            {
                return false;
            }

            bool changed = !node.HasEditorPosition ||
                !Mathf.Approximately(node.EditorPositionX, position.x) ||
                !Mathf.Approximately(node.EditorPositionY, position.y);

            node.HasEditorPosition = true;
            node.EditorPositionX = position.x;
            node.EditorPositionY = position.y;
            return changed;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isRebuilding || _effectConfig == null)
            {
                return change;
            }

            bool modified = false;
            bool needsRefresh = false;
            if (change.elementsToRemove != null)
            {
                HashSet<string> deletedNodeIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < change.elementsToRemove.Count; i++)
                {
                    if (change.elementsToRemove[i] is Edge edge)
                    {
                        modified |= RemoveEdgeRelation(edge);
                    }
                    else if (change.elementsToRemove[i] is EffectGraphNode nodeView)
                    {
                        deletedNodeIds.Add(nodeView.NodeId);
                    }
                }

                foreach (string nodeId in deletedNodeIds)
                {
                    DeleteNode(nodeId);
                    modified = true;
                }

                if (deletedNodeIds.Count > 0 || change.elementsToRemove.Count > 0)
                {
                    needsRefresh = true;
                }
            }

            if (change.edgesToCreate != null)
            {
                for (int i = 0; i < change.edgesToCreate.Count; i++)
                {
                    modified |= AddEdgeRelation(change.edgesToCreate[i]);
                }

                if (change.edgesToCreate.Count > 0)
                {
                    needsRefresh = true;
                }
            }

            if (change.movedElements != null)
            {
                for (int i = 0; i < change.movedElements.Count; i++)
                {
                    if (change.movedElements[i] is EffectGraphNode nodeView)
                    {
                        SkillEffectNodeConfig node = FindNode(nodeView.NodeId);
                        modified |= UpdateNodePosition(node, nodeView.GetPosition());
                    }
                }
            }

            if (modified)
            {
                MarkDirty();
                if (needsRefresh)
                {
                    EditorApplication.delayCall += RefreshWindow;
                }
            }

            return change;
        }

        private bool AddEdgeRelation(Edge edge)
        {
            if (edge?.output?.node is EntryGraphNode && edge.input?.node is EffectGraphNode entryChildView)
            {
                RemoveChildReference(entryChildView.NodeId);
                if (!string.Equals(_effectConfig.RootNodeId, entryChildView.NodeId, StringComparison.Ordinal))
                {
                    _effectConfig.RootNodeId = entryChildView.NodeId;
                    return true;
                }

                return false;
            }

            EffectGraphNode parentView = edge.output?.node as EffectGraphNode;
            EffectGraphNode childView = edge.input?.node as EffectGraphNode;
            if (parentView == null || childView == null)
            {
                return false;
            }

            SkillEffectNodeConfig parent = FindNode(parentView.NodeId);
            SkillEffectNodeConfig child = FindNode(childView.NodeId);
            if (parent == null || child == null || parent == child)
            {
                return false;
            }

            RemoveChildReference(child.NodeId);
            EnsureChildren(parent);

            if (parent.NodeType == SkillEffectNodeType.Sequence)
            {
                if (!parent.Children.Contains(child.NodeId))
                {
                    parent.Children.Add(child.NodeId);
                    return true;
                }
                return false;
            }

            if (parent.NodeType == SkillEffectNodeType.Condition)
            {
                while (parent.Children.Count < 2)
                {
                    parent.Children.Add(string.Empty);
                }

                int index = edge.output.userData is string key && key == FailPortKey ? 1 : 0;
                if (!string.Equals(parent.Children[index], child.NodeId, StringComparison.Ordinal))
                {
                    parent.Children[index] = child.NodeId;
                    return true;
                }
            }

            return false;
        }

        private bool RemoveEdgeRelation(Edge edge)
        {
            if (edge?.output?.node is EntryGraphNode && edge.input?.node is EffectGraphNode entryChildView)
            {
                if (string.Equals(_effectConfig.RootNodeId, entryChildView.NodeId, StringComparison.Ordinal))
                {
                    _effectConfig.RootNodeId = string.Empty;
                    return true;
                }

                return false;
            }

            EffectGraphNode parentView = edge.output?.node as EffectGraphNode;
            EffectGraphNode childView = edge.input?.node as EffectGraphNode;
            if (parentView == null || childView == null)
            {
                return false;
            }

            SkillEffectNodeConfig parent = FindNode(parentView.NodeId);
            if (parent == null || parent.Children == null)
            {
                return false;
            }

            bool removed = false;
            for (int i = parent.Children.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(parent.Children[i], childView.NodeId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (parent.NodeType == SkillEffectNodeType.Condition)
                {
                    parent.Children[i] = string.Empty;
                }
                else
                {
                    parent.Children.RemoveAt(i);
                }
                removed = true;
            }

            return removed;
        }

        private void DrawInspector()
        {
            GUILayout.Space(8f);
            GUILayout.BeginVertical();

            if (_effectConfig == null || _effectConfig.Nodes == null || _effectConfig.Nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("当前效果树为空。请先创建节点，并连接到 Entry。", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            SkillEffectNodeConfig node = FindNode(_selectedNodeId) ?? FindNode(_effectConfig.RootNodeId);
            if (node == null)
            {
                EditorGUILayout.HelpBox("当前没有可编辑节点。", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            _selectedNodeId = node.NodeId;
            EditorGUILayout.LabelField("EffectNodeInfo", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("NodeId", node.NodeId, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Root", string.Equals(node.NodeId, _effectConfig.RootNodeId, StringComparison.Ordinal) ? "Yes" : "No", EditorStyles.miniLabel);
            EditorGUILayout.Space(6f);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("设为 Root"))
                {
                    _effectConfig.RootNodeId = node.NodeId;
                    MarkDirty();
                    RefreshWindow();
                    GUILayout.EndVertical();
                    return;
                }

                if (GUILayout.Button("删除节点") && EditorUtility.DisplayDialog("删除节点", "确认删除该节点及其子树？", "删除", "取消"))
                {
                    DeleteNode(node.NodeId);
                    MarkDirty();
                    RefreshWindow();
                    GUILayout.EndVertical();
                    return;
                }
            }

            SkillEffectNodeType nextType = (SkillEffectNodeType)EditorGUILayout.EnumPopup("NodeType", node.NodeType);
            if (nextType != node.NodeType)
            {
                node.NodeType = nextType;
                ApplyNodeDefaults(node, true);
                MarkDirty();
                RefreshWindow();
                GUILayout.EndVertical();
                return;
            }

            DrawChildren(node);
            EditorGUILayout.Space(8f);
            DrawNodeInfo(node);
            GUILayout.EndVertical();
        }

        private void DrawChildren(SkillEffectNodeConfig node)
        {
            EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
            if (node.NodeType == SkillEffectNodeType.Action)
            {
                EditorGUILayout.HelpBox("Action 节点没有子节点。", MessageType.None);
                return;
            }

            if (node.NodeType == SkillEffectNodeType.Sequence)
            {
                DrawSequenceChildren(node);
            }
            else
            {
                DrawConditionChildren(node);
            }
        }

        private void DrawSequenceChildren(SkillEffectNodeConfig node)
        {
            EnsureChildren(node);
            for (int i = 0; i < node.Children.Count; i++)
            {
                SkillEffectNodeConfig child = FindNode(node.Children[i]);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(child != null ? GetNodeTitle(child) : "空引用", GUILayout.Width(180f));

                    if (GUILayout.Button("选中", GUILayout.Width(50f)) && child != null)
                    {
                        SelectNode(child.NodeId);
                    }

                    GUI.enabled = i > 0;
                    if (GUILayout.Button("上移", GUILayout.Width(50f)))
                    {
                        SwapChild(node, i, i - 1);
                        GUI.enabled = true;
                        MarkDirty();
                        RefreshWindow();
                        return;
                    }

                    GUI.enabled = i < node.Children.Count - 1;
                    if (GUILayout.Button("下移", GUILayout.Width(50f)))
                    {
                        SwapChild(node, i, i + 1);
                        GUI.enabled = true;
                        MarkDirty();
                        RefreshWindow();
                        return;
                    }

                    GUI.enabled = true;
                    if (GUILayout.Button("移除", GUILayout.Width(50f)))
                    {
                        node.Children.RemoveAt(i);
                        MarkDirty();
                        RefreshWindow();
                        return;
                    }
                }
            }

            DrawCreateChildButtons(node, -1);
        }

        private void DrawConditionChildren(SkillEffectNodeConfig node)
        {
            EnsureChildren(node);
            while (node.Children.Count < 2)
            {
                node.Children.Add(string.Empty);
            }

            DrawConditionChildSlot(node, 0, "通过分支");
            DrawConditionChildSlot(node, 1, "失败分支");
        }

        private void DrawConditionChildSlot(SkillEffectNodeConfig node, int index, string label)
        {
            SkillEffectNodeConfig child = FindNode(node.Children[index]);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(child != null ? GetNodeTitle(child) : "未连接", GUILayout.Width(180f));

                if (GUILayout.Button("选中", GUILayout.Width(50f)) && child != null)
                {
                    SelectNode(child.NodeId);
                }

                if (GUILayout.Button("清空", GUILayout.Width(50f)))
                {
                    node.Children[index] = string.Empty;
                    MarkDirty();
                    RefreshWindow();
                    return;
                }
            }

            DrawCreateChildButtons(node, index);
        }

        private void DrawCreateChildButtons(SkillEffectNodeConfig parent, int conditionIndex)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加 Sequence"))
                {
                    CreateChildNode(parent, conditionIndex, SkillEffectNodeType.Sequence);
                    return;
                }

                if (GUILayout.Button("添加 Condition"))
                {
                    CreateChildNode(parent, conditionIndex, SkillEffectNodeType.Condition);
                    return;
                }

                if (GUILayout.Button("添加 Action"))
                {
                    CreateChildNode(parent, conditionIndex, SkillEffectNodeType.Action);
                    return;
                }
            }
        }

        private void CreateChildNode(SkillEffectNodeConfig parent, int conditionIndex, SkillEffectNodeType nodeType)
        {
            SkillEffectNodeConfig child = CreateNode(nodeType);
            _effectConfig.Nodes.Add(child);

            if (parent.NodeType == SkillEffectNodeType.Condition && conditionIndex >= 0)
            {
                EnsureChildren(parent);
                while (parent.Children.Count < 2)
                {
                    parent.Children.Add(string.Empty);
                }

                parent.Children[conditionIndex] = child.NodeId;
            }
            else
            {
                EnsureChildren(parent);
                parent.Children.Add(child.NodeId);
            }

            _selectedNodeId = child.NodeId;
            MarkDirty();
            RefreshWindow();
        }

        private void DrawNodeInfo(SkillEffectNodeConfig node)
        {
            switch (node.NodeType)
            {
                case SkillEffectNodeType.Sequence:
                    EditorGUILayout.HelpBox("Sequence 只按顺序执行子节点，不允许并行。", MessageType.None);
                    break;
                case SkillEffectNodeType.Condition:
                    DrawConditionInfo(node);
                    break;
                case SkillEffectNodeType.Action:
                    DrawActionInfo(node);
                    break;
            }
        }

        private void DrawConditionInfo(SkillEffectNodeConfig node)
        {
            EnsureCondition(node);

            SkillConditionType nextType = (SkillConditionType)EditorGUILayout.EnumPopup("ConditionType", node.Condition.ConditionType);
            if (nextType != node.Condition.ConditionType)
            {
                node.Condition.CreateData(nextType);
                MarkDirty();
                RefreshWindow();
                return;
            }

            switch (node.Condition.Data)
            {
                case AttributeCompare_SkillConditionData attributeCompare:
                    DrawAttributeCompare(attributeCompare.Args);
                    break;
                case HasBuff_SkillConditionData hasBuff:
                    DrawBuffCondition(hasBuff.Args);
                    break;
                case HasTag_SkillConditionData hasTag:
                    DrawTagCondition(hasTag.Args);
                    break;
                case LastActionSucceeded_SkillConditionData succeeded:
                    DrawActionResultCondition(succeeded.Args);
                    break;
                case LastActionFailed_SkillConditionData failed:
                    DrawActionResultCondition(failed.Args);
                    break;
            }
        }

        private void DrawActionInfo(SkillEffectNodeConfig node)
        {
            EnsureAction(node);

            if (SkillEditorInspectorWindow.TagSelectionEditorUtility.DrawTagContainer("Action Tags", node.Action.Tags))
            {
                MarkDirty();
                RefreshWindow();
                return;
            }

            SkillActionType nextType = (SkillActionType)EditorGUILayout.EnumPopup("ActionType", node.Action.ActionType);
            if (nextType != node.Action.ActionType)
            {
                node.Action.CreateData(nextType);
                MarkDirty();
                RefreshWindow();
                return;
            }

            switch (node.Action.Data)
            {
                case DealDamage_SkillActionData dealDamage:
                    DrawDamageAction(dealDamage.Args);
                    break;
                case AddToughnessDamage_SkillActionData addToughnessDamage:
                    DrawDamageAction(addToughnessDamage.Args);
                    break;
                case AddAttribute_SkillActionData addAttribute:
                    DrawAttributeAction(addAttribute.Args);
                    break;
                case AddTag_SkillActionData addTag:
                    DrawTagAction(addTag.Args);
                    break;
                case AddBuff_SkillActionData addBuff:
                    DrawBuffAction(addBuff.Args);
                    break;
                case RemoveBuff_SkillActionData removeBuff:
                    DrawBuffAction(removeBuff.Args);
                    break;
            }
        }

        private void DrawAttributeCompare(AttributeCompareArgs args)
        {
            SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("QueryTarget", args.QueryTarget);
            SkillAttributeType attribute = (SkillAttributeType)EditorGUILayout.EnumPopup("AttributeType", args.AttributeType);
            SkillCompareOperator compare = (SkillCompareOperator)EditorGUILayout.EnumPopup("CompareOperator", args.CompareOperator);
            float value = EditorGUILayout.FloatField("Value", args.Value);

            if (target != args.QueryTarget || attribute != args.AttributeType || compare != args.CompareOperator || !Mathf.Approximately(value, args.Value))
            {
                args.QueryTarget = target;
                args.AttributeType = attribute;
                args.CompareOperator = compare;
                args.Value = value;
                MarkDirty();
                RefreshWindow();
            }
        }

        private void DrawBuffCondition(BuffConditionArgs args)
        {
            SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("QueryTarget", args.QueryTarget);
            string buffId = EditorGUILayout.TextField("BuffId", args.BuffId);
            if (target != args.QueryTarget || buffId != args.BuffId)
            {
                args.QueryTarget = target;
                args.BuffId = buffId;
                MarkDirty();
                RefreshWindow();
            }
        }

        private void DrawTagCondition(TagConditionArgs args)
        {
            SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("QueryTarget", args.QueryTarget);
            string tag = args.Tag;
            bool tagChanged = SkillEditorInspectorWindow.TagSelectionEditorUtility.DrawSingleTagField("Tag", ref tag);
            if (target != args.QueryTarget || tagChanged)
            {
                args.QueryTarget = target;
                args.Tag = tag;
                MarkDirty();
                RefreshWindow();
            }
        }

        private void DrawActionResultCondition(ActionResultConditionArgs args)
        {
            int actionIndex = EditorGUILayout.IntField("ActionIndex", args.ActionIndex);
            if (actionIndex != args.ActionIndex)
            {
                args.ActionIndex = actionIndex;
                MarkDirty();
                RefreshWindow();
            }
        }

        private void DrawDamageAction(DamageActionArgs args)
        {
            SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("Targets", args.QueryTarget);
            SkillAttributeType sourceAttribute = (SkillAttributeType)EditorGUILayout.EnumPopup("SourceAttribute", args.SourceAttribute);
            string actionId = EditorGUILayout.TextField("ActionId", args.ActionId);
            string description = EditorGUILayout.TextField("Description", args.Description);
            float ratio = EditorGUILayout.FloatField("Ratio", args.Ratio);

            if (target != args.QueryTarget || sourceAttribute != args.SourceAttribute || actionId != args.ActionId || description != args.Description || !Mathf.Approximately(ratio, args.Ratio))
            {
                args.QueryTarget = target;
                args.SourceAttribute = sourceAttribute;
                args.ActionId = actionId;
                args.Description = description;
                args.Ratio = ratio;
                MarkDirty();
                RefreshWindow();
            }
        }

        private void DrawBuffAction(BuffActionArgs args)
        {
            SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("Targets", args.QueryTarget);
            string buffId = EditorGUILayout.TextField("BuffId", args.BuffId);
            float duration = EditorGUILayout.FloatField("Duration", args.Duration);

            if (target != args.QueryTarget || buffId != args.BuffId || !Mathf.Approximately(duration, args.Duration))
            {
                args.QueryTarget = target;
                args.BuffId = buffId;
                args.Duration = duration;
                MarkDirty();
                RefreshWindow();
            }
        }

        private void DrawAttributeAction(AttributeActionArgs args)
        {
            SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("Targets", args.QueryTarget);
            SkillAttributeType attributeType = (SkillAttributeType)EditorGUILayout.EnumPopup("AttributeType", args.AttributeType);
            AttributeModifyMode modifyMode = (AttributeModifyMode)EditorGUILayout.EnumPopup("ModifyMode", args.ModifyMode);
            AttributeApplyLifetime applyLifetime = (AttributeApplyLifetime)EditorGUILayout.EnumPopup("ApplyLifetime", args.ApplyLifetime);
            float value = EditorGUILayout.FloatField("Value", args.Value);

            if (target != args.QueryTarget || attributeType != args.AttributeType || modifyMode != args.ModifyMode ||
                applyLifetime != args.ApplyLifetime || !Mathf.Approximately(value, args.Value))
            {
                args.QueryTarget = target;
                args.AttributeType = attributeType;
                args.ModifyMode = modifyMode;
                args.ApplyLifetime = applyLifetime;
                args.Value = value;
                MarkDirty();
                RefreshWindow();
            }
        }

        private void DrawTagAction(TagActionArgs args)
        {
            SkillQueryTargetType target = (SkillQueryTargetType)EditorGUILayout.EnumPopup("Targets", args.QueryTarget);
            args.Tags ??= new List<string>();
            bool tagsChanged = SkillEditorInspectorWindow.TagSelectionEditorUtility.DrawTagList("Tags", args.Tags);
            int stack = Mathf.Max(1, EditorGUILayout.IntField("Stack", args.Stack));
            AttributeApplyLifetime applyLifetime = (AttributeApplyLifetime)EditorGUILayout.EnumPopup("ApplyLifetime", args.ApplyLifetime);

            if (target != args.QueryTarget || tagsChanged || stack != args.Stack || applyLifetime != args.ApplyLifetime)
            {
                args.QueryTarget = target;
                args.Stack = stack;
                args.ApplyLifetime = applyLifetime;
                MarkDirty();
                RefreshWindow();
            }
        }

        private void SelectNode(string nodeId)
        {
            _selectedNodeId = nodeId;
            SkillEditorInspectorWindow.OpenEffectNodeSelection(_entry, FindNode(nodeId), HandleInspectorModified);
        }

        private void CreateStandaloneNode(SkillEffectNodeType nodeType)
        {
            SkillEffectNodeConfig node = CreateNode(nodeType);
            _effectConfig.Nodes.Add(node);
            if (string.IsNullOrEmpty(_effectConfig.RootNodeId))
            {
                _effectConfig.RootNodeId = node.NodeId;
            }

            SelectNode(node.NodeId);
            MarkDirty();
            RefreshWindow();
        }

        private void CreateRootNode(SkillEffectNodeType nodeType)
        {
            CreateStandaloneNode(nodeType);
        }

        private SkillEffectNodeConfig CreateNode(SkillEffectNodeType nodeType)
        {
            SkillEffectNodeConfig node = new SkillEffectNodeConfig
            {
                NodeType = nodeType,
                Children = new List<string>()
            };
            ApplyNodeDefaults(node, true);
            UpdateNodePosition(node, new Rect(GetDefaultNodePosition(_effectConfig?.Nodes?.Count ?? 0), new Vector2(NodeWidth, NodeHeight)));
            return node;
        }

        private void HandleInspectorModified()
        {
            RefreshWindow();
            Repaint();
        }

        private void DeleteNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            HashSet<string> subtree = new HashSet<string>(StringComparer.Ordinal);
            CollectSubtree(nodeId, subtree);

            for (int i = 0; i < _effectConfig.Nodes.Count; i++)
            {
                SkillEffectNodeConfig node = _effectConfig.Nodes[i];
                if (node == null || node.Children == null)
                {
                    continue;
                }

                for (int childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
                {
                    if (!subtree.Contains(node.Children[childIndex]))
                    {
                        continue;
                    }

                    if (node.NodeType == SkillEffectNodeType.Condition)
                    {
                        node.Children[childIndex] = string.Empty;
                    }
                    else
                    {
                        node.Children.RemoveAt(childIndex);
                    }
                }
            }

            for (int i = _effectConfig.Nodes.Count - 1; i >= 0; i--)
            {
                SkillEffectNodeConfig node = _effectConfig.Nodes[i];
                if (node != null && subtree.Contains(node.NodeId))
                {
                    _effectConfig.Nodes.RemoveAt(i);
                }
            }

            if (subtree.Contains(_effectConfig.RootNodeId))
            {
                _effectConfig.RootNodeId = _effectConfig.Nodes.Count > 0 ? _effectConfig.Nodes[0].NodeId : string.Empty;
            }

            _selectedNodeId = _effectConfig.RootNodeId;
        }

        private void CollectSubtree(string nodeId, HashSet<string> result)
        {
            if (string.IsNullOrEmpty(nodeId) || result.Contains(nodeId))
            {
                return;
            }

            SkillEffectNodeConfig node = FindNode(nodeId);
            if (node == null)
            {
                return;
            }

            result.Add(nodeId);
            if (node.Children == null)
            {
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                CollectSubtree(node.Children[i], result);
            }
        }

        private void SwapChild(SkillEffectNodeConfig node, int from, int to)
        {
            string temp = node.Children[from];
            node.Children[from] = node.Children[to];
            node.Children[to] = temp;
        }

        private void RemoveChildReference(string childNodeId)
        {
            for (int i = 0; i < _effectConfig.Nodes.Count; i++)
            {
                SkillEffectNodeConfig node = _effectConfig.Nodes[i];
                if (node == null || node.Children == null)
                {
                    continue;
                }

                for (int childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
                {
                    if (!string.Equals(node.Children[childIndex], childNodeId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (node.NodeType == SkillEffectNodeType.Condition)
                    {
                        node.Children[childIndex] = string.Empty;
                    }
                    else
                    {
                        node.Children.RemoveAt(childIndex);
                    }
                }
            }
        }

        private void EnsureConfig()
        {
            _effectConfig ??= new SkillEffectConfig();
            _effectConfig.Nodes ??= new List<SkillEffectNodeConfig>();

            for (int i = _effectConfig.Nodes.Count - 1; i >= 0; i--)
            {
                SkillEffectNodeConfig node = _effectConfig.Nodes[i];
                if (node == null)
                {
                    _effectConfig.Nodes.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(node.NodeId))
                {
                    node.NodeId = Guid.NewGuid().ToString("N");
                }

                ApplyNodeDefaults(node, false);
            }

            if (string.IsNullOrEmpty(_effectConfig.RootNodeId) && _effectConfig.Nodes.Count > 0)
            {
                _effectConfig.RootNodeId = _effectConfig.Nodes[0].NodeId;
            }
        }

        private static void ApplyNodeDefaults(SkillEffectNodeConfig node, bool resetChildren)
        {
            if (node == null)
            {
                return;
            }

            node.Children ??= new List<string>();

            if (resetChildren)
            {
                if (node.NodeType == SkillEffectNodeType.Action)
                {
                    node.Children.Clear();
                }
                else if (node.NodeType == SkillEffectNodeType.Condition)
                {
                    while (node.Children.Count < 2)
                    {
                        node.Children.Add(string.Empty);
                    }

                    if (node.Children.Count > 2)
                    {
                        node.Children.RemoveRange(2, node.Children.Count - 2);
                    }
                }
            }

            if (node.NodeType == SkillEffectNodeType.Condition)
            {
                node.Condition ??= new SkillConditionConfig();
                if (node.Condition.Data == null)
                {
                    node.Condition.CreateData(SkillConditionType.AttributeCompare);
                }
            }
            else if (node.NodeType == SkillEffectNodeType.Action)
            {
                node.Action ??= new SkillActionConfig();
                if (node.Action.Data == null)
                {
                    node.Action.CreateData(SkillActionType.DealDamage);
                }
            }
        }

        private static void EnsureChildren(SkillEffectNodeConfig node)
        {
            if (node != null && node.Children == null)
            {
                node.Children = new List<string>();
            }
        }

        private static void EnsureCondition(SkillEffectNodeConfig node)
        {
            node.Condition ??= new SkillConditionConfig();
            if (node.Condition.Data == null)
            {
                node.Condition.CreateData(SkillConditionType.AttributeCompare);
            }
        }

        private static void EnsureAction(SkillEffectNodeConfig node)
        {
            node.Action ??= new SkillActionConfig();
            if (node.Action.Data == null)
            {
                node.Action.CreateData(SkillActionType.DealDamage);
            }
        }

        private SkillEffectNodeConfig FindNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || _effectConfig == null || _effectConfig.Nodes == null)
            {
                return null;
            }

            for (int i = 0; i < _effectConfig.Nodes.Count; i++)
            {
                SkillEffectNodeConfig node = _effectConfig.Nodes[i];
                if (node != null && string.Equals(node.NodeId, nodeId, StringComparison.Ordinal))
                {
                    return node;
                }
            }

            return null;
        }

        private string GetNodeTitle(SkillEffectNodeConfig node)
        {
            if (node == null)
            {
                return "Null";
            }

            switch (node.NodeType)
            {
                case SkillEffectNodeType.Sequence:
                    return "Sequence";
                case SkillEffectNodeType.Condition:
                    return $"Condition : {node.Condition?.ConditionType ?? SkillConditionType.None}";
                case SkillEffectNodeType.Action:
                    return $"Action : {node.Action?.ActionType ?? SkillActionType.None}";
                default:
                    return node.NodeType.ToString();
            }
        }

        private string GetNodeDetail(SkillEffectNodeConfig node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            switch (node.NodeType)
            {
                case SkillEffectNodeType.Sequence:
                    return $"顺序执行 {node.Children?.Count ?? 0} 个子节点";
                case SkillEffectNodeType.Condition:
                    return "满足走通过分支，不满足走失败分支";
                case SkillEffectNodeType.Action:
                    return "执行实际效果";
                default:
                    return string.Empty;
            }
        }

        private void MarkDirty()
        {
            SkillResourceRepository.MarkDirty(_entry);
            _onModified?.Invoke();
        }

        private void ClearGraph()
        {
            if (!EditorUtility.DisplayDialog("清空 Effects", "确认清空当前效果树？", "清空", "取消"))
            {
                return;
            }

            _effectConfig.RootNodeId = string.Empty;
            _effectConfig.Nodes.Clear();
            _selectedNodeId = string.Empty;
            MarkDirty();
            RefreshWindow();
        }

        private sealed class EffectGraphView : GraphView
        {
            private readonly SkillEffectEditorWindow _window;

            public EffectGraphView(SkillEffectEditorWindow window)
            {
                _window = window;
                Insert(0, new GridBackground());
                style.flexGrow = 1f;
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());
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
                    if (port == startPort || port.node == startPort.node || port.direction == startPort.direction)
                    {
                        continue;
                    }

                    bool startIsEntry = port.node is EntryGraphNode || startPort.node is EntryGraphNode;
                    if (startIsEntry && !(port.node is EffectGraphNode && port.direction == Direction.Input))
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
                if (_window._effectConfig == null)
                {
                    return;
                }

                evt.menu.AppendAction("创建节点/Sequence", _ => _window.CreateStandaloneNode(SkillEffectNodeType.Sequence));
                evt.menu.AppendAction("创建节点/Condition", _ => _window.CreateStandaloneNode(SkillEffectNodeType.Condition));
                evt.menu.AppendAction("创建节点/Action", _ => _window.CreateStandaloneNode(SkillEffectNodeType.Action));
            }
        }

        private sealed class EntryGraphNode : Node
        {
            public Port OutputPort { get; }

            public EntryGraphNode(Rect rect)
            {
                viewDataKey = EntryNodeId;
                title = "Entry";
                SetPosition(rect);
                capabilities &= ~(Capabilities.Deletable | Capabilities.Movable | Capabilities.Copiable | Capabilities.Groupable | Capabilities.Ascendable | Capabilities.Renamable);

                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                if (OutputPort != null)
                {
                    OutputPort.portName = "Out";
                    outputContainer.Add(OutputPort);
                }

                mainContainer.style.backgroundColor = new Color(0.32f, 0.32f, 0.32f);
                Label detail = new Label("效果树入口");
                detail.style.whiteSpace = WhiteSpace.Normal;
                detail.style.color = Color.white;
                detail.style.marginLeft = 6f;
                detail.style.marginRight = 6f;
                extensionContainer.Add(detail);

                RefreshExpandedState();
                RefreshPorts();
            }
        }

        private sealed class EffectGraphNode : Node
        {
            private readonly SkillEffectEditorWindow _window;

            public string NodeId { get; }
            public Port InputPort { get; }

            private readonly Port _sequenceOutput;
            private readonly Port _passOutput;
            private readonly Port _failOutput;

            public EffectGraphNode(SkillEffectEditorWindow window, SkillEffectNodeConfig config, Rect rect)
            {
                _window = window;
                NodeId = config.NodeId;
                title = window.GetNodeTitle(config);
                SetPosition(rect);

                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
                if (InputPort != null)
                {
                    InputPort.portName = "In";
                    inputContainer.Add(InputPort);
                }

                switch (config.NodeType)
                {
                    case SkillEffectNodeType.Sequence:
                        _sequenceOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                        _sequenceOutput.portName = "Children";
                        outputContainer.Add(_sequenceOutput);
                        mainContainer.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f);
                        break;
                    case SkillEffectNodeType.Condition:
                        _passOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                        _passOutput.portName = "通过";
                        _passOutput.userData = PassPortKey;
                        outputContainer.Add(_passOutput);

                        _failOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                        _failOutput.portName = "失败";
                        _failOutput.userData = FailPortKey;
                        outputContainer.Add(_failOutput);
                        mainContainer.style.backgroundColor = new Color(0.18f, 0.42f, 0.72f);
                        break;
                    case SkillEffectNodeType.Action:
                        mainContainer.style.backgroundColor = new Color(0.82f, 0.55f, 0.20f);
                        break;
                }

                Label detail = new Label(window.GetNodeDetail(config));
                detail.style.whiteSpace = WhiteSpace.Normal;
                detail.style.color = Color.white;
                detail.style.marginLeft = 6f;
                detail.style.marginRight = 6f;
                extensionContainer.Add(detail);

                RefreshExpandedState();
                RefreshPorts();
            }

            public Port GetOutputPort(int childIndex)
            {
                if (_sequenceOutput != null)
                {
                    return _sequenceOutput;
                }

                if (childIndex == 0)
                {
                    return _passOutput;
                }

                if (childIndex == 1)
                {
                    return _failOutput;
                }

                return null;
            }

            public override void OnSelected()
            {
                base.OnSelected();
                _window.SelectNode(NodeId);
            }
        }
    }
}