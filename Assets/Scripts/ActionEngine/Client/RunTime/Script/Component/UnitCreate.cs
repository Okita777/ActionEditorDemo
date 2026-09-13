using AsiActionEngine.RunTime;
using System.Collections;
using UnityEngine;
#if GeNa_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
#if Addressables
using UnityEngine.AddressableAssets;
#endif

namespace AsiTimeLine.RunTime
{
    public class UnitCreate : MonoBehaviour
    {
        public int UnitWarpID;
        [EditorProperty("目标单位(Unit)", EditorPropertyType.EEPT_ListUnit)]
        public int DisUnitWarpID
        {
            get { return UnitWarpID; }
            set { UnitWarpID = value; }
        }
        public bool IsPlayer = false;

        [Header("端类型")]public ERuntimeDataChannel Channel = ERuntimeDataChannel.Local;

        [SerializeField, Min(1)]
        private long mNavigationStableSourceId = 1;

        public Transform lightGroupRoot;

#if UNITY_EDITOR
        private void Reset()
        {
            mNavigationStableSourceId =
                NavigationStableUnitIdentity.CreateAuthoringSourceId();
        }
#endif

        private void Awake()
        {
#if Addressables
            StartCoroutine(InitializeGame());
            return;
#endif
            LoadStart();
        }

#if Addressables

        IEnumerator InitializeGame()
        {
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            LoadStart();

            yield return new WaitForEndOfFrame();
        }
#endif
        private void LoadStart()
        {
            if (mNavigationStableSourceId <= 0L)
            {
                throw new System.InvalidOperationException(
                    $"UnitCreate requires Navigation Stable Source ID: {name}, configuredChannel={Channel}.");
            }

            ActionEngineManager manager = ActionEngineManager_Input.Instance.CreateGameManager();
            ERuntimeDataChannel runtimeChannel = manager.ResolveRuntimeChannel(Channel);
            bool isServerUnit = runtimeChannel == ERuntimeDataChannel.Server;
            ulong navigationStableUnitId = BuildAuthoringNavigationStableUnitId(
                runtimeChannel);
            EngineDebug.Log($"[UnitCreate] LoadStart UnitWarpID=[{UnitWarpID}] Channel=[{runtimeChannel}] IsPlayer=[{IsPlayer}]");
            ActionEngineManager_Unit.Instance.CreateIdentifiedUnit(
                UnitWarpID,
                navigationStableUnitId,
                (TargetUnit _target) =>
            {
                ActionEngine_Unit _unit = _target.GetUnit();
                if (_unit is null) return;
                _unit.Channel = runtimeChannel;
                _unit.gameObject.name += $"---[{transform.GetSiblingIndex()}]";
                // EngineDebug.Log("创建角色了");
                _unit.transform.SetPositionAndRotation(transform.position, transform.rotation);
                _unit.ActionStateMachine.SetMouseXY(transform.rotation);
                if (IsPlayer)// && !isServerUnit
                {
                    ActionEngineManager_Input.Instance.ChangePlayer(_unit);

                    if (lightGroupRoot != null)
                    {
                        if (_unit.TryGetComponent(out CharacterConfig config) && config.HelpPointDic.TryGetValue(ECharacteLimbType.LightGroup, out var lightGroup))
                        {
                            var component = lightGroup.gameObject.AddComponent<ActionEngine_LightGroup>();
                            //if (lightGroupRoot is null) lightGroupRoot = new GameObject("LightGroupRoot").transform;
                            component.Init(_unit.transform, lightGroupRoot, wasAddedByCaller: true);
                        }
                    }

#if GeNa_HDRP
                    if (!isServerUnit)
                    {
                        var mainCamera = Camera.main;
                        if (mainCamera && mainCamera.TryGetComponent(out HDAdditionalCameraData data))
                        {
                            data.volumeAnchorOverride = _unit.transform;
                        }
                    }
#endif
                }
            }, EUnitType.Entity, runtimeChannel);
        }

        private ulong BuildAuthoringNavigationStableUnitId(
            ERuntimeDataChannel runtimeChannel)
        {
            if (mNavigationStableSourceId <= 0L)
            {
                throw new System.InvalidOperationException(
                    $"UnitCreate requires Navigation Stable Source ID: {name}, channel={runtimeChannel}.");
            }

            return NavigationStableUnitId.FromPositiveInt64(
                NavigationStableUnitDomain.AuthoringPreview,
                mNavigationStableSourceId);
        }

        //public void Update()
        //{
        //    if (ActionEngineManager_Input.Instance.Player is not null)
        //    {
        //        EngineDebug.LogWarning($"当前玩家为: [{ActionEngineManager_Input.Instance.Player.transform.name}]");
        //        EngineDebug.LogWarning($"输入行为: [{ActionEngineManager_Input.Instance.Player.ActionStateMachine.IsMoveInput}]");
        //    }
        //    else
        //    {
        //        EngineDebug.LogWarning($"<color=#ff0000>玩家未注册</color>");
        //    }
        //}
    }
}
