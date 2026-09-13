using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class EX_Update_AnimIK : ActionLogics
    {
        //ik所需参数
        private List<Event_TowBoneIK> EnteredEvents = new List<Event_TowBoneIK>();
        private List<Event_TowBoneIK> ExitingEvents = new List<Event_TowBoneIK>();
        private Matrix4x4 convertPosition = Matrix4x4.zero;

        public void Enter(Event_TowBoneIK event_TowBoneIK)
        {
            event_TowBoneIK.m_BlnedTime = 0;
            EnteredEvents.Add(event_TowBoneIK);
        }

        public void Exit(Event_TowBoneIK event_TowBoneIK)
        {
            event_TowBoneIK.m_BlnedTime = event_TowBoneIK.ExitTime;
            EnteredEvents.Remove(event_TowBoneIK);
            ExitingEvents.Add(event_TowBoneIK);
        }

        public override void LateUpdate(ActionStateMachine _actionState, float _deltaTime)
        {
            base.LateUpdate(_actionState, _deltaTime);

            foreach (Event_TowBoneIK e in EnteredEvents)
            {
                if (e.m_BlnedTime < e.EnterTime)
                {
                    e.m_BlnedTime += _deltaTime;
                    float t = e.m_BlnedTime / e.EnterTime;
                    SetTowBoneIK(_actionState, e, Mathf.Min(t, 1));
                }
                else
                {
                    SetTowBoneIK(_actionState, e);
                }
            }

            for (int i = 0; i < ExitingEvents.Count; i++)
            {
                Event_TowBoneIK e = ExitingEvents[i];
                e.m_BlnedTime -= _deltaTime;
                if (e.m_BlnedTime > 0)
                {
                    float t = e.m_BlnedTime / e.ExitTime;
                    SetTowBoneIK(_actionState, e, t);
                }
                else
                {
                    ExitingEvents.Remove(e);
                }
            }
        }

        private void SetTowBoneIK(ActionStateMachine stateMachine, Event_TowBoneIK e, float weight = 1)
        {
            if (weight <= 0) return;

            ActionStatePart part = stateMachine.AllActionStatePart[0];

            Transform _target = e.m_IKEnder;
            Vector2 m_BoneLength = e.m_BoneLength;


            Vector3 targetPos = e.IkPoint.GetValue(part).pos;
            Vector3 foward = e.forward;
            Vector3 up = e.up;
            // Vector3 foward = Vector3.up;
            // Vector3 up = Vector3.forward;
            Quaternion targetRot = e.IkPoint.GetValue(part).rot;

            //ik解算函数
            Transform[] ikTarget = new Transform[3];
            ikTarget[0] = _target.parent.parent;
            ikTarget[1] = _target.parent;
            ikTarget[2] = _target;
            Quaternion[] ikRot = new Quaternion[ikTarget.Length];
            Vector3[] ikPos = new Vector3[ikTarget.Length];
            for (int i = 0; i < ikTarget.Length; i++)
            {
                ikRot[i] = ikTarget[i].rotation;
                ikPos[i] = ikTarget[i].position;
            }

            //IK控制末端
            Vector3 endPos = _target.position;
            if (e.IsReferTarget) endPos = e.m_IKRefer.position;
            // //获得相对矩阵
            // if (e.IsReferTarget)
            //     convertPosition = e.m_IKRefer.worldToLocalMatrix * e.m_IKEnder.localToWorldMatrix;

            // Vector3 referPos = e.IsReferTarget ? targetPos + convertPosition.GetPosition() : targetPos;
            // Vector3 ikTargetPos = Vector3.Lerp(ikPos[2], referPos, weight);
            Vector3 ikTargetPos = Vector3.Lerp(endPos, targetPos, weight);

            // Vector3 ikTargetPos = Vector3.Lerp(ikPos[2], targetPos, weight);

            // EngineDebug.DrawSphere(ikPos[2],0.05f,Color.red);
            // EngineDebug.DrawSphere(ikTargetPos,0.03f,Color.green);

            //骨骼长度
            m_BoneLength.y = (_target.parent.position - _target.parent.parent.position).magnitude;
            m_BoneLength.x = (endPos - _target.parent.position).magnitude;
            float ikDis = Vector3.Distance(ikTarget[0].position, ikTargetPos);
            if (ikDis < m_BoneLength.x + m_BoneLength.y)
            {
                //IK范围内
                float ik_angle =
                    Mathf.Acos((m_BoneLength.x * m_BoneLength.x + ikDis * ikDis - m_BoneLength.y * m_BoneLength.y) /
                               (2 * m_BoneLength.x * ikDis)) * (180 / Mathf.PI);

                Vector3 vZ = ikTargetPos - ikPos[0];
                Vector3 vY = ikTarget[0].rotation * up;
                // Quaternion rot = Quaternion.LookRotation(vZ, vY) * Quaternion.Euler(up * -ik_angle);
                int valid = e.m_IsReversal ? -1 : 1;
                Quaternion rot = Quaternion.LookRotation(vZ, vY) * Quaternion.Euler(0, ik_angle * valid, 0);//Quaternion.Euler(0, -ik_angle, 0)


                ikTarget[0].rotation = Quaternion.LookRotation(rot * foward, rot * up);
                rot = Quaternion.LookRotation(ikTargetPos - ikTarget[1].position, ikTarget[0].rotation * up);
                ikTarget[1].rotation = Quaternion.LookRotation(rot * foward, rot * up);

            }
            else
            {
                Vector3 vZ = ikTargetPos - ikPos[0];
                Vector3 vY = ikTarget[0].rotation * up;
                Quaternion rot = Quaternion.LookRotation(vZ, vY);
                ikTarget[0].rotation = Quaternion.LookRotation(rot * foward, rot * up);
                ikTarget[1].rotation = ikTarget[0].rotation;
            }

            // if (e.IsReferTarget)
            // {
            //     ikTarget[2].rotation = Quaternion.Lerp(ikRot[2], targetRot * convertPosition.rotation, weight);
            // }

            // EngineDebug.DrawSphere(ikTarget[2].position,0.02f,Color.blue);

        }
    }
}