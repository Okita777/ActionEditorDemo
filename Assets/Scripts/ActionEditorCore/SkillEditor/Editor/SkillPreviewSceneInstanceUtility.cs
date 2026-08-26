using System;
using ActionEditor.CameraSystem;
using ActionEditor.InputSystem;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using AsiSkillEditor.RunTime;
using SkillEditor.Preview;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkillEditor.Editor
{
    internal static class SkillPreviewSceneInstanceUtility
    {
        private const string PreviewRootName = "__SkillEditorPreviewUnit__";
        private const string PreviewCameraRootName = "__SkillEditorPreviewCamera__";
        private const string PreviewWeaponRootPrefix = "__SkillEditorPreviewWeapon__";
        private const string RootMountPointName = "Root";
        private const string MainCameraMountPointName = "MainCamera";

        public static GameObject GetCurrentInstance()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == PreviewRootName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        public static GameObject CreateOrReplace(GameObject prefab)
        {
            return CreateOrReplace(prefab, string.Empty);
        }

        public static GameObject CreateOrReplace(GameObject prefab, string cameraPrefabPath)
        {
            if (prefab == null)
            {
                return null;
            }

            RemoveCurrentInstances();

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, activeScene) as GameObject;
            if (instance == null)
            {
                return null;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(instance))
            {
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create Skill Preview Unit");
            instance.name = PreviewRootName;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            AttachPreviewWeapons(instance);
            if (!string.IsNullOrEmpty(cameraPrefabPath))
            {
                CreateCameraInstance(instance, cameraPrefabPath, activeScene);
            }

            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
            EditorSceneManager.MarkSceneDirty(activeScene);
            SceneView.RepaintAll();
            return instance;
        }

        public static GameObject GetCurrentPrimaryWeaponInstance()
        {
            GameObject unitInstance = GetCurrentInstance();
            if (unitInstance == null)
            {
                return null;
            }

            Transform weaponTransform = FindChildByPrefix(unitInstance.transform, PreviewWeaponRootPrefix);
            return weaponTransform != null ? weaponTransform.gameObject : null;
        }

        public static GameObject GetCurrentWeaponInstance(int bindingIndex)
        {
            if (bindingIndex < 0)
            {
                return null;
            }

            GameObject unitInstance = GetCurrentInstance();
            if (unitInstance == null)
            {
                return null;
            }

            Transform weaponTransform = FindChildByExactName(unitInstance.transform, $"{PreviewWeaponRootPrefix}{bindingIndex}");
            return weaponTransform != null ? weaponTransform.gameObject : null;
        }

        public static bool TryApplyWeaponBindingPose(int bindingIndex, PreviewWeaponBinding binding)
        {
            GameObject weaponInstance = GetCurrentWeaponInstance(bindingIndex);
            if (weaponInstance == null || binding == null)
            {
                return false;
            }

            Undo.RecordObject(weaponInstance.transform, "Apply Preview Weapon Pose");
            weaponInstance.transform.localPosition = binding.LocalPosition;
            weaponInstance.transform.localRotation = Quaternion.Euler(binding.LocalRotation);
            SceneView.RepaintAll();
            return true;
        }

        public static bool TryCaptureWeaponBindingPose(int bindingIndex, PreviewWeaponBinding binding)
        {
            GameObject weaponInstance = GetCurrentWeaponInstance(bindingIndex);
            if (weaponInstance == null || binding == null)
            {
                return false;
            }

            binding.LocalPosition = weaponInstance.transform.localPosition;
            binding.LocalRotation = weaponInstance.transform.localEulerAngles;
            return true;
        }

        public static void RemoveCurrentInstance()
        {
            RemoveCurrentInstances();
        }

        private static void RemoveCurrentUnitInstance()
        {
            GameObject instance = GetCurrentInstance();
            if (instance == null)
            {
                return;
            }

            Scene activeScene = instance.scene;
            Undo.DestroyObjectImmediate(instance);
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            SceneView.RepaintAll();
        }

        public static void RemoveCurrentInstances()
        {
            RemoveCurrentUnitInstance();
            GameObject cameraInstance = FindSceneRoot(PreviewCameraRootName);
            if (cameraInstance == null)
            {
                return;
            }

            Scene activeScene = cameraInstance.scene;
            Undo.DestroyObjectImmediate(cameraInstance);
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            SceneView.RepaintAll();
        }

        public static bool ValidateCameraConfiguration(UnitConfig unitConfig, GameObject unitPrefab, out string errorMessage)
        {
            if (unitConfig == null || string.IsNullOrEmpty(unitConfig.CameraResourcePath))
            {
                errorMessage = "当前 Unit 还没有配置 Camera Prefab。";
                return false;
            }

            GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(unitConfig.CameraResourcePath);
            if (cameraPrefab == null)
            {
                errorMessage = $"未找到 Camera Prefab: {unitConfig.CameraResourcePath}";
                return false;
            }

            GameplayCameraRigController rigController = cameraPrefab.GetComponent<GameplayCameraRigController>() ??
                cameraPrefab.GetComponentInChildren<GameplayCameraRigController>(true);
            if (rigController == null)
            {
                errorMessage = "Camera Prefab 缺少 GameplayCameraRigController。";
                return false;
            }

            if (!rigController.ValidateConfiguration(out errorMessage))
            {
                return false;
            }

            Camera outputCamera = Camera.main;
            if (outputCamera == null)
            {
                errorMessage = "当前场景缺少标记为 MainCamera 的真实 Camera。";
                return false;
            }

            if (outputCamera.GetComponent<CinemachineBrain>() == null)
            {
                errorMessage = "当前场景的 Main Camera 缺少 CinemachineBrain。";
                return false;
            }

            GameUnit gameUnit = unitPrefab != null
                ? unitPrefab.GetComponent<GameUnit>() ?? unitPrefab.GetComponentInChildren<GameUnit>(true)
                : null;
            if (!ValidateRequiredCharacterMountPoint(gameUnit, RootMountPointName, out errorMessage) ||
                !ValidateRequiredCharacterMountPoint(gameUnit, MainCameraMountPointName, out errorMessage))
            {
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static void CreateCameraInstance(GameObject unitInstance, string cameraPrefabPath, Scene activeScene)
        {
            GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cameraPrefabPath);
            if (cameraPrefab == null || unitInstance == null)
            {
                return;
            }

            GameObject cameraInstance = PrefabUtility.InstantiatePrefab(cameraPrefab, activeScene) as GameObject;
            if (cameraInstance == null)
            {
                return;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(cameraInstance))
            {
                PrefabUtility.UnpackPrefabInstance(cameraInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            Undo.RegisterCreatedObjectUndo(cameraInstance, "Create Skill Preview Camera");
            cameraInstance.name = PreviewCameraRootName;
            cameraInstance.transform.position = Vector3.zero;
            cameraInstance.transform.rotation = Quaternion.identity;

            GameUnit gameUnit = unitInstance.GetComponent<GameUnit>() ?? unitInstance.GetComponentInChildren<GameUnit>(true);
            TryResolveCharacterMountPoint(gameUnit, MainCameraMountPointName, out Transform mainCameraAnchor);
            CharacterInputDriver inputDriver = unitInstance.GetComponent<CharacterInputDriver>() ??
                unitInstance.GetComponentInChildren<CharacterInputDriver>(true);
            GameplayCameraRigController rigController = cameraInstance.GetComponent<GameplayCameraRigController>() ??
                cameraInstance.GetComponentInChildren<GameplayCameraRigController>(true);
            Camera outputCamera = Camera.main;
            if (outputCamera == null)
            {
                Debug.LogError("SkillEditor Camera Apply 失败：场景缺少标记为 MainCamera 的真实 Camera。");
                return;
            }

            if (outputCamera.GetComponent<CinemachineBrain>() == null)
            {
                Debug.LogError("SkillEditor Camera Apply 失败：Main Camera 缺少 CinemachineBrain。");
                return;
            }

            if (rigController == null || !rigController.Bind(mainCameraAnchor, inputDriver, outputCamera))
            {
                Debug.LogError("SkillEditor Camera Apply 失败：相机 Rig 无法绑定 GameUnit.MainCamera 挂点。", cameraInstance);
                return;
            }

            EditorUtility.SetDirty(rigController);

            ActionEditor.CharacterMotion.InputMotionSource inputMotionSource =
                unitInstance.GetComponent<ActionEditor.CharacterMotion.InputMotionSource>() ??
                unitInstance.GetComponentInChildren<ActionEditor.CharacterMotion.InputMotionSource>(true);
            inputMotionSource?.SetCameraBasisProvider(rigController);
            if (inputMotionSource != null)
            {
                EditorUtility.SetDirty(inputMotionSource);
            }
        }

        private static GameObject FindSceneRoot(string rootName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == rootName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static bool TryResolveCharacterMountPoint(
            GameUnit gameUnit,
            string socketName,
            out Transform mountTransform)
        {
            mountTransform = null;
            if (gameUnit == null || gameUnit.MountPoints == null)
            {
                return false;
            }

            for (int i = 0; i < gameUnit.MountPoints.Count; i++)
            {
                PreviewMountPoint mountPoint = gameUnit.MountPoints[i];
                if (mountPoint == null || mountPoint.MountTransform == null ||
                    !string.Equals(mountPoint.SocketName, socketName, StringComparison.Ordinal))
                {
                    continue;
                }

                mountTransform = mountPoint.MountTransform;
                return true;
            }

            return false;
        }

        private static bool ValidateRequiredCharacterMountPoint(
            GameUnit gameUnit,
            string socketName,
            out string errorMessage)
        {
            int validCount = 0;
            if (gameUnit != null && gameUnit.MountPoints != null)
            {
                for (int i = 0; i < gameUnit.MountPoints.Count; i++)
                {
                    PreviewMountPoint mountPoint = gameUnit.MountPoints[i];
                    if (mountPoint != null && mountPoint.MountTransform != null &&
                        string.Equals(mountPoint.SocketName, socketName, StringComparison.Ordinal))
                    {
                        validCount++;
                    }
                }
            }

            if (validCount == 1)
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = validCount == 0
                ? $"角色 Prefab 的 GameUnit 未配置有效的 {socketName} 挂点。"
                : $"角色 Prefab 的 GameUnit 存在多个 {socketName} 挂点；同名挂点只能配置一个。";
            return false;
        }

        private static void AttachPreviewWeapons(GameObject unitInstance)
        {
            if (unitInstance == null)
            {
                return;
            }

            GameUnit unitConfig = unitInstance.GetComponent<GameUnit>() ?? unitInstance.GetComponentInChildren<GameUnit>(true);
            if (unitConfig == null || unitConfig.WeaponBindings == null || unitConfig.WeaponBindings.Count == 0)
            {
                return;
            }

            SkillPreviewWeaponSettingsData[] previewWeapons = SkillPreviewUnitSettings.LoadPreviewWeapons();
            for (int i = 0; i < previewWeapons.Length; i++)
            {
                SkillPreviewWeaponSettingsData previewWeapon = previewWeapons[i];
                if (previewWeapon == null || string.IsNullOrEmpty(previewWeapon.WeaponPrefabPath))
                {
                    continue;
                }

                GameObject previewWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(previewWeapon.WeaponPrefabPath);
                if (previewWeaponPrefab == null)
                {
                    continue;
                }

                PreviewWeaponBinding binding = ResolveSelectedWeaponBinding(unitConfig, previewWeapon);
                if (binding == null)
                {
                    continue;
                }

                Transform equipSocket = ResolveMountPoint(unitConfig, binding.EquipSocketName) ?? unitInstance.transform;
                GameObject weaponInstance = PrefabUtility.InstantiatePrefab(previewWeaponPrefab, unitInstance.scene) as GameObject;
                if (weaponInstance == null)
                {
                    continue;
                }

                if (PrefabUtility.IsPartOfPrefabInstance(weaponInstance))
                {
                    PrefabUtility.UnpackPrefabInstance(weaponInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                Undo.RegisterCreatedObjectUndo(weaponInstance, "Create Skill Preview Weapon");
                weaponInstance.name = $"{PreviewWeaponRootPrefix}{i}";
                weaponInstance.transform.SetParent(equipSocket, false);
                weaponInstance.transform.localPosition = binding.LocalPosition;
                weaponInstance.transform.localRotation = Quaternion.Euler(binding.LocalRotation);
            }
        }

        private static PreviewWeaponBinding ResolveSelectedWeaponBinding(GameUnit config, SkillPreviewWeaponSettingsData previewWeapon)
        {
            if (config == null || config.WeaponBindings == null || previewWeapon == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(previewWeapon.WeaponBindingName))
            {
                for (int i = 0; i < config.WeaponBindings.Count; i++)
                {
                    PreviewWeaponBinding binding = config.WeaponBindings[i];
                    if (binding == null)
                    {
                        continue;
                    }

                    string displayName = string.IsNullOrEmpty(binding.DisplayName) ? $"武器挂载 {i + 1}" : binding.DisplayName;
                    if (string.Equals(displayName, previewWeapon.WeaponBindingName, StringComparison.Ordinal))
                    {
                        return binding;
                    }
                }
            }

            for (int i = 0; i < config.WeaponBindings.Count; i++)
            {
                PreviewWeaponBinding binding = config.WeaponBindings[i];
                if (binding != null && binding.WeaponType == previewWeapon.WeaponType)
                {
                    return binding;
                }
            }

            return null;
        }

        private static Transform ResolveMountPoint(GameUnit config, string socketName)
        {
            if (config == null || config.MountPoints == null || string.IsNullOrEmpty(socketName))
            {
                return null;
            }

            for (int i = 0; i < config.MountPoints.Count; i++)
            {
                PreviewMountPoint mountPoint = config.MountPoints[i];
                if (mountPoint == null || mountPoint.MountTransform == null)
                {
                    continue;
                }

                if (string.Equals(mountPoint.SocketName, socketName))
                {
                    return mountPoint.MountTransform;
                }
            }

            return null;
        }

        private static Transform FindChildByPrefix(Transform root, string prefix)
        {
            if (root == null || string.IsNullOrEmpty(prefix))
            {
                return null;
            }

            if (root.name.StartsWith(prefix))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChildByPrefix(root.GetChild(i), prefix);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform FindChildByExactName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            if (string.Equals(root.name, targetName))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChildByExactName(root.GetChild(i), targetName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
