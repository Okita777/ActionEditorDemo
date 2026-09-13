using System.Collections.Generic;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [ExecuteAlways]
    public class EquipCostume : MonoBehaviour
    {
        [System.Serializable]
        public struct ColthPaths
        {
            public string mName;
            public string mPath;

            public ColthPaths(string _mName, string _mPath)
            {
                mName = _mName;
                mPath = _mPath;
            }
        }

        public List<ColthPaths> mColthPaths = new List<ColthPaths>();


        private void Awake()
        {
            foreach (var VARIABLE in mColthPaths)
            {
                UnitClothPool.Instance.AddClothPath(VARIABLE.mName, VARIABLE.mPath);
            }
        }
    }
}