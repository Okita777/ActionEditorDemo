using AsiActionEngine.RunTime;
using System;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [Serializable]
    public class Event_AfterImageEntity : IActionEventData
    {
        [SerializeField] private string m_PrefabPath;
        [SerializeField] private string m_AnimationName;
        //[SerializeField] private byte m_SkillMaster;
        [SerializeField] private int m_PartPointType;
        [SerializeField] private EVector3 m_OffsetPos;
        [SerializeField] private EVector3 m_OffsetRot;
        [SerializeField] private EVector3 m_LocalScale = new(1, 1, 1);
        [SerializeField] private bool m_AlwaysFollow;

        #region Properties

        [EditorProperty("幻象预制体", EditorPropertyType.EEPT_GameObject, Required = true)]
        public string PrefabPath
        {
            get => m_PrefabPath;
            set => m_PrefabPath = value;
        }

        [EditorProperty("幻象动作", EditorPropertyType.EEPT_String, Required = true)]
        public string AnimationName
        {
            get => m_AnimationName;
            set => m_AnimationName = value;
        }
        
        //[EditorProperty("设定技能父级和源", EditorPropertyType.EEPT_Enum, Tooltip = "如果源不是角色单位则会继续向上级找，直到找到源角色单位",
        //    EnumNames = new[] { "自身", "自身父级", "自身的源", "自身的命中者", "自身的攻击者" }, LabelWidth = 120)]
        //public byte SkillMaster
        //{
        //    get => m_SkillMaster;
        //    set => m_SkillMaster = value;
        //}

        [EditorProperty("目标挂点", EditorPropertyType.EEPT_CharacteLimbType)]
        public int PartPointType
        {
            get => m_PartPointType;
            set => m_PartPointType = value;
        }

        [EditorProperty("位置偏移", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetPos
        {
            get => m_OffsetPos;
            set => m_OffsetPos = value;
        }

        [EditorProperty("角度偏移", EditorPropertyType.EEPT_Vector3)]
        public EVector3 OffsetRot
        {
            get => m_OffsetRot;
            set => m_OffsetRot = value;
        }

        [EditorProperty("缩放", EditorPropertyType.EEPT_Vector3)]
        public EVector3 LocalScale
        {
            get => m_LocalScale;
            set => m_LocalScale = value;
        }
        
        [EditorProperty("始终跟随", EditorPropertyType.EEPT_Bool)]
        public bool AlwaysFollow
        {
            get => m_AlwaysFollow;
            set => m_AlwaysFollow = value;
        }
        
        #endregion

        public int GetEvenType() => (int)EEvenType.EET_AfterImageEntity;
        public IActionEventData Creact() => new Event_AfterImageEntity();

        [NonSerialized] private UnitEquip m_UnitEquip;
        [NonSerialized] private Transform m_Effect;
        [NonSerialized] private bool m_Valid;
        [NonSerialized] private int m_DelayFrame=-1;
        [NonSerialized] private Animator m_ReferAnimator;

        private static readonly EquipType[] s_AllEquipTypes =
        {
            EquipType.Head, EquipType.Body, EquipType.Hand, EquipType.Leg,
            EquipType.Cape, EquipType.Hair, EquipType.Face,
            EquipType.MainWeapon, EquipType.SubWeapon,
        };

        public void Enter(ActionStatePart actionState, bool isSingle)
        {
            m_DelayFrame = -1;
            //var stateMachine = actionState.IsTem ? actionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine : actionState.ActionStateMachine;
            //var self = stateMachine.CurUnit;
            //var unitTransform = m_SkillMaster switch
            //{
            //    1 => self.GetMaster,
            //    2 => self.GetSource,
            //    3 => self.ActionStateMachine.HitUnit,
            //    4 => self.ActionStateMachine.AttackerUnit,
            //    _ => self,
            //};
            //if (!unitTransform || !unitTransform.TryGetComponent(out m_UnitEquip))
            //{
            //    return;
            //}
            //EngineDebug.LogError("设置武器Mesh_0");

            ActionStateMachine sourMachine = actionState.ActionStateMachine.CurUnit.GetSource.ActionStateMachine;
            if (!sourMachine.TryGetComponent(out UnitEquip m_selfUnitEquip, nameof(UnitEquip))) 
            {
                return;
            }
            m_UnitEquip = m_selfUnitEquip;

            ActionEngine_Prop mainWeaponProp = null;
            ActionEngine_Prop subWeaponProp = null;
            if (sourMachine.TryGetComponent(out ActionEngine_Entity entity, nameof(ActionEngine_Entity)))
            {
                const int mainWeaponSlotID = 0;
                const int subWeaponSlotID = 1;
                if (entity.mPropDic_Slot.TryGetValue(mainWeaponSlotID, out mainWeaponProp) && (!m_UnitEquip.GetMeshByType(EquipType.MainWeapon, out var renderer) || !Equals(renderer, mainWeaponProp.rendererMesh)))
                {
                    m_UnitEquip.SetMeshByType(EquipType.MainWeapon, mainWeaponProp.rendererMesh);
                }

                if (entity.mPropDic_Slot.TryGetValue(subWeaponSlotID, out subWeaponProp) && (!m_UnitEquip.GetMeshByType(EquipType.SubWeapon, out renderer) || !Equals(renderer, subWeaponProp.rendererMesh)))
                {
                    m_UnitEquip.SetMeshByType(EquipType.SubWeapon, subWeaponProp.rendererMesh);
                }
            }

            m_Valid = true;
            EngineResourcesManager.Instance.CreactObjToComponent<Transform>(m_PrefabPath, instance =>
            {

                if (!m_Valid)
                {
                    EngineResourcesManager.Instance.RemoveComponent(m_PrefabPath, instance);
                }
                else
                {
                    m_Effect = (Transform)instance;
                    ReplaceMeshes(m_Effect, mainWeaponProp, subWeaponProp);
                    //SetPositionAndRotation(m_Effect, actionState);
                    if (actionState.IsTem)
                    {
                        m_Effect.SetPositionAndRotation(actionState.Pos, actionState.Rot * Quaternion.Euler(OffsetRot.GetValue()));
                    }
                    else if (m_AlwaysFollow)
                    {
                        SetPositionAndRotation(m_Effect, actionState);
                    }
                    m_Effect.transform.localScale = m_LocalScale.GetValue();

                    if (m_Effect.TryGetComponent(out Animator animator))
                    {
                        animator.Rebind();
                        m_ReferAnimator = animator;
                        m_DelayFrame = 2;
                    }
                }
            }, 1);
        }

        public void Update(ActionStatePart actionState, ActionMachineTime actionTime)
        {
            if (!m_Effect)
            {
                return;
            }
            
            if(m_DelayFrame > 0)
            {
                m_DelayFrame--;
                if(m_DelayFrame == 0)
                {
                    m_ReferAnimator.Play(m_AnimationName, 0, 0f);
                }
            }
            else if(actionState.IsTem)
            {
                m_Effect.SetPositionAndRotation(actionState.Pos, actionState.Rot * Quaternion.Euler(OffsetRot.GetValue()));
            }
            else if (m_AlwaysFollow)
            {
                SetPositionAndRotation(m_Effect, actionState);
            }
        }

        public void Exit(ActionStatePart actionState, bool interrupt)
        {
            m_Valid = false;
            if (m_Effect)
            {
                EngineResourcesManager.Instance.RemoveComponent(m_PrefabPath, m_Effect);
            }
            m_UnitEquip = null;
        }

        private void SetPositionAndRotation(Transform effect, ActionStatePart actionState)
        {
            var stateMachine = actionState.ActionStateMachine;

            if (stateMachine.TryGetComponent(out CharacterConfig config, nameof(CharacterConfig)) && config.HelpPointDic.TryGetValue((ECharacteLimbType)m_PartPointType, out var point))
            {
                var pos = point.TransformPoint(m_OffsetPos.GetValue());
                var rot = point.rotation * Quaternion.Euler(m_OffsetRot.GetValue());
                effect.transform.SetPositionAndRotation(pos, rot);
            }
            else
            {
                var unitTr = stateMachine.CurUnit.transform;
                var pos = unitTr.TransformPoint(m_OffsetPos.GetValue());
                var rot = unitTr.rotation * Quaternion.Euler(m_OffsetRot.GetValue());
                effect.transform.SetPositionAndRotation(pos, rot);
            }
        }

        private void ReplaceMeshes(Transform instance, ActionEngine_Prop mainWeaponProp, ActionEngine_Prop subWeaponProp)
        {
            if (!m_UnitEquip || !instance.TryGetComponent(out UnitEquip instanceEquip))
            {
                return;
            }
            
            foreach (var equipType in s_AllEquipTypes)
            {
                if (!instanceEquip.GetMeshByType(equipType, out var target) || !target)
                {
                    continue;
                }

                if (!m_UnitEquip.GetMeshByType(equipType, out var renderer) || !renderer || !renderer.enabled || !renderer.gameObject.activeSelf)
                {
                    target.gameObject.SetActive(false);
                    continue;
                }

                if (renderer is SkinnedMeshRenderer source && target is SkinnedMeshRenderer copy)
                {
                    if (source.sharedMesh)
                    {
                        copy.sharedMesh = source.sharedMesh;
                        copy.bones = GetBones(instanceEquip, source.bones);
                        copy.rootBone = instanceEquip.GetBoneByName(source.rootBone.name);
                        target.gameObject.SetActive(true);
                    }
                    else
                    {
                        target.gameObject.SetActive(false);
                    }
                }
            }
            ReplaceWeapon(EquipType.MainWeapon, instanceEquip, mainWeaponProp);
            ReplaceWeapon(EquipType.SubWeapon, instanceEquip, subWeaponProp);
        }

        private void ReplaceWeapon(EquipType type, UnitEquip instanceEquip, ActionEngine_Prop source)
        {
            if (!instanceEquip.GetMeshByType(type, out var weapon) || !weapon || !weapon.TryGetComponent(out MeshFilter meshFilter))
            {
                // EngineDebug.LogError($"替换网格 [{instanceEquip.gameObject.name}] ({type.ToString()})[{instanceEquip.GetMeshByType(type, out var aa)}]");
                // EngineDebug.LogError($"替换网格 [{weapon is not null}]");

                return;
            }

            if (source && source.rendererMesh.TryGetComponent(out MeshFilter sourceMeshFilter))
            {
                meshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
                var sourceBone = source.transform.parent.name;
                var localPosition = source.transform.localPosition;
                var localRotation = source.transform.localRotation;
                var localScale = source.transform.localScale;
                var copyBone = instanceEquip.GetBoneByName(sourceBone);
                if (copyBone)
                {
                    weapon.transform.SetParent(copyBone, false);
                    weapon.transform.SetLocalPositionAndRotation(localPosition, localRotation);
                    weapon.transform.localScale = localScale;
                }
            }
            else
            {
                meshFilter.sharedMesh = null;
            }
        }

        private Transform[] GetBones(UnitEquip equip, Transform[] newBones)
        {
            var count = newBones.Length;
            var result = new Transform[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = equip.GetBoneByName(newBones[i].name);
            }

            return result;
        }

        public IActionEventData Clone(IActionEventData source)
        {
            var copy = source as Event_AfterImageEntity;
            copy.m_PrefabPath = m_PrefabPath;
            copy.m_AnimationName = m_AnimationName;
            //copy.m_SkillMaster = m_SkillMaster;
            copy.m_PartPointType = m_PartPointType;
            copy.m_OffsetPos = m_OffsetPos;
            copy.m_OffsetRot = m_OffsetRot;
            copy.m_LocalScale = m_LocalScale;
            copy.m_AlwaysFollow = m_AlwaysFollow;

            return copy;
        }
    }
}
