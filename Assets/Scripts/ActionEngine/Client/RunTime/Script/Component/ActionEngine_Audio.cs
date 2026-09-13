using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;
#if FMOD
using FMODUnity;
#endif

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_Audio : MonoBehaviour
    {
#if FMOD
        public List<StudioEventEmitter> m_AudioSources = new List<StudioEventEmitter>();
        public List<string> m_AudioSourceNames = new List<string>();
        public void PlayAudio(byte AudioSourceID, EventReference _sources)
        {
            if (!_sources.IsNull)
            {
                m_AudioSources[AudioSourceID].EventReference = _sources;
                m_AudioSources[AudioSourceID].Play();
            }
        }
        public void StopAudio(byte AudioSourceID)
        {
            m_AudioSources[AudioSourceID].Stop();
        }
#else
        public List<AudioSource> m_AudioSources = new List<AudioSource>();
        public List<string> m_AudioSourceNames = new List<string>();
        private void OnEnable()
        {
            ActionEngineManager_AudioClip.Instance.Init();
            // ActionEngineManager_AudioClip.Instance._audioClipDicList.Load(true);
        }

        public void PlayAudio(float audioVolume, byte AudioSourceID, byte AudioDicDicID, byte AudioDicChilID)
        {
            if (!ActionEngineManager_AudioClip.Instance.loaded)
            {
                return;
            }

            AudioClipDic _clipDic = ActionEngineManager_AudioClip.Instance._audioClipDicList.clips[AudioDicDicID];
            if (_clipDic.GetAudioClip(AudioDicChilID, out AudioClip _clip))
            {
                AudioSource _audio = m_AudioSources[AudioSourceID];
                _audio.volume = audioVolume;
                _audio.clip = _clip;
                _audio.Play();
            }
        }

        public void PlayAudio(ActionStateMachine _stateMachine, float audioVolume, byte AudioSourceID,
            byte AudioDicDicID)
        {
            if (!ActionEngineManager_AudioClip.Instance.loaded)
            {
                return;
            }

            AudioClipDicList _clipDic = ActionEngineManager_AudioClip.Instance._audioClipDicList;
            string name = _clipDic.gString.GetValue(_stateMachine.AllActionStatePart[0]);
            if (_clipDic.clips[AudioDicDicID].GetAudioClip(name, out AudioClip _clip))
            {
                AudioSource _audio = m_AudioSources[AudioSourceID];
                _audio.volume = audioVolume;
                _audio.clip = _clip;
                _audio.Play();
            }

        }

        public void PlayAudio(float audioVolume, byte AudioSourceID, byte AudioDicDicID)
        {
            if (!ActionEngineManager_AudioClip.Instance.loaded)
            {
                return;
            }
            AudioClipDicList _clipDic = ActionEngineManager_AudioClip.Instance._audioClipDicList;
            if (_clipDic.clips[AudioDicDicID].GetAudioClip("test null audio", out AudioClip _clip))
            {
                AudioSource _audio = m_AudioSources[AudioSourceID];
                _audio.volume = audioVolume;
                _audio.clip = _clip;
                _audio.Play();
            }

        }
#endif
    }
}