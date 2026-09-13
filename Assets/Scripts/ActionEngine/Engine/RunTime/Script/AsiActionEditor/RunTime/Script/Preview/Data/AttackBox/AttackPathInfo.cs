using System.Linq;
using UnityEngine;

namespace AsiActionEngine.RunTime
{

    [System.Serializable]
    public class AttackPathInfo
    {
        [SerializeField] public AttackPath_Box[] Box = new AttackPath_Box[0];
        [SerializeField] public AttackPath_Point[] Point = new AttackPath_Point[1];
        [SerializeField] public AttackPath_Sphere[] Sphere = new AttackPath_Sphere[0];
        [SerializeField] public AttackPath_Capsule[] Capsule = new AttackPath_Capsule[0];
        [SerializeField] public float HitInterval = -1.0f;

        public AttackPathInfo Clone()
        {
            AttackPathInfo _attackBoxInfo = new AttackPathInfo();

            _attackBoxInfo.Box = Box.ToArray();
            _attackBoxInfo.Point = Point.ToArray();
            _attackBoxInfo.Sphere = Sphere.ToArray();
            _attackBoxInfo.Capsule = Capsule.ToArray();
            _attackBoxInfo.HitInterval = HitInterval;

            return _attackBoxInfo;
        }
    }

    [System.Serializable]
    public struct AttackPath_Point
    {
        [SerializeField] public EVector3 StartPos;

        public AttackPath_Point(Vector3 _StartPos)
        {
            StartPos = new EVector3(_StartPos.x, _StartPos.y, _StartPos.z);
        }

    }

    [System.Serializable]
    public struct AttackPath_Sphere
    {
        [SerializeField] public EVector3 StartPos;
        [SerializeField] public float Radius;

        public AttackPath_Sphere(Vector3 _StartPos, float _Radius)
        {
            StartPos = new EVector3(_StartPos.x, _StartPos.y, _StartPos.z);
            Radius = _Radius;
        }
    }

    [System.Serializable]
    public struct AttackPath_Capsule
    {
        [SerializeField] public EVector3 StartPos;
        [SerializeField] public EVector3 EndPos;
        [SerializeField] public float Radius;

        public AttackPath_Capsule(Vector3 _StartPos, Vector3 _EndPos, float _Radius)
        {
            StartPos = new EVector3(_StartPos.x, _StartPos.y, _StartPos.z);
            EndPos = new EVector3(_EndPos.x, _EndPos.y, _EndPos.z);
            Radius = _Radius;
        }
    }

    [System.Serializable]
    public struct AttackPath_Box
    {
        [SerializeField] public EVector3 Center;
        [SerializeField] public EVector3 HalfScale;
        [SerializeField] public EVector3 Rotate;

        public AttackPath_Box(Vector3 _Center, Vector3 _HalfScale, Vector3 _Rotate)
        {
            Center = new EVector3(_Center.x, _Center.y, _Center.z);
            HalfScale = new EVector3(_HalfScale.x, _HalfScale.y, _HalfScale.z);
            Rotate = new EVector3(_Rotate.x, _Rotate.y, _Rotate.z);
        }
    }
}