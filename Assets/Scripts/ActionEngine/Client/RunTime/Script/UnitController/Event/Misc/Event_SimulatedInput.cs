using System;
using AsiActionEngine.RunTime;
using UnityEngine;

#if FMOD
using FMODUnity;
#endif

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class Event_SimulatedInput : IActionEventData
    {
        [SerializeField] private string mCheckKeyName = String.Empty;
        [SerializeField] private EInputKeyType mInputType = EInputKeyType.OnDown;


        //[EditorProperty("模拟按键", EditorPropertyType.EEPT_String)]
        public string CheckKeyName
        {
            get { return mCheckKeyName; }
            set { mCheckKeyName = value; }
        }
        [EditorProperty("行为类型", EditorPropertyType.EEPT_Enum)]
        public EInputKeyType InputType
        {
            get { return mInputType; }
            set { mInputType = value; }
        }
        public int GetEvenType() => (int)EEvenType.EET_SimulatedInput;

        public IActionEventData Creact() => new Event_SimulatedInput();

        public void Enter(ActionStatePart _actionState, bool _isSingle)
        {
            //ActionStatePart actionStatePart = _actionState;
            ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            if (mInputType == EInputKeyType.OnDown)
            {
                //actionStatePart.NowInputDownKey = mCheckKeyName;
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputDownKey = mCheckKeyName;
            }
            else if (mInputType == EInputKeyType.OnUp)
            {
                //actionStatePart.NowInputUpKey = mCheckKeyName;
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputUpKey = mCheckKeyName;
            }
            else if (mInputType == EInputKeyType.OnClick)
            {
                //actionStatePart.NowInputClickKey = mCheckKeyName;
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputClickKey = mCheckKeyName;
            }
            else if (mInputType == EInputKeyType.Down_State)
            {
                //if(!actionStatePart.NowInputKey.Contains(mCheckKeyName))
                //    actionStatePart.NowInputKey.Add(mCheckKeyName);
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble && !VARIABLE.NowInputKey.Contains(mCheckKeyName))
                        VARIABLE.NowInputKey.Add(mCheckKeyName);
            }
            else if (mInputType == EInputKeyType.Up_State)
            {
                //actionStatePart.NowInputKey.Remove(mCheckKeyName);
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputKey.Remove(mCheckKeyName);
            }
            else
            {
                //actionStatePart.NowInputHoldKey = mCheckKeyName;
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputHoldKey = mCheckKeyName;
            }
        }
        public void Exit(ActionStatePart _actionState, bool _interruot)
        {
            //ActionStatePart actionStatePart = _actionState;
            //if (mInputType == EInputKeyType.OnDown)
            //{
            //    actionStatePart.NowInputDownKey = MotionEngineConst.NondKeyName;
            //}
            //else if (mInputType == EInputKeyType.OnUp)
            //{
            //    actionStatePart.NowInputUpKey = MotionEngineConst.NondKeyName;
            //}
            //else if (mInputType == EInputKeyType.OnClick)
            //{
            //    actionStatePart.NowInputClickKey = MotionEngineConst.NondKeyName;
            //}
            //else if (mInputType == EInputKeyType.Down_State)
            //{
            //    actionStatePart.NowInputKey.Remove(mCheckKeyName);
            //}
            //else if (mInputType == EInputKeyType.Up_State)
            //{
            //    actionStatePart.NowInputKey.Remove(mCheckKeyName);
            //}
            //else
            //{
            //    actionStatePart.NowInputHoldKey = MotionEngineConst.NondKeyName;
            //}
            string noneName = MotionEngineConst.NondKeyName;
            ActionStateMachine stateMachine = _actionState.ActionStateMachine;
            if (mInputType == EInputKeyType.OnDown)
            {
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputDownKey = noneName;
            }
            else if (mInputType == EInputKeyType.OnUp)
            {
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputUpKey = noneName;
            }
            else if (mInputType == EInputKeyType.OnClick)
            {
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputClickKey = noneName;
            }
            else if (mInputType == EInputKeyType.Down_State)
            {

                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputKey.Remove(mCheckKeyName);
            }
            else if (mInputType == EInputKeyType.Up_State)
            {
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputKey.Remove(mCheckKeyName);
            }
            else
            {
                foreach (ActionStatePart VARIABLE in stateMachine.AllActionStatePart)
                    if (VARIABLE.ActionEnble) VARIABLE.NowInputHoldKey = noneName;
            }
        }
        public IActionEventData Clone(IActionEventData _eventData)
        {
            Event_SimulatedInput _event = _eventData as Event_SimulatedInput;
            _event.CheckKeyName = mCheckKeyName;
            _event.InputType = mInputType;
            return _event;
        }
    }
}