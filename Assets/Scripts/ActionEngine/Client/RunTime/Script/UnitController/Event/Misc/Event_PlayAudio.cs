using System;
using AsiActionEngine.RunTime;
using UnityEngine;

#if FMOD
using FMODUnity;
#endif

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_PlayAudio : IActionEventData
    {
        [SerializeField] public byte m_AudioSourceIndex;
        [SerializeField] public float m_AudioVolume = 1.0f;
        [SerializeField] public bool m_CoustomAudio;
        [SerializeField] public byte m_AudioDicID;
        [SerializeField] public byte m_AudioDicChailID;

#if FMOD
        [SerializeField] public EventReference m_EventReference;
#endif
        [NonSerialized] private ActionEngine_Audio m_ActionEngine_Audio = null;
        public int GetEvenType() => (int)EEvenType.EET_Audio;

        public IActionEventData Creact() => new Event_PlayAudio();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            ActionStateMachine _stateMachine = _actionState.ActionStateMachine;
            if (_stateMachine.TryGetComponent(out m_ActionEngine_Audio, nameof(ActionEngine_Audio)))
            {
#if FMOD
                m_ActionEngine_Audio.PlayAudio(m_AudioSourceIndex, m_EventReference);
#else

                if (m_CoustomAudio)
                {
                    m_ActionEngine_Audio.PlayAudio(m_AudioVolume, m_AudioSourceIndex, m_AudioDicID, m_AudioDicChailID);
                }
                else
                {
                    m_ActionEngine_Audio.PlayAudio(_stateMachine, m_AudioVolume, m_AudioSourceIndex, m_AudioDicID);
                }
#endif
            }
        }

        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            if (m_ActionEngine_Audio is not null)
            {
#if FMOD
                m_ActionEngine_Audio.StopAudio(m_AudioSourceIndex);
#endif
            }
        }

        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_PlayAudio _event = _eventData as Event_PlayAudio;
            _event.m_AudioSourceIndex = m_AudioSourceIndex;
            _event.m_AudioVolume = m_AudioVolume;
            _event.m_CoustomAudio = m_CoustomAudio;
            _event.m_AudioDicID = m_AudioDicID;
            _event.m_AudioDicChailID = m_AudioDicChailID;
#if FMOD
            _event.m_EventReference = m_EventReference;
#endif
            return _event;
        }

        //UnityEditor
        //public static implicit operator Event_PlayAudio(SerializedObject v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}