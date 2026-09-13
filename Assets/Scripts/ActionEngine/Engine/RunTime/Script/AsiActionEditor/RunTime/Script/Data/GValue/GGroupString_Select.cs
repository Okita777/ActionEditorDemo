
namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class GGroupString_Select
    {
        //[UnityEngine.SerializeField] public float mSerValue;
        private GInt mStringID = new GInt();
        private GGroupString mStringGroup = new GGroupString();

        public GInt StringID
        {
            get { return mStringID; }
            set { mStringID = value; }
        }
        public GGroupString StringGroup
        {
            get { return mStringGroup; }
            set { mStringGroup = value; }
        }
        public string GetValue(ActionStatePart part)
        {
            return mStringGroup.GetValue(part)[mStringID.GetValue(part)];
        }

        public GGroupString_Select Clone()
        {
#if UNITY_EDITOR
            GGroupString_Select n = new GGroupString_Select();
            n.mStringID = (GInt)mStringID.Clone();
            n.mStringGroup = (GGroupString)mStringGroup.Clone();
            return n;
#endif
            return this;
        }
    }
}