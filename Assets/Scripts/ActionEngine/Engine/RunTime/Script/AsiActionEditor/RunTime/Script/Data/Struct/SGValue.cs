namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public struct SGValue
    {
        public ushort mGroupID;
        public ushort mID;
        EGValueType mType;
        public object mValue;

        public SGValue(ushort _mGroupID, ushort _mID, EGValueType _mType, object _mValue)
        {
            mGroupID = _mGroupID;
            mID = _mID;
            mType = _mType;
            mValue = _mValue;
        }
    }
}