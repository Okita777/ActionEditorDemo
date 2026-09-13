using System;
using System.Collections.Generic;
// using System.Runtime.Serialization.Formatters.Binary;

namespace AsiActionEngine.RunTime
{
    public interface IResourceLoader
    {
        public string DataName_Unit();
        public string DataName_Action();
        public string DataName_Skill();
        public string DataName_Cam();
        public string DataName_Module();
        public string DataName_Item();
        public string DataName_Gvalue();
        public string DataName_Equation();

        public Dictionary<ushort, EngineGValue> GetEngineGValue();
        public void Init();
        public void LoadUnit(int assetKey, Action<TargetUnit> _callback, EUnitType _type);
        public void LoadSkill(int skillID, Action<ActionEngine_Skill> _callback);
        public ActionEngine_Unit Player();
        public void DestoryUnit(ActionEngine_Unit _unit);
        public void LoadAssets(string assetKey, Action<UnityEngine.Object> onGetAsset);
        public void LoadBinary(string assetKey, Action<object> onGetAsset);

        public void ClearLoadAssets(string assetKey);
        public void ClearLoadBinary(string assetKey);

        //将完整路径转换为Runtime路径
        public string GetRunTimePath(string assetKey);
        //仅Editor加载资产时使用
        public void LoadAssetsEditor(string assetKey, Action<UnityEngine.Object> onGetAsset, bool ondebug);
        public void ClearLoadAssetsEditor(string assetKey);
    }
}
