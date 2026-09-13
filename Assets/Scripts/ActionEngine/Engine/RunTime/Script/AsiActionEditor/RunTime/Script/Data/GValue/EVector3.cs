using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public struct EVector3
    {
        [SerializeField] public float x, y, z;

        // public EVector3() { }
        public EVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public EVector3(Vector3 value)
        {
            this.x = value.x;
            this.y = value.y;
            this.z = value.z;
        }
        public void SetValue(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void SetValue(Vector3 value)
        {
            this.x = value.x;
            this.y = value.y;
            this.z = value.z;
        }

        public Vector3 GetValue()
        {
            return new Vector3(x, y, z);
        }

        public EVector3 Clone()
        {
            return new EVector3(x, y, z);
        }
    }
}