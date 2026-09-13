using AsiActionEngine.RunTime.GraphVal;
using UnityEngine;

namespace AsiActionEngine.RunTime
{

    [System.Serializable]
    public class AttackBoxInfo
    {
        [SerializeField] public AttackBoxPart[] Box = new AttackBoxPart[0];
        [SerializeField] public int AttackBoxType;

        [SerializeField] public ECharacteLimbType ReferPoint;
        [SerializeField] public EVector3 OffsetPos = new EVector3();
        [SerializeField] public EVector3 OffsetRot = new EVector3();
        [SerializeField] public float AnchorZ = 0.0f;
        [SerializeField] public EVector3 Scale = new EVector3(1, 0.2f, 0);//X长度,Y半径
        [SerializeField] public bool UseBluePrint_Lenth = false;
        [SerializeField] public bool UseBluePrint_Radius = false;
        [SerializeField] public GraphEvent_NoValue_Float Length = new GraphEvent_NoValue_Float();
        [SerializeField] public GraphEvent_NoValue_Float Radius = new GraphEvent_NoValue_Float();
        [SerializeField] public float HitInterval = -1.5f;

        public AttackBoxInfo Clone()
        {
            AttackBoxInfo _attackBoxInfo = new AttackBoxInfo();

            _attackBoxInfo.Box = Box;
            _attackBoxInfo.AttackBoxType = AttackBoxType;

            _attackBoxInfo.ReferPoint = ReferPoint;
            _attackBoxInfo.OffsetPos = OffsetPos;
            _attackBoxInfo.OffsetRot = OffsetRot;
            _attackBoxInfo.AnchorZ = AnchorZ;
            _attackBoxInfo.Scale = Scale;
            _attackBoxInfo.HitInterval = HitInterval;
            _attackBoxInfo.UseBluePrint_Lenth = UseBluePrint_Lenth;
            _attackBoxInfo.UseBluePrint_Radius = UseBluePrint_Radius;
            if (Length is null) Length = new GraphEvent_NoValue_Float(Scale.x);
            if (Radius is null) Radius = new GraphEvent_NoValue_Float(Scale.y);
            _attackBoxInfo.Length = Length.Clone();
            _attackBoxInfo.Radius = Radius.Clone();
            return _attackBoxInfo;
        }
    }

    [System.Serializable]
    public struct AttackBoxPart
    {
        [SerializeField] public EVector3 StartPos;
        [SerializeField] public EVector3 Dir;
        [SerializeField] public int TriggerTime;

        public AttackBoxPart(Vector3 _StartPos, Vector3 _dir, int _TriggerTime)
        {
            StartPos = new EVector3(_StartPos.x, _StartPos.y, _StartPos.z);
            Dir = new EVector3(_dir.x, _dir.y, _dir.z);
            TriggerTime = _TriggerTime;
        }
    }
}