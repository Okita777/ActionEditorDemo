using System;

namespace AsiActionEngine.RunTime
{
    public partial class EngineResourcesManager : SingletonBase<EngineResourcesManager>
    {
        private void InitLoadData()
        {
            OnInit_RT();
        }

        private void OnAsyncLoadBinary(string assetKey, Action<object> onGetAsset)
        {
            m_LoadAsync.LoadBinary(assetKey, onGetAsset);
        }
    }
}