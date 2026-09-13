using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public struct PointData
    {
        [SerializeField] public EVector3 eVector_pos;
        [SerializeField] public EVector3 eVector_rot;
        public Quaternion rot => Quaternion.Euler(eVector_rot.GetValue());
        public Vector3 pos => eVector_pos.GetValue();
        public PointData(Vector3 pos, Quaternion rot)
        {
            eVector_pos = new EVector3(pos.x, pos.y, pos.z);
            eVector_rot = new EVector3(rot.eulerAngles);
        }
        public PointData(Vector3 pos, Vector3 rot)
        {
            eVector_pos = new EVector3(pos.x, pos.y, pos.z);
            eVector_rot = new EVector3(rot);
        }
        public void Reset()
        {
            eVector_pos = new EVector3(0, 0, 0);
            eVector_rot = new EVector3(0, 0, 0);
        }
        public bool isValid
        {
            get
            {
                if (pos.sqrMagnitude > 0.000001f)
                {
                    return true;
                }
                else
                {
                    return eVector_rot.GetValue().sqrMagnitude > 0.000001f;
                }
            }
        }
    }
}