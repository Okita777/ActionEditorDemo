using System;
using System.Collections.Generic;
using UnityEngine;

namespace ActionEditor.InputSystem
{
    public enum CharacterInputActionEventType
    {
        Pressed,
        Released,
        MoveStarted,
        MoveUpdated,
        MoveStopped,
    }

    public readonly struct CharacterInputActionEvent
    {
        public CharacterInputActionEvent(string actionName, CharacterInputActionEventType eventType)
        {
            ActionName = actionName ?? string.Empty;
            EventType = eventType;
        }

        public string ActionName { get; }

        public CharacterInputActionEventType EventType { get; }
    }

    public struct CharacterInputActionState
    {
        public bool IsHeld;
        public bool WasPressedThisFrame;
        public bool WasReleasedThisFrame;
        public bool WasShortReleasedThisFrame;
        public bool WasLongPressStartedThisFrame;
        public bool WasLongPressReleasedThisFrame;
        public bool WasHoldTickThisFrame;
        public bool IsLongHeld;
        public float HoldDuration;
    }

    public sealed class CharacterInputFrame
    {
        private readonly HashSet<string> _heldActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _downActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _upActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _heldPhysicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _downPhysicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _upPhysicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<CharacterInputActionEvent> _events = new List<CharacterInputActionEvent>();
        private readonly Dictionary<string, CharacterInputActionState> _actionStates = new Dictionary<string, CharacterInputActionState>(StringComparer.OrdinalIgnoreCase);

        public Vector2 MoveAxis { get; internal set; }

        public Vector2 LookAxis { get; internal set; }

        public bool HasMoveInput { get; internal set; }

        public bool HadMoveInputLastFrame { get; internal set; }

        public IReadOnlyCollection<string> HeldActions => _heldActions;

        public IReadOnlyCollection<string> DownActions => _downActions;

        public IReadOnlyCollection<string> UpActions => _upActions;

        public IReadOnlyCollection<string> HeldPhysicalKeys => _heldPhysicalKeys;

        public IReadOnlyCollection<string> DownPhysicalKeys => _downPhysicalKeys;

        public IReadOnlyCollection<string> UpPhysicalKeys => _upPhysicalKeys;

        public IReadOnlyList<CharacterInputActionEvent> Events => _events;

        public bool TryGetActionState(string actionName, out CharacterInputActionState actionState)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                actionState = default(CharacterInputActionState);
                return false;
            }

            return _actionStates.TryGetValue(actionName, out actionState);
        }

        public bool IsActionHeld(string actionName)
        {
            return !string.IsNullOrWhiteSpace(actionName) && _heldActions.Contains(actionName);
        }

        public bool IsActionDown(string actionName)
        {
            return !string.IsNullOrWhiteSpace(actionName) && _downActions.Contains(actionName);
        }

        public bool IsActionUp(string actionName)
        {
            return !string.IsNullOrWhiteSpace(actionName) && _upActions.Contains(actionName);
        }

        public bool IsActionShortReleased(string actionName)
        {
            return TryGetActionState(actionName, out CharacterInputActionState actionState) && actionState.WasShortReleasedThisFrame;
        }

        public bool IsActionLongPressStarted(string actionName)
        {
            return TryGetActionState(actionName, out CharacterInputActionState actionState) && actionState.WasLongPressStartedThisFrame;
        }

        public bool IsActionLongPressReleased(string actionName)
        {
            return TryGetActionState(actionName, out CharacterInputActionState actionState) && actionState.WasLongPressReleasedThisFrame;
        }

        public float GetActionHoldDuration(string actionName)
        {
            return TryGetActionState(actionName, out CharacterInputActionState actionState) ? actionState.HoldDuration : 0f;
        }

        public bool IsActionHoldTick(string actionName)
        {
            return TryGetActionState(actionName, out CharacterInputActionState actionState) && actionState.WasHoldTickThisFrame;
        }

        internal void Reset(bool hadMoveInputLastFrame)
        {
            MoveAxis = Vector2.zero;
            LookAxis = Vector2.zero;
            HasMoveInput = false;
            HadMoveInputLastFrame = hadMoveInputLastFrame;
            _heldActions.Clear();
            _downActions.Clear();
            _upActions.Clear();
            _heldPhysicalKeys.Clear();
            _downPhysicalKeys.Clear();
            _upPhysicalKeys.Clear();
            _events.Clear();
            _actionStates.Clear();
        }

        internal void AddHeldAction(string actionName)
        {
            if (!string.IsNullOrWhiteSpace(actionName))
            {
                _heldActions.Add(actionName);
            }
        }

        internal void AddDownAction(string actionName)
        {
            if (!string.IsNullOrWhiteSpace(actionName) && _downActions.Add(actionName))
            {
                _events.Add(new CharacterInputActionEvent(actionName, CharacterInputActionEventType.Pressed));
            }
        }

        internal void AddUpAction(string actionName)
        {
            if (!string.IsNullOrWhiteSpace(actionName) && _upActions.Add(actionName))
            {
                _events.Add(new CharacterInputActionEvent(actionName, CharacterInputActionEventType.Released));
            }
        }

        internal void AddHeldPhysicalKey(string keyName)
        {
            if (!string.IsNullOrWhiteSpace(keyName))
            {
                _heldPhysicalKeys.Add(keyName);
            }
        }

        internal void AddDownPhysicalKey(string keyName)
        {
            if (!string.IsNullOrWhiteSpace(keyName))
            {
                _downPhysicalKeys.Add(keyName);
            }
        }

        internal void AddUpPhysicalKey(string keyName)
        {
            if (!string.IsNullOrWhiteSpace(keyName))
            {
                _upPhysicalKeys.Add(keyName);
            }
        }

        internal void AddEvent(string actionName, CharacterInputActionEventType eventType)
        {
            if (!string.IsNullOrWhiteSpace(actionName))
            {
                _events.Add(new CharacterInputActionEvent(actionName, eventType));
            }
        }

        internal void SetActionState(string actionName, CharacterInputActionState actionState)
        {
            if (!string.IsNullOrWhiteSpace(actionName))
            {
                _actionStates[actionName] = actionState;
            }
        }
    }

    public interface ICharacterInputProvider
    {
        CharacterInputMapConfig Config { get; }

        CharacterInputFrame CurrentFrame { get; }

        void Tick(float deltaTime);

        bool HasActionBinding(string actionName);
    }

    public sealed class LegacyCharacterInputProvider : ICharacterInputProvider
    {
        private struct ActionEvaluationState
        {
            public bool IsHeld;
            public bool IsLongHeld;
            public float HoldDuration;
        }

        private struct ActionInputAggregate
        {
            public bool IsHeld;
            public bool IsDown;
            public bool IsUp;
        }

        private readonly CharacterInputFrame _frame = new CharacterInputFrame();
        private readonly Dictionary<string, ActionEvaluationState> _actionStates = new Dictionary<string, ActionEvaluationState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ActionInputAggregate> _actionAggregates = new Dictionary<string, ActionInputAggregate>(StringComparer.OrdinalIgnoreCase);
        private readonly float _longPressThreshold;

        public LegacyCharacterInputProvider(CharacterInputMapConfig config, float longPressThreshold)
        {
            Config = config;
            _longPressThreshold = Mathf.Max(0f, longPressThreshold);
        }

        public CharacterInputMapConfig Config { get; }

        public CharacterInputFrame CurrentFrame => _frame;

        public bool HasActionBinding(string actionName)
        {
            return Config != null && Config.HasActionBinding(actionName);
        }

        public void Tick(float deltaTime)
        {
            bool hadMoveInput = _frame.HasMoveInput;
            _frame.Reset(hadMoveInput);

            if (Config == null)
            {
                return;
            }

            _actionAggregates.Clear();
            TickButtons();
            EvaluateActionStates(deltaTime);
            TickMoveAndLook();
            EmitMoveEvents();
        }

        private void TickButtons()
        {
            if (Config.ButtonBindings == null)
            {
                return;
            }

            for (int i = 0; i < Config.ButtonBindings.Count; i++)
            {
                CharacterInputButtonBinding binding = Config.ButtonBindings[i];
                if (binding == null)
                {
                    continue;
                }

                TickButtonSource(binding.ActionName, binding.PcKey, "PC");
                TickGamepadButtonSource(binding.ActionName, binding.GamepadButton);
            }
        }

        private void TickButtonSource(string actionName, KeyCode keyCode, string sourcePrefix)
        {
            if (string.IsNullOrWhiteSpace(actionName) || keyCode == KeyCode.None)
            {
                return;
            }

            bool isHeld = Input.GetKey(keyCode);
            bool isDown = Input.GetKeyDown(keyCode);
            bool isUp = Input.GetKeyUp(keyCode);
            string physicalName = $"{sourcePrefix}:{keyCode}";

            ActionInputAggregate aggregate;
            if (!_actionAggregates.TryGetValue(actionName, out aggregate))
            {
                aggregate = default(ActionInputAggregate);
            }

            aggregate.IsHeld |= isHeld;
            aggregate.IsDown |= isDown;
            aggregate.IsUp |= isUp;
            _actionAggregates[actionName] = aggregate;

            if (isHeld)
            {
                _frame.AddHeldAction(actionName);
                _frame.AddHeldPhysicalKey(physicalName);
            }

            if (isDown)
            {
                _frame.AddDownAction(actionName);
                _frame.AddDownPhysicalKey(physicalName);
            }

            if (isUp)
            {
                _frame.AddUpAction(actionName);
                _frame.AddUpPhysicalKey(physicalName);
            }
        }

        private void TickGamepadButtonSource(string actionName, CharacterInputGamepadButton button)
        {
            if (string.IsNullOrWhiteSpace(actionName) || button == CharacterInputGamepadButton.None)
            {
                return;
            }

            TickButtonSource(actionName, MapGamepadButton(button), "Gamepad");
        }

        private void EvaluateActionStates(float deltaTime)
        {
            foreach (KeyValuePair<string, ActionInputAggregate> pair in _actionAggregates)
            {
                string actionName = pair.Key;
                ActionInputAggregate aggregate = pair.Value;
                ActionEvaluationState runtimeState;
                if (!_actionStates.TryGetValue(actionName, out runtimeState))
                {
                    runtimeState = default(ActionEvaluationState);
                }

                CharacterInputActionState frameState = new CharacterInputActionState();

                if (aggregate.IsDown && !runtimeState.IsHeld)
                {
                    runtimeState.IsHeld = true;
                    runtimeState.IsLongHeld = false;
                    runtimeState.HoldDuration = 0f;
                    frameState.WasPressedThisFrame = true;
                    _frame.AddEvent(actionName, CharacterInputActionEventType.Pressed);
                }

                if (runtimeState.IsHeld && aggregate.IsHeld)
                {
                    runtimeState.HoldDuration += Mathf.Max(0f, deltaTime);
                    frameState.WasHoldTickThisFrame = true;
                }

                if (runtimeState.IsHeld && !runtimeState.IsLongHeld && runtimeState.HoldDuration >= _longPressThreshold)
                {
                    runtimeState.IsLongHeld = true;
                    frameState.WasLongPressStartedThisFrame = true;
                }

                frameState.IsHeld = runtimeState.IsHeld && aggregate.IsHeld;
                frameState.IsLongHeld = runtimeState.IsLongHeld;
                frameState.HoldDuration = runtimeState.IsHeld ? runtimeState.HoldDuration : 0f;

                if (aggregate.IsUp && runtimeState.IsHeld)
                {
                    frameState.WasReleasedThisFrame = true;
                    if (runtimeState.IsLongHeld)
                    {
                        frameState.WasLongPressReleasedThisFrame = true;
                    }
                    else
                    {
                        frameState.WasShortReleasedThisFrame = true;
                    }

                    runtimeState.IsHeld = false;
                    runtimeState.IsLongHeld = false;
                    runtimeState.HoldDuration = 0f;
                    frameState.IsHeld = false;
                    frameState.IsLongHeld = false;
                    frameState.HoldDuration = 0f;
                    _frame.AddEvent(actionName, CharacterInputActionEventType.Released);
                }

                if (runtimeState.IsHeld && !aggregate.IsHeld && !aggregate.IsUp)
                {
                    frameState.IsHeld = true;
                    frameState.IsLongHeld = runtimeState.IsLongHeld;
                    frameState.HoldDuration = runtimeState.HoldDuration;
                }

                _actionStates[actionName] = runtimeState;
                _frame.SetActionState(actionName, frameState);
            }
        }

        private void TickMoveAndLook()
        {
            Vector2 moveAxis = ReadMoveAxis();
            _frame.MoveAxis = moveAxis;
            _frame.HasMoveInput = moveAxis.sqrMagnitude > 0.0001f;
            _frame.LookAxis = ReadLookAxis();
        }

        private Vector2 ReadMoveAxis()
        {
            Vector2 axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (axis.sqrMagnitude > 1f)
            {
                axis.Normalize();
            }

            return axis;
        }

        private Vector2 ReadLookAxis()
        {
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        }

        private static KeyCode MapGamepadButton(CharacterInputGamepadButton button)
        {
            switch (button)
            {
                case CharacterInputGamepadButton.South:
                    return KeyCode.JoystickButton0;
                case CharacterInputGamepadButton.East:
                    return KeyCode.JoystickButton1;
                case CharacterInputGamepadButton.West:
                    return KeyCode.JoystickButton2;
                case CharacterInputGamepadButton.North:
                    return KeyCode.JoystickButton3;
                case CharacterInputGamepadButton.LeftShoulder:
                    return KeyCode.JoystickButton4;
                case CharacterInputGamepadButton.RightShoulder:
                    return KeyCode.JoystickButton5;
                case CharacterInputGamepadButton.Select:
                    return KeyCode.JoystickButton6;
                case CharacterInputGamepadButton.Start:
                    return KeyCode.JoystickButton7;
                case CharacterInputGamepadButton.LeftStickPress:
                    return KeyCode.JoystickButton8;
                case CharacterInputGamepadButton.RightStickPress:
                    return KeyCode.JoystickButton9;
                default:
                    return KeyCode.None;
            }
        }

        private void EmitMoveEvents()
        {
            if (_frame.HasMoveInput && !_frame.HadMoveInputLastFrame)
            {
                _frame.AddEvent("Move", CharacterInputActionEventType.MoveStarted);
                return;
            }

            if (_frame.HasMoveInput)
            {
                _frame.AddEvent("Move", CharacterInputActionEventType.MoveUpdated);
                return;
            }

            if (_frame.HadMoveInputLastFrame)
            {
                _frame.AddEvent("Move", CharacterInputActionEventType.MoveStopped);
            }
        }
    }
}