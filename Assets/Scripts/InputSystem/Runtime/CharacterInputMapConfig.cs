using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActionEditor.InputSystem
{
    public static class CharacterInputConstants
    {
        public const string MainConfigAssetPath = "Assets/GamePlay/InputSystem/Config/CharacterInputMapConfig.asset";
    }

    public enum CharacterInputActionValueType
    {
        Button,
    }

    public enum CharacterInputGamepadButton
    {
        None = 0,
        South,
        East,
        West,
        North,
        LeftShoulder,
        RightShoulder,
        Select,
        Start,
        LeftStickPress,
        RightStickPress,
    }

    [Serializable]
    public sealed class CharacterInputActionDefinition
    {
        public string ActionName = "Action";
        public CharacterInputActionValueType ValueType = CharacterInputActionValueType.Button;
    }

    [Serializable]
    public sealed class CharacterInputButtonBinding
    {
        public string ActionName = "Action";
        [FormerlySerializedAs("Key")]
        public KeyCode PcKey = KeyCode.None;
        public CharacterInputGamepadButton GamepadButton = CharacterInputGamepadButton.None;

        [FormerlySerializedAs("MouseButton")]
        [HideInInspector]
        public int LegacyMouseButton = -1;
    }

    [CreateAssetMenu(fileName = "CharacterInputMapConfig", menuName = "ActionEditor/Input/Character Input Map")]
    public sealed class CharacterInputMapConfig : ScriptableObject
    {
        public List<CharacterInputActionDefinition> Actions = new List<CharacterInputActionDefinition>();
        public List<CharacterInputButtonBinding> ButtonBindings = new List<CharacterInputButtonBinding>();

        public bool HasButtonBinding(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName) || ButtonBindings == null)
            {
                return false;
            }

            for (int i = 0; i < ButtonBindings.Count; i++)
            {
                CharacterInputButtonBinding binding = ButtonBindings[i];
                if (binding == null)
                {
                    continue;
                }

                if (string.Equals(binding.ActionName, actionName, StringComparison.OrdinalIgnoreCase) &&
                    (binding.PcKey != KeyCode.None || binding.GamepadButton != CharacterInputGamepadButton.None))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasActionBinding(string actionName)
        {
            return HasButtonBinding(actionName);
        }

        private void OnEnable()
        {
            if (Actions == null)
            {
                Actions = new List<CharacterInputActionDefinition>();
            }

            if (ButtonBindings == null)
            {
                ButtonBindings = new List<CharacterInputButtonBinding>();
            }

            MigrateLegacyButtonBindings();
        }

        private void OnValidate()
        {
            MigrateLegacyButtonBindings();
        }

        private void MigrateLegacyButtonBindings()
        {
            if (ButtonBindings == null)
            {
                return;
            }

            for (int i = 0; i < ButtonBindings.Count; i++)
            {
                CharacterInputButtonBinding binding = ButtonBindings[i];
                if (binding == null)
                {
                    continue;
                }

                if (binding.PcKey == KeyCode.None && binding.LegacyMouseButton >= 0)
                {
                    binding.PcKey = ConvertLegacyMouseButton(binding.LegacyMouseButton);
                }

                binding.LegacyMouseButton = -1;

                if (string.IsNullOrWhiteSpace(binding.ActionName) && Actions != null && Actions.Count > 0)
                {
                    binding.ActionName = Actions[0].ActionName;
                }
            }
        }

        private static KeyCode ConvertLegacyMouseButton(int mouseButton)
        {
            switch (mouseButton)
            {
                case 0:
                    return KeyCode.Mouse0;
                case 1:
                    return KeyCode.Mouse1;
                case 2:
                    return KeyCode.Mouse2;
                default:
                    return KeyCode.None;
            }
        }
    }
}