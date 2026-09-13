using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class ActionEngineProperty
    {
        public List<SAEP> mUnits = new List<SAEP>();
        // public List<SAEP> mActions = new List<SAEP>();
        public List<SAEP> mProps = new List<SAEP>();
        public List<SAEP> mGValues = new List<SAEP>();
        public List<SAEP> mEquations = new List<SAEP>();
        public List<SAEP> mCameras = new List<SAEP>();
        public List<SAEP> mSkills = new List<SAEP>();

        private Dictionary<int, string> mDicUnit = new Dictionary<int, string>();
        // private Dictionary<int, string> mDicAction = new Dictionary<int, string>();
        private Dictionary<int, string> mDicProp = new Dictionary<int, string>();
        private Dictionary<int, string> mDicGValue = new Dictionary<int, string>();
        private Dictionary<int, string> mDicEquation = new Dictionary<int, string>();
        private Dictionary<int, string> mDicCamera = new Dictionary<int, string>();
        private Dictionary<int, string> mDicSkill = new Dictionary<int, string>();
        private bool mIsInitialized = false;

        private void Initialize()
        {
            if (mIsInitialized) return;
            InitializeRun();
            mIsInitialized = true;
        }

        public void InitializeRun()
        {
            mDicUnit.Clear();
            // mDicAction.Clear();
            mDicProp.Clear();
            mDicGValue.Clear();
            mDicEquation.Clear();
            mDicCamera.Clear();
            mDicSkill.Clear();

            foreach (SAEP _saep in mUnits) mDicUnit.Add(_saep.mID, _saep.mName);
            // foreach (SAEP _saep in mActions) mDicAction.Add(_saep.mID, _saep.mName);
            foreach (SAEP _saep in mProps) mDicProp.Add(_saep.mID, _saep.mName);
            foreach (SAEP _saep in mGValues) mDicGValue.Add(_saep.mID, _saep.mName);
            foreach (SAEP _saep in mEquations) mDicEquation.Add(_saep.mID, _saep.mName);
            foreach (SAEP _saep in mCameras) mDicCamera.Add(_saep.mID, _saep.mName);
            foreach (SAEP _saep in mSkills) mDicSkill.Add(_saep.mID, _saep.mName);

            mIsInitialized = true;
        }

        public bool TryGetUnit(int _id, out string _name)
        {
            Initialize();
            bool _isValid = mDicUnit.TryGetValue(_id, out _name);
            if (!_isValid) EngineDebug.LogError($"尝试[<color=#ffcc00>Unit命名</color>]时获取错误，ID[<color=#ffcc00>{_id}</color>]");
            return _isValid;
        }
        public bool TryGetAction(int _id, out string _name)
        {
            Initialize();
            bool _isValid = mDicUnit.TryGetValue(_id, out _name);
            if (!_isValid) EngineDebug.LogError($"尝试[<color=#ffcc00>Action命名</color>]时获取错误，ID[<color=#ffcc00>{_id}</color>]");

            return _isValid;
        }
        public bool TryGetProp(int _id, out string _name)
        {
            Initialize();
            bool _isValid = mDicProp.TryGetValue(_id, out _name);
            if (!_isValid) EngineDebug.LogError($"尝试[<color=#ffcc00>Prop命名</color>]时获取错误，ID[<color=#ffcc00>{_id}</color>]");

            return _isValid;
        }
        public bool TryGetGValue(int _id, out string _name)
        {
            Initialize();
            bool _isValid = mDicGValue.TryGetValue(_id, out _name);
            if (!_isValid) EngineDebug.LogError($"尝试[<color=#ffcc00>GValue命名</color>]时获取错误，ID[<color=#ffcc00>{_id}</color>]");

            return _isValid;
        }
        public bool TryGetSkill(int _id, out string _name)
        {
            Initialize();
            bool _isValid = mDicSkill.TryGetValue(_id, out _name);
            if (!_isValid) EngineDebug.LogError($"尝试[<color=#ffcc00>GValue命名</color>]时获取错误，ID[<color=#ffcc00>{_id}</color>]");

            return _isValid;
        }
        public bool TryGetEquation(int _id, out string _name)
        {
            Initialize();
            bool _isValid = mDicEquation.TryGetValue(_id, out _name);
            if (!_isValid) EngineDebug.LogError($"尝试[<color=#ffcc00>Equation命名</color>]时获取错误，ID[<color=#ffcc00>{_id}</color>]");
            return _isValid;
        }
        public bool TryGetCamera(int _id, out string _name)
        {
            Initialize();
            bool _isValid = mDicCamera.TryGetValue(_id, out _name);
            if (!_isValid) EngineDebug.LogError($"尝试[<color=#ffcc00>Camera命名</color>]时获取错误，ID[<color=#ffcc00>{_id}</color>]");
            return _isValid;
        }
    }
}