using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class AudioClipDicList
    {
        public List<AudioClipDic> clips = new List<AudioClipDic>();
        public GString gString = new GString();

        /// <summary>
        /// 加载音频资源
        /// </summary>
        /// <param name="_isAll">是否加载全部音频资源</param> 
        public void Load(bool _isAll)
        {
            foreach (AudioClipDic _clipDic in clips)
            {
                // if(_clipDic.initLoad || _isAll) 
                _clipDic.Init();
            }
        }
    }

    [System.Serializable]
    public class AudioClipDic
    {
        public string name;
        public string defaultClipPath;
        public bool initLoad = false;
        public List<AudioClipGroup_Part> parts = new List<AudioClipGroup_Part>();
        [NonSerialized] public AudioClip defaultClip;
        [NonSerialized] public bool initialized = false;
        [NonSerialized] public bool loadFilish = false;
        [NonSerialized] private int loadNumber = 0;

        [NonSerialized]
        private Dictionary<string, AudioClipGroup_Part> audioClipDic = new Dictionary<string, AudioClipGroup_Part>();
        public void Init()
        {
            if (!initialized)
            {
                //避免多次调用时多次加载
                initialized = true;
                loadFilish = false;
                if (audioClipDic is null) audioClipDic = new Dictionary<string, AudioClipGroup_Part>();

                if (parts.Count == 0)
                {
                    loadFilish = true;
                    return;
                }

                if (string.IsNullOrEmpty(defaultClipPath))
                {
                    LoadAudioClips();
                }
                else
                {
                    EngineResourcesManager.Instance.AsyncLoadObj(defaultClipPath, (UnityEngine.Object _obj) =>
                    {
                        if (_obj is AudioClip audioClip)
                        {
                            defaultClip = audioClip;
                        }
                        LoadAudioClips();
                    });
                }
            }
        }

        public void SetDefaultClip(AudioClip clip)
        {
            defaultClip = clip;
#if UNITY_EDITOR
            defaultClipPath = UnityEditor.AssetDatabase.GetAssetPath(clip);
#else
            defaultClipPath = string.Empty;
#endif
            // defaultClipPath = ActionEngineResources.Instance.GetAssetPath(clip);
        }

        public void UpdateAudioClipDic()
        {
            audioClipDic.Clear();
            foreach (AudioClipGroup_Part part in parts)
            {
                audioClipDic.TryAdd(part.ClipName, part);
            }
        }

        private void LoadAudioClips()
        {
            loadNumber = parts.Count;
            audioClipDic.Clear();
            foreach (AudioClipGroup_Part _audio in parts)
            {
                _audio.Init(
                    (str, part) =>
                    {
                        audioClipDic.TryAdd(part.ClipName, part);
                        EngineDebug.Log($"音频字典成员 [{name}] [{str}] 加载完成");

                        loadNumber--;
                        if (loadNumber <= 0)
                        {
                            loadFilish = true;
                            EngineDebug.Log($"音频字典 [{name} 已完成加载]");
                        }
                    }
                    , str => { EngineDebug.Log("加载音频： " + str); }
                );
            }
        }

        //获取音频
        public bool GetAudioClip(string clipName, out AudioClip clip)
        {
            if (loadFilish)
            {
                EngineDebug.Log("播放");
                if (audioClipDic.TryGetValue(clipName, out AudioClipGroup_Part _audio))
                {
                    if (_audio.isRandom)
                    {
                        int selectID = _audio.AudioClips.Count;
                        selectID = Random.Range(0, selectID);
                        clip = _audio.AudioClips[selectID];
                    }
                    else
                    {
                        _audio.index++;
                        if (_audio.index >= _audio.AudioClips.Count)
                        {
                            _audio.index = 0;
                        }
                        clip = _audio.AudioClips[_audio.index];
                    }
                }
                else
                {
                    clip = defaultClip;
                }
                return true;
            }
            else
            {
#if UNITY_EDITOR
                EngineDebug.LogWarning($"在尝试获取 [{name}] 下的音频，但该字典未加载完成");
#endif
            }
            clip = null;
            return false;
        }

        public bool GetAudioClip(byte groupID, out AudioClip clip)
        {

            if (loadFilish)
            {
                if (groupID < parts.Count)
                {
                    AudioClipGroup_Part _audio = parts[groupID];

                    if (_audio.AudioClips.Count > 0)
                    {
                        if (_audio.isRandom)
                        {
                            int selectID = _audio.AudioClips.Count;
                            selectID = Random.Range(0, selectID);
                            clip = _audio.AudioClips[selectID];
                        }
                        else
                        {
                            _audio.index++;
                            if (_audio.index >= _audio.AudioClips.Count)
                            {
                                _audio.index = 0;
                            }
                            clip = _audio.AudioClips[_audio.index];
                        }
                        return true;
                    }
                    clip = defaultClip;
                    return true;
                }
#if UNITY_EDITOR
                else
                {
                    EngineDebug.LogError(
                        $"在尝试获取音频时出错 尝试获取字典 [{name}] 下的成员时超出索引!!" + "\n" +
                        $"尝试获取ID [{groupID}] , 但总长只有 [{parts.Count}]");
                }
#endif
            }
#if UNITY_EDITOR
            else
            {
                EngineDebug.LogWarning($"在尝试获取 [{name}] 下的音频，但该字典未加载完成");
            }
#endif


            clip = null;
            return false;
        }
    }

    [System.Serializable]
    public class AudioClipGroup_Part
    {
        public string ClipName;
        public List<string> AudioClipsPath = new List<string>();
        public bool open = false;
        public bool isRandom = true;
        [NonSerialized] public List<AudioClip> AudioClips = new List<AudioClip>();
        [NonSerialized] public int index = 0;
        [NonSerialized] bool Initialized = false;
        // [NonSerialized] private int loadID = 0;

        public void Init(Action<string, AudioClipGroup_Part> loadFinishedCallback = null, Action<string> locadCallback = null)
        {
            if (!Initialized)
            {
                //避免多次调用时多次加载  
                Initialized = true;
                // Debug.Log("加载了: " + AudioClipsPath.Count);
                AudioClips = new List<AudioClip>();
                foreach (string path in AudioClipsPath)
                    AudioClips.Add(null);
                if (AudioClipsPath.Count > 0)
                {
                    LoadAudioClips(0, locadCallback, loadFinishedCallback);
                }
                else
                {
                    loadFinishedCallback?.Invoke(ClipName, this);
                }
            }
        }

        public void SetAudioClip(int id, AudioClip clip)
        {
            AudioClips[id] = clip;
            // AudioClipsPath[id] = ActionEngineResources.Instance.GetAssetPath(clip);
#if UNITY_EDITOR
            AudioClipsPath[id] = UnityEditor.AssetDatabase.GetAssetPath(clip);
#else
            AudioClipsPath[id] = string.Empty;
#endif
        }

        public void RemoveClip(int id)
        {
            AudioClips.RemoveAt(id);
            AudioClipsPath.RemoveAt(id);
        }

        public void AddAudioClip()
        {
            AudioClips.Add(null);
            AudioClipsPath.Add(String.Empty);
        }

        private void LoadAudioClips(int id, Action<string> locadCallback, Action<string, AudioClipGroup_Part> loadFinishedCallback)
        {
            if (string.IsNullOrEmpty(AudioClipsPath[id]))
            {
                id++;
                if (id < AudioClipsPath.Count)
                {
                    LoadAudioClips(id, locadCallback, loadFinishedCallback);
                }
                else
                {
                    loadFinishedCallback?.Invoke(ClipName, this);
                }
                return;
            }
            EngineResourcesManager.Instance.AsyncLoadObj(AudioClipsPath[id], (UnityEngine.Object _obj) =>
            {
                if (_obj is AudioClip audioClip)
                {
                    AudioClips[id] = audioClip;
                    locadCallback?.Invoke(audioClip.name);
                }

                id++;
                if (id < AudioClipsPath.Count)
                {
                    LoadAudioClips(id, locadCallback, loadFinishedCallback);
                }
                else
                {
                    loadFinishedCallback?.Invoke(ClipName, this);
                }
            });

        }


    }
}