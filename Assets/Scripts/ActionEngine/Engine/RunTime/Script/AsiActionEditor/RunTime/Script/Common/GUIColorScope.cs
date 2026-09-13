using System;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public struct GUIColorScope : IDisposable
    {
        private readonly Color mColorRecord;
        private readonly bool mIsChange;

        public GUIColorScope(Color _color, bool _ischange = true)
        {
            mColorRecord = GUI.color;
            mIsChange = _ischange;
            if (mIsChange)
            {
                GUI.color = _color;
            }
        }

        public void Dispose()
        {
            if (mIsChange)
            {
                GUI.color = mColorRecord;
            }
        }
    }
}