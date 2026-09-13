using System;
using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.Rendering;

namespace AsiTimeLine.RunTime
{
    [System.Flags]
    public enum RenderingLayerCfg
    {
        None = 0,
        Default = 1 << 0,
        SceneGround = 1 << 1,
        SceneBlock = 1 << 2,
        SceneTerrain = 1 << 3,
        Vfx = 1 << 4,
        UnitPlayer = 1 << 5,
        UnitOther = 1 << 6,
        RTMod = 1 << 7,
        Outline = 1 << 8,
    }

    public abstract class UnitEquip : MonoBehaviour
    {
        protected readonly EquipType[] m_AllEquipTypes = (EquipType[])Enum.GetValues(typeof(EquipType));
        
        public abstract Transform GetBoneByName(string boneName);
        public abstract bool GetMeshByType(EquipType type, out Renderer skinMesh);
        public abstract void SetMeshByType(EquipType type, Renderer skinMesh);
        public abstract void SetKeywordState(int equipMask, string keyword, bool flag);
        public abstract void ExecuteMaterialModify(int equipMask, ShaderPropertyType type, string propertyName);
        public abstract void UpdateMaterialModify(int equipMask, ShaderPropertyType type, string propertyName, object value);
        public abstract bool RevertMaterialModify(int equipMask, ShaderPropertyType type, string propertyName, bool interrupt);
        public abstract bool GetGradientAsset(string assetName, out Gradient color);
        public abstract bool GetCurveAsset(string assetName, out AnimationCurve curveValue);
        public abstract bool GetColorAsset(string assetName, out Color color);
        public abstract void SetRenderingLayerMask(RenderingLayerCfg renderLayer, bool force);
        
        protected uint GetRealLayer(RenderingLayerCfg renderLayer)
        {
            return (uint)renderLayer;
        }

        public abstract void RegisterAllEquip();
    }
}