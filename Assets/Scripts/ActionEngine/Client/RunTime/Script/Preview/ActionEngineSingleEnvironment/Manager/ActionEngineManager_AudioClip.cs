using AsiActionEngine.RunTime;

namespace AsiTimeLine.RunTime
{
    public class ActionEngineManager_AudioClip
    {
        #region Instance
        private static ActionEngineManager_AudioClip _instance = null;
        public static ActionEngineManager_AudioClip Instance
        {
            get
            {
                if (_instance is null) _instance = new ActionEngineManager_AudioClip();
                return _instance;
            }
        }
        #endregion

        public const string _assetsFolder = "Audio";
        public bool loaded = false;
        public AudioClipDicList _audioClipDicList = new AudioClipDicList();

        private bool initialized = false;

        public void Init()
        {
            if (!initialized)
            {
                initialized = true;
                LoadAudioClipDicList(false);
            }
        }

        private void LoadAudioClipDicList(bool _loadAllAudioClipDic)
        {
            ActionEnginLoadData.Instance.LoadInfo(EInfoType.AssetData, (value) =>
            {
                if (value is AudioClipDicList _target)
                {
                    _audioClipDicList = _target;
                    _audioClipDicList.Load(_loadAllAudioClipDic);
                }
                loaded = true;

            }, 0);
        }
    }
}