
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public abstract class GValue
    {
        /// <summary>
        /// 详细ID
        /// </summary>
        [UnityEngine.SerializeField] public ushort mValueIndex = 0;
        /// <summary>
        /// 组ID
        /// </summary>
        [UnityEngine.SerializeField] public ushort mValueGroupIndex = 0;
        /// <summary>
        /// 是否为Gvalue
        /// </summary>
        [UnityEngine.SerializeField] public bool mType;


        public abstract GValue Clone();
    }
}