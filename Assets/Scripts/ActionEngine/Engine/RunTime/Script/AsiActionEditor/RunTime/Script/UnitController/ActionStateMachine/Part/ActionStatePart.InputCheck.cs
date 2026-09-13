using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStatePart
    {
        private Dictionary<string, float> mKeyKeepTime = new Dictionary<string, float>();

        private float mHoldKeyIntervalTime => mActionStateMachine.HoldKeyIntervalTime;

        public string NowInputClickKey { get; set; } //当前点击的按键
        public string NowInputDownKey { get; set; } //当前按下的按键
        public string NowInputUpKey { get; set; } //当前松开的按键
        public string NowInputHoldKey { get; set; } //当前长按的按键
        public List<string> NowInputKey { get; private set; } = new List<string>(); //当前按键状态

        // 注册按键按下
        public void SetKeyDown(string _keyName)
        {
            //Debug.Log($"注册按键按下[{AnimaLayer}]: [{_keyName}]");
            NowInputDownKey = _keyName;
            if (!NowInputKey.Contains(_keyName))
                NowInputKey.Add(_keyName);
            if (mKeyKeepTime.ContainsKey(_keyName))
            {
                mKeyKeepTime[_keyName] = 0;
            }
            else
            {
                mKeyKeepTime.Add(_keyName, 0f);
            }
        }
        // 注册按键抬起
        public void SetKeyUp(string _keyName)
        {
            //Debug.Log($"注册按键抬起[{AnimaLayer}]: [{_keyName}]");
            NowInputUpKey = _keyName;
            if (mKeyKeepTime.ContainsKey(_keyName))
            {
                if (mKeyKeepTime[_keyName] < mHoldKeyIntervalTime)
                {
                    NowInputClickKey = _keyName;
                }
                //重置长按按钮
                if (_keyName == NowInputHoldKey)
                {
                    NowInputHoldKey = MotionEngineConst.NondKeyName;
                }
                mKeyKeepTime.Remove(_keyName);
            }
            if (NowInputKey.Contains(_keyName))
                NowInputKey.Remove(_keyName);
            // NowInputKey = MotionEngineConst.NondKeyName;
        }

        public void ReSetKeys(bool _resetHoldKey = false)
        {
            //Debug.Log($"被重置按键了[{AnimaLayer}]");
            NowInputClickKey = MotionEngineConst.NondKeyName;
            NowInputDownKey = MotionEngineConst.NondKeyName;
            NowInputUpKey = MotionEngineConst.NondKeyName;
            IsMoveInputPre = false;
            if (_resetHoldKey)
            {
                NowInputHoldKey = MotionEngineConst.NondKeyName;
                //mKeyKeepTime.Clear();
            }
        }

        private void UpdateInputKey(float _deltatime)
        {
            List<string> _keys = new List<string>(mKeyKeepTime.Keys);
            foreach (var _key in _keys)
            {
                float _value = mKeyKeepTime[_key];
                if (_value < mHoldKeyIntervalTime)
                {
                    mKeyKeepTime[_key] += _deltatime;
                    if (mKeyKeepTime[_key] >= mHoldKeyIntervalTime)
                    {
                        NowInputHoldKey = _key;
                    }
                }
            }
        }

    }
}