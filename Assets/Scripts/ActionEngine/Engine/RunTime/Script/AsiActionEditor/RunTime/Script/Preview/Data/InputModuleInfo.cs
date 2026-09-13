using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    // public enum Vector2Input
    // {
    //     [Description("WASD")]WASD,
    //     [Description("方向键")]Dpad,
    //     [Description("鼠标位移")]MouseDelta,
    // }

    [System.Serializable]
    public class InputModuleInfo
    {
        public List<InputModulePart> InputModulePart = new List<InputModulePart>();
        public int InputSystemID = 0;
        public float LongPressInput = 0.2f;
        public float SloMotionScale = 0.2f;
        public int ExitRunTimeKey = (int)KeyCode.Escape;
        public int EnterRunTimeKey = -1;
        public int DisMouse = (int)KeyCode.Tab;
        public int SloMotionKey = (int)KeyCode.B;
        public InputModuleInfo(List<InputModulePart> _inputModulePart, int _InputSystemID)
        {
            InputModulePart = _inputModulePart;
            InputSystemID = _InputSystemID;
        }
    }

    [System.Serializable]
    public class InputModulePart
    {
        //当前输入模式的名字
        public string name;

        //键盘输入
        public List<InputKeyAction> keyActions = new List<InputKeyAction>();

        //鼠标输入
        public List<MouseButtonAction> mouseActions = new List<MouseButtonAction>();

        //预输入组列表
        public List<KeyCombinations> ComboInputActions = new List<KeyCombinations>();

        //组合键列表
        public List<KeyCombinations> KeyCombinAtions = new List<KeyCombinations>();

        //移动输入
        public int moveInput = 0;

        //视角输入
        public int viewInput = 1;
    }

    [System.Serializable]
    public class InputKeyAction
    {
        public string mAction;
        public KeyCode KeyCode;

        public InputKeyAction(string _mAction, KeyCode _KeyCode)
        {
            KeyCode = _KeyCode;
            mAction = _mAction;
        }
    }

    [System.Serializable]
    public class MouseButtonAction
    {
        public string mAction;
        public int mouseButton;
        public MouseButtonAction(string _mAction, int _mouseButton)
        {
            mouseButton = _mouseButton;
            mAction = _mAction;
        }
    }

    [System.Serializable]
    public class KeyCombinations
    {
        public List<int> keyGroup;
        public string mAction;

        public KeyCombinations(string _action, List<int> _keyGroup)
        {
            mAction = _action;
            keyGroup = _keyGroup;
        }
    }
}