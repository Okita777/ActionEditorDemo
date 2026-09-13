using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public struct EVector2
    {
        [SerializeField] public float x, y;

        // public EVector3() { }
        public EVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public EVector2(Vector2 value)
        {
            this.x = value.x;
            this.y = value.y;
        }
        public void SetValue(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public void SetValue(Vector2 value)
        {
            this.x = value.x;
            this.y = value.y;
        }

        public Vector2 GetValue()
        {
            return new Vector2(x, y);
        }

        public EVector2 Clone()
        {
            return new EVector2(x, y);
        }
    }
}