namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public struct SAEP
    {
        public int mID;
        public string mName;

        public SAEP(int id, string name)
        {
            mID = id;
            mName = name;
        }
    }
}