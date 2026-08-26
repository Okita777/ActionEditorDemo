using System.Collections.Generic;
using AsiSkillEditor.RunTime;
using SkillEditor.Preview;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    internal static class UnitConfigEditorUtility
    {
        public static void Draw(UnitConfig config)
        {
            if (config == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical("box");
            config.UnitId = EditorGUILayout.TextField("unitId", config.UnitId);
            config.DisplayName = EditorGUILayout.TextField("displayName", config.DisplayName);

            GameObject currentPrefab = string.IsNullOrEmpty(config.PrefabAssetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(config.PrefabAssetPath);
            GameObject nextPrefab = EditorGUILayout.ObjectField("prefab", currentPrefab, typeof(GameObject), false) as GameObject;
            if (nextPrefab != currentPrefab)
            {
                config.PrefabAssetPath = nextPrefab == null ? string.Empty : AssetDatabase.GetAssetPath(nextPrefab);
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(currentPrefab == null))
            {
                if (GUILayout.Button("定位 Prefab"))
                {
                    Selection.activeObject = currentPrefab;
                    EditorGUIUtility.PingObject(currentPrefab);
                }

                if (GUILayout.Button("打开挂点配置"))
                {
                    GameUnit previewConfig = currentPrefab != null ? currentPrefab.GetComponent<GameUnit>() : null;
                    if (previewConfig != null)
                    {
                        Selection.activeObject = previewConfig;
                        EditorGUIUtility.PingObject(previewConfig);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            DefaultAsset currentAnimationFolder = string.IsNullOrEmpty(config.AnimationDirectory)
                ? null
                : AssetDatabase.LoadAssetAtPath<DefaultAsset>(config.AnimationDirectory);
            DefaultAsset nextAnimationFolder = EditorGUILayout.ObjectField("animationDir", currentAnimationFolder, typeof(DefaultAsset), false) as DefaultAsset;
            if (nextAnimationFolder != currentAnimationFolder)
            {
                config.AnimationDirectory = nextAnimationFolder == null ? string.Empty : AssetDatabase.GetAssetPath(nextAnimationFolder);
            }

            GameObject currentCameraPrefab = string.IsNullOrEmpty(config.CameraResourcePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(config.CameraResourcePath);
            GameObject nextCameraPrefab = EditorGUILayout.ObjectField("cameraPrefab", currentCameraPrefab, typeof(GameObject), false) as GameObject;
            if (nextCameraPrefab != currentCameraPrefab)
            {
                config.CameraResourcePath = nextCameraPrefab == null ? string.Empty : AssetDatabase.GetAssetPath(nextCameraPrefab);
            }

            DrawLayerDefaultStateFields(config);
            EditorGUILayout.EndVertical();

            DrawHardLockFields(config);
            EditorGUILayout.Space(8f);
            DrawLocomotionFields(config);
            EditorGUILayout.Space(8f);
            DrawRecoveryCancelFields(config);
            EditorGUILayout.Space(8f);
            DrawAnimationLayerFields(config);
            EditorGUILayout.Space(8f);
            DrawUnitActiveSkillSlots(config);
            EditorGUILayout.Space(8f);
            DrawUnitPassiveSkillSlots(config);
        }

        private static void DrawHardLockFields(UnitConfig config)
        {
            config.HardLock ??= new UnitHardLockConfig();
            UnitHardLockConfig hardLock = config.HardLock;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("纯视角硬锁定 / Camera Only", EditorStyles.boldLabel);
            hardLock.SearchRadius = Mathf.Max(0f, EditorGUILayout.FloatField("搜索半径", hardLock.SearchRadius));
            hardLock.HorizontalFanAngle = EditorGUILayout.Slider("水平扇形总角度", hardLock.HorizontalFanAngle, 0f, 360f);
            hardLock.ViewPivotHeightOffset = EditorGUILayout.FloatField("锁定视点高度偏移", hardLock.ViewPivotHeightOffset);
            hardLock.TargetLayers = EditorGUILayout.MaskField("目标层", hardLock.TargetLayers, GetLayerNames());
            hardLock.ObstacleLayers = EditorGUILayout.MaskField("障碍层", hardLock.ObstacleLayers, GetLayerNames());
            hardLock.DistanceWeight = Mathf.Max(0f, EditorGUILayout.FloatField("距离权重", hardLock.DistanceWeight));
            hardLock.AngleWeight = Mathf.Max(0f, EditorGUILayout.FloatField("角度权重", hardLock.AngleWeight));
            hardLock.OcclusionUnlockDelay = Mathf.Max(0f, EditorGUILayout.FloatField("遮挡解锁延迟", hardLock.OcclusionUnlockDelay));
            hardLock.UnlockRadius = Mathf.Max(0f, EditorGUILayout.FloatField("解锁半径", hardLock.UnlockRadius));
            hardLock.ToggleAction = InputActionEditorUtility.DrawActionPopup("锁定/解除", hardLock.ToggleAction);
            hardLock.SwitchLeftAction = InputActionEditorUtility.DrawActionPopup("切换左侧", hardLock.SwitchLeftAction);
            hardLock.SwitchRightAction = InputActionEditorUtility.DrawActionPopup("切换右侧", hardLock.SwitchRightAction);
            hardLock.SwitchFartherAction = InputActionEditorUtility.DrawActionPopup("切换更远", hardLock.SwitchFartherAction);
            hardLock.SwitchNearerAction = InputActionEditorUtility.DrawActionPopup("切换更近", hardLock.SwitchNearerAction);
            EditorGUILayout.HelpBox("硬锁定只控制相机，不读取或修改角色控制器、旋转、移动策略与状态机。", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private static void DrawLocomotionFields(UnitConfig config)
        {
            config.Locomotion ??= new UnitLocomotionConfig();
            UnitLocomotionConfig locomotion = config.Locomotion;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("基础移动 / Locomotion", EditorStyles.boldLabel);
            locomotion.MaxMoveSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("最大移动速度", locomotion.MaxMoveSpeed));
            locomotion.GroundAcceleration = Mathf.Max(0f, EditorGUILayout.FloatField("地面加速度", locomotion.GroundAcceleration));
            locomotion.GroundDeceleration = Mathf.Max(0f, EditorGUILayout.FloatField("松手减速度", locomotion.GroundDeceleration));
            locomotion.DirectionChangeAcceleration = Mathf.Max(0f, EditorGUILayout.FloatField("变向加速度", locomotion.DirectionChangeAcceleration));
            locomotion.HardTurnAngle = EditorGUILayout.Slider("急转判定角度", locomotion.HardTurnAngle, 45f, 180f);
            locomotion.HardTurnSpeedRetention = EditorGUILayout.Slider("急转速度保留", locomotion.HardTurnSpeedRetention, 0f, 1f);
            locomotion.TurnSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("普通转向速度", locomotion.TurnSpeed));
            locomotion.AirTurnSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("空中转向速度", locomotion.AirTurnSpeed));
            locomotion.HardTurnSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("急转转向速度", locomotion.HardTurnSpeed));
            locomotion.SnapFacingOnHardTurn = EditorGUILayout.Toggle("急转立即朝向输入", locomotion.SnapFacingOnHardTurn);
            locomotion.StableGroundLayers = EditorGUILayout.MaskField("稳定地面层", locomotion.StableGroundLayers, GetLayerNames());
            locomotion.AirAcceleration = Mathf.Max(0f, EditorGUILayout.FloatField("空中加速度", locomotion.AirAcceleration));
            locomotion.MaxAirSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("最大空中速度", locomotion.MaxAirSpeed));
            locomotion.EnableGravity = EditorGUILayout.Toggle("启用重力", locomotion.EnableGravity);
            locomotion.Gravity = Mathf.Max(0f, EditorGUILayout.FloatField("重力加速度", locomotion.Gravity));
            locomotion.MaxFallSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("最大下落速度", locomotion.MaxFallSpeed));
            locomotion.ExternalVelocityDrag = Mathf.Max(0f, EditorGUILayout.FloatField("外力水平衰减", locomotion.ExternalVelocityDrag));
            EditorGUILayout.HelpBox("急转会先清除旧方向和横向速度，再沿新输入方向重新加速；速度保留为 0 时折返最干脆。", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private static void DrawRecoveryCancelFields(UnitConfig config)
        {
            config.RecoveryCancel ??= new UnitRecoveryCancelPolicy();
            UnitRecoveryCancelPolicy policy = config.RecoveryCancel;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("技能后摇取消 / Recovery Cancel", EditorStyles.boldLabel);
            policy.AllowSkillCancel = EditorGUILayout.Toggle("允许技能取消", policy.AllowSkillCancel);
            policy.AllowMoveCancel = EditorGUILayout.Toggle("允许移动取消", policy.AllowMoveCancel);
            policy.AllowHitReactionCancel = EditorGUILayout.Toggle("允许受击取消", policy.AllowHitReactionCancel);
            policy.AllowForcedCancel = EditorGUILayout.Toggle("允许强制取消", policy.AllowForcedCancel);
            EditorGUILayout.HelpBox("取消规则作用于该单位的所有 Recovery 状态；无需在每个技能后摇中重复添加 Interrupt 轨道。", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private static string[] GetLayerNames()
        {
            string[] names = new string[32];
            for (int i = 0; i < names.Length; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                names[i] = string.IsNullOrEmpty(layerName) ? $"Layer {i}" : layerName;
            }

            return names;
        }

        private static void DrawUnitActiveSkillSlots(UnitConfig config)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("主动技能槽位", EditorStyles.boldLabel);
            config.ActiveSkillSlots ??= new List<UnitActiveSkillSlotConfig>();

            List<SkillResourceFileEntry> allSkillEntries = SkillResourceRepository.LoadSkills(config.UnitId);
            List<SkillResourceFileEntry> skillEntries = FilterUnitSkillEntriesByCategory(allSkillEntries, SkillCastCategory.Active, config.ActiveSkillSlots, null);
            for (int i = 0; i < config.ActiveSkillSlots.Count; i++)
            {
                UnitActiveSkillSlotConfig slot = config.ActiveSkillSlots[i] ?? new UnitActiveSkillSlotConfig();
                config.ActiveSkillSlots[i] = slot;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                slot.DisplayName = EditorGUILayout.TextField("名称", slot.DisplayName);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.ActiveSkillSlots.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                slot.SlotIndex = Mathf.Max(1, EditorGUILayout.IntField("槽位编号", slot.SlotIndex));
                slot.ActionName = InputActionEditorUtility.DrawActionPopup("输入动作", slot.ActionName);
                slot.SkillId = DrawSkillPopup("技能", skillEntries, slot.SkillId);
                DrawSkillSummary(allSkillEntries, slot.SkillId, SkillCastCategory.Active);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加主动技能槽", GUILayout.Height(28f)))
            {
                config.ActiveSkillSlots.Add(new UnitActiveSkillSlotConfig
                {
                    SlotIndex = config.ActiveSkillSlots.Count + 1,
                    DisplayName = $"主动技能槽{config.ActiveSkillSlots.Count + 1}",
                });
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawAnimationLayerFields(UnitConfig config)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("animationLayers", EditorStyles.boldLabel);
            config.AnimationLayers ??= new List<UnitAnimationLayerConfig>();

            for (int i = 0; i < config.AnimationLayers.Count; i++)
            {
                UnitAnimationLayerConfig layerConfig = config.AnimationLayers[i] ?? new UnitAnimationLayerConfig();
                config.AnimationLayers[i] = layerConfig;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                layerConfig.Layer = (AnimationLayerType)EditorGUILayout.EnumPopup("layer", layerConfig.Layer);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.AnimationLayers.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                layerConfig.AnimancerLayerIndex = Mathf.Max(0, EditorGUILayout.IntField("animancerLayerIndex", layerConfig.AnimancerLayerIndex));
                layerConfig.BlendMode = (AsiSkillEditor.RunTime.AnimationBlendMode)EditorGUILayout.EnumPopup("blendMode", layerConfig.BlendMode);
                layerConfig.DefaultWeight = Mathf.Clamp01(EditorGUILayout.Slider("defaultWeight", layerConfig.DefaultWeight, 0f, 1f));

                AvatarMask currentMask = string.IsNullOrEmpty(layerConfig.AvatarMaskAssetPath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<AvatarMask>(layerConfig.AvatarMaskAssetPath);
                AvatarMask nextMask = EditorGUILayout.ObjectField("avatarMask", currentMask, typeof(AvatarMask), false) as AvatarMask;
                if (nextMask != currentMask)
                {
                    layerConfig.AvatarMaskAssetPath = nextMask == null ? string.Empty : AssetDatabase.GetAssetPath(nextMask);
                }

                if (nextMask != null && !layerConfig.AvatarMaskAssetPath.Replace('\\', '/').Contains("/Resources/"))
                {
                    EditorGUILayout.HelpBox("AvatarMask 必须位于 Resources 目录，正式运行时才能加载。", MessageType.Error);
                }

                if (layerConfig.Layer == AnimationLayerType.UpperBody && nextMask == null)
                {
                    EditorGUILayout.HelpBox("UpperBody 层必须配置 AvatarMask。", MessageType.Error);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加动画层", GUILayout.Height(26f)))
            {
                config.AnimationLayers.Add(new UnitAnimationLayerConfig());
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawUnitPassiveSkillSlots(UnitConfig config)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("被动技能槽位", EditorStyles.boldLabel);
            config.PassiveSkillSlots ??= new List<UnitPassiveSkillSlotConfig>();

            List<SkillResourceFileEntry> allSkillEntries = SkillResourceRepository.LoadSkills(config.UnitId);
            List<SkillResourceFileEntry> skillEntries = FilterUnitSkillEntriesByCategory(allSkillEntries, SkillCastCategory.Passive, null, config.PassiveSkillSlots);
            for (int i = 0; i < config.PassiveSkillSlots.Count; i++)
            {
                UnitPassiveSkillSlotConfig slot = config.PassiveSkillSlots[i] ?? new UnitPassiveSkillSlotConfig();
                config.PassiveSkillSlots[i] = slot;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                slot.DisplayName = EditorGUILayout.TextField("名称", slot.DisplayName);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    config.PassiveSkillSlots.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                slot.SlotIndex = Mathf.Max(1, EditorGUILayout.IntField("槽位编号", slot.SlotIndex));
                slot.SkillId = DrawSkillPopup("技能", skillEntries, slot.SkillId);
                DrawSkillSummary(allSkillEntries, slot.SkillId, SkillCastCategory.Passive);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加被动技能槽", GUILayout.Height(28f)))
            {
                config.PassiveSkillSlots.Add(new UnitPassiveSkillSlotConfig
                {
                    SlotIndex = config.PassiveSkillSlots.Count + 1,
                    DisplayName = $"被动技能槽{config.PassiveSkillSlots.Count + 1}",
                });
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawLayerDefaultStateFields(UnitConfig config)
        {
            EditorGUILayout.LabelField("layerDefaultStates", EditorStyles.boldLabel);
            DrawLayerDefaultStateField(config, StateLayerType.Locomotion, "locomotionDefaultState");
            DrawLayerDefaultStateField(config, StateLayerType.Action, "actionDefaultState");
        }

        private static void DrawLayerDefaultStateField(UnitConfig config, StateLayerType layerType, string label)
        {
            UnitLayerDefaultStateConfig layerDefaultState = GetLayerDefaultStateEntry(config, layerType);
            if (layerDefaultState == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.LabelField("<未创建配置项>");
                if (GUILayout.Button("创建", GUILayout.Width(64f)))
                {
                    AddLayerDefaultStateEntry(config, layerType);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox($"{layerType} 层缺少默认状态配置项。请手动创建后再选择默认状态。", MessageType.Error);
                return;
            }

            BuildLayerStateOptions(config.UnitId, layerType, out string[] optionLabels, out string[] optionValues);

            int currentIndex = 0;
            for (int i = 1; i < optionValues.Length; i++)
            {
                if (!string.IsNullOrEmpty(optionValues[i]) && string.Equals(layerDefaultState.DefaultStateId, optionValues[i], System.StringComparison.Ordinal))
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, optionLabels);
            string nextValue = nextIndex >= 0 && nextIndex < optionValues.Length ? optionValues[nextIndex] : string.Empty;
            if (!string.Equals(layerDefaultState.DefaultStateId, nextValue, System.StringComparison.Ordinal))
            {
                layerDefaultState.DefaultStateId = nextValue;
            }

            if (!string.IsNullOrWhiteSpace(layerDefaultState.DefaultStateId) && currentIndex == 0)
            {
                EditorGUILayout.HelpBox($"当前默认状态不属于 {layerType} 层，请重新选择同层状态。", MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(layerDefaultState.DefaultStateId))
            {
                EditorGUILayout.HelpBox($"{layerType} 层必须手动配置默认状态，未配置时运行时会阻断。", MessageType.Error);
            }
        }

        private static void BuildLayerStateOptions(string unitId, StateLayerType layerType, out string[] optionLabels, out string[] optionValues)
        {
            List<SkillResourceFileEntry> stateEntries = SkillResourceRepository.LoadStates(unitId);
            List<string> labels = new List<string> { "未设置" };
            List<string> values = new List<string> { string.Empty };

            for (int i = 0; i < (stateEntries != null ? stateEntries.Count : 0); i++)
            {
                StateConfig stateConfig = stateEntries[i] != null ? stateEntries[i].Config as StateConfig : null;
                if (stateConfig == null || stateConfig.Layer != layerType)
                {
                    continue;
                }

                string stateId = stateConfig.StateId;
                string stateName = stateConfig.StateName;
                values.Add(stateId ?? string.Empty);
                labels.Add(string.IsNullOrEmpty(stateId)
                    ? "<无效状态>"
                    : (string.IsNullOrEmpty(stateName) ? stateId : $"{stateName} ({stateId})"));
            }

            optionLabels = labels.ToArray();
            optionValues = values.ToArray();
        }

        private static void AddLayerDefaultStateEntry(UnitConfig config, StateLayerType layerType)
        {
            config.LayerDefaultStates ??= new List<UnitLayerDefaultStateConfig>();
            if (GetLayerDefaultStateEntry(config, layerType) != null)
            {
                return;
            }

            config.LayerDefaultStates.Add(new UnitLayerDefaultStateConfig
            {
                Layer = layerType,
            });
        }

        private static UnitLayerDefaultStateConfig GetLayerDefaultStateEntry(UnitConfig config, StateLayerType layerType)
        {
            if (config == null || config.LayerDefaultStates == null)
            {
                return null;
            }

            for (int i = 0; i < config.LayerDefaultStates.Count; i++)
            {
                UnitLayerDefaultStateConfig layerDefaultState = config.LayerDefaultStates[i];
                if (layerDefaultState != null && layerDefaultState.Layer == layerType)
                {
                    return layerDefaultState;
                }
            }

            return null;
        }

        private static List<SkillResourceFileEntry> FilterUnitSkillEntriesByCategory(List<SkillResourceFileEntry> allSkillEntries, SkillCastCategory expectedCategory, List<UnitActiveSkillSlotConfig> activeSlots, List<UnitPassiveSkillSlotConfig> passiveSlots)
        {
            List<SkillResourceFileEntry> results = new List<SkillResourceFileEntry>();
            HashSet<string> equippedAssets = new HashSet<string>(System.StringComparer.Ordinal);
            if (activeSlots != null)
            {
                for (int i = 0; i < activeSlots.Count; i++)
                {
                    if (activeSlots[i] != null && !string.IsNullOrEmpty(activeSlots[i].SkillId))
                    {
                        equippedAssets.Add(activeSlots[i].SkillId);
                    }
                }
            }

            if (passiveSlots != null)
            {
                for (int i = 0; i < passiveSlots.Count; i++)
                {
                    if (passiveSlots[i] != null && !string.IsNullOrEmpty(passiveSlots[i].SkillId))
                    {
                        equippedAssets.Add(passiveSlots[i].SkillId);
                    }
                }
            }

            if (allSkillEntries == null)
            {
                return results;
            }

            for (int i = 0; i < allSkillEntries.Count; i++)
            {
                SkillResourceFileEntry entry = allSkillEntries[i];
                SkillConfig skillConfig = entry != null ? entry.Config as SkillConfig : null;
                if (entry == null || skillConfig == null)
                {
                    continue;
                }

                if (skillConfig.SkillCategory == expectedCategory ||
                    equippedAssets.Contains(SkillResourceRepository.GetSkillRuntimeId(entry)) ||
                    equippedAssets.Contains(skillConfig.SkillId) ||
                    equippedAssets.Contains(entry.BaseName))
                {
                    results.Add(entry);
                }
            }

            return results;
        }

        private static string DrawSkillPopup(string label, List<SkillResourceFileEntry> skillEntries, string currentSkillId)
        {
            if (skillEntries == null || skillEntries.Count == 0)
            {
                EditorGUILayout.LabelField(label, "<暂无 Skill 资源>");
                return string.Empty;
            }

            string[] options = new string[skillEntries.Count + 1];
            options[0] = "<未装配>";
            int currentIndex = 0;
            for (int i = 0; i < skillEntries.Count; i++)
            {
                SkillResourceFileEntry entry = skillEntries[i];
                SkillConfig skillConfig = entry.Config as SkillConfig;
                string displayName = SkillResourceRepository.GetDisplayName(entry);
                options[i + 1] = string.IsNullOrEmpty(skillConfig?.SkillId) ? displayName : $"{displayName} ({skillConfig.SkillId})";
                if (SkillResourceRepository.IsMatchingSkillReference(entry, currentSkillId))
                {
                    currentIndex = i + 1;
                }
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, options);
            if (nextIndex <= 0)
            {
                return string.Empty;
            }

            SkillConfig selectedConfig = skillEntries[nextIndex - 1].Config as SkillConfig;
            string runtimeSkillId = SkillResourceRepository.GetSkillRuntimeId(skillEntries[nextIndex - 1]);
            return !string.IsNullOrEmpty(runtimeSkillId)
                ? runtimeSkillId
                : (!string.IsNullOrEmpty(selectedConfig?.SkillId) ? selectedConfig.SkillId : skillEntries[nextIndex - 1].BaseName);
        }

        private static void DrawSkillSummary(List<SkillResourceFileEntry> skillEntries, string skillId, SkillCastCategory expectedCategory)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                EditorGUILayout.HelpBox("当前槽位还没有装配 Skill。", MessageType.Info);
                return;
            }

            SkillConfig skillConfig = FindSkillConfig(skillEntries, skillId);
            if (skillConfig == null)
            {
                EditorGUILayout.HelpBox($"未找到 Skill 资源: {skillId}", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("skillId", skillConfig.SkillId, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("skillType", skillConfig.SkillCategory.ToString(), EditorStyles.miniLabel);
            if (skillConfig.SkillCategory != expectedCategory)
            {
                EditorGUILayout.HelpBox($"该 Skill 当前被标记为 {skillConfig.SkillCategory}，与当前槽位期望的 {expectedCategory} 不一致。", MessageType.Warning);
            }
        }

        private static SkillConfig FindSkillConfig(List<SkillResourceFileEntry> skillEntries, string skillReference)
        {
            if (skillEntries == null || string.IsNullOrEmpty(skillReference))
            {
                return null;
            }

            for (int i = 0; i < skillEntries.Count; i++)
            {
                if (SkillResourceRepository.IsMatchingSkillReference(skillEntries[i], skillReference))
                {
                    return skillEntries[i].Config as SkillConfig;
                }
            }

            return null;
        }
    }
}