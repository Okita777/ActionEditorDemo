using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class Ex_Update_Attach : ActionLogics
    {
        private List<Event_Attach> m_AnligTargets = new List<Event_Attach>();

        public void StartAttach(Event_Attach _Event_Attach)
        {
            if (_Event_Attach.LerpTime > 0)
                _Event_Attach.m_LerpTime_t = _Event_Attach.LerpTime;
            else
                _Event_Attach.m_LerpTime_t = -1.0f;
            m_AnligTargets.Add(_Event_Attach);
        }

        public override void Update(ActionStateMachine _actionState, float _deltaTime)
        {
            ActionStatePart part = _actionState.AllActionStatePart[0];
            for (int i = 0; i < m_AnligTargets.Count; i++)
            {
                Event_Attach m_Event_Attach = m_AnligTargets[i];
                m_Event_Attach.m_LerpTime_t -= _deltaTime;
                if (m_Event_Attach.m_LerpTime_t > 0)
                {
                    float t = m_Event_Attach.m_LerpTime_t / m_Event_Attach.LerpTime;


                    if (m_Event_Attach.AlignToPoint)
                    {
                        Vector3 pos = m_Event_Attach.AlignPoint.GetValue(part).pos;
                        Quaternion rot = m_Event_Attach.AlignPoint.GetValue(part).rot;

                        m_Event_Attach.m_Refer.position = Vector3.Lerp(pos, m_Event_Attach.m_StartPos, t);
                        m_Event_Attach.m_Refer.rotation = Quaternion.Lerp(rot, m_Event_Attach.m_StartRot, t);
                    }
                    else
                    {
                        Vector3 pos = m_Event_Attach.m_Target.position;
                        Quaternion rot = m_Event_Attach.m_Target.rotation;

                        m_Event_Attach.m_Refer.position = Vector3.Lerp(pos, m_Event_Attach.m_StartPos, t);
                        m_Event_Attach.m_Refer.rotation = Quaternion.Lerp(rot, m_Event_Attach.m_StartRot, t);
                    }
                }
                else
                {
                    Align(part, m_Event_Attach);
                    m_AnligTargets.RemoveAt(i);
                }
            }
        }

        private void Align(ActionStatePart part, Event_Attach m_Event_Attach)
        {
            if (m_Event_Attach.AlignToPoint)
            {
                m_Event_Attach.m_Refer.position = m_Event_Attach.AlignPoint.GetValue(part).pos;
                m_Event_Attach.m_Refer.rotation = m_Event_Attach.AlignPoint.GetValue(part).rot;
            }
            else
            {
                m_Event_Attach.m_Refer.position = m_Event_Attach.m_Target.position;
                m_Event_Attach.m_Refer.rotation = m_Event_Attach.m_Target.rotation;
            }
        }
    }
}