using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ActionEditor.InputSystem
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class CharacterInputDriver : MonoBehaviour
    {
        [SerializeField] private CharacterInputMapConfig _config;
        [SerializeField] private float _longPressThreshold = 0.35f;

        private ICharacterInputProvider _provider;

        public CharacterInputMapConfig Config => _config;

        public CharacterInputFrame CurrentFrame => _provider != null ? _provider.CurrentFrame : null;

        public bool IsReady => _provider != null;

        public float LongPressThreshold
        {
            get => _longPressThreshold;
            set => _longPressThreshold = Mathf.Max(0f, value);
        }

        private void OnEnable()
        {
            EnsureConfigReference();
            RebuildProvider();
        }

        private void OnValidate()
        {
            EnsureConfigReference();
            RebuildProvider();
        }

        private void Update()
        {
            _provider?.Tick(Time.deltaTime);
        }

        public void RebuildProvider()
        {
            EnsureConfigReference();
            _provider = _config != null ? new LegacyCharacterInputProvider(_config, _longPressThreshold) : null;
        }

        private void EnsureConfigReference()
        {
#if UNITY_EDITOR
            if (_config == null)
            {
                _config = AssetDatabase.LoadAssetAtPath<CharacterInputMapConfig>(CharacterInputConstants.MainConfigAssetPath);
                if (_config != null && !Application.isPlaying)
                {
                    EditorUtility.SetDirty(this);
                }
            }
#endif
        }

        public bool HasActionBinding(string actionName)
        {
            return _provider != null && _provider.HasActionBinding(actionName);
        }

        public bool IsActionDown(string actionName)
        {
            CharacterInputFrame frame = CurrentFrame;
            return frame != null && frame.IsActionDown(actionName);
        }

        public bool IsActionUp(string actionName)
        {
            CharacterInputFrame frame = CurrentFrame;
            return frame != null && frame.IsActionUp(actionName);
        }

        public bool IsActionHeld(string actionName)
        {
            CharacterInputFrame frame = CurrentFrame;
            return frame != null && frame.IsActionHeld(actionName);
        }

        public bool IsActionShortReleased(string actionName)
        {
            CharacterInputFrame frame = CurrentFrame;
            return frame != null && frame.IsActionShortReleased(actionName);
        }

        public bool IsActionLongPressStarted(string actionName)
        {
            CharacterInputFrame frame = CurrentFrame;
            return frame != null && frame.IsActionLongPressStarted(actionName);
        }

        public bool IsActionLongPressReleased(string actionName)
        {
            CharacterInputFrame frame = CurrentFrame;
            return frame != null && frame.IsActionLongPressReleased(actionName);
        }

        public bool IsActionHoldTick(string actionName)
        {
            CharacterInputFrame frame = CurrentFrame;
            return frame != null && frame.IsActionHoldTick(actionName);
        }

        public float GetActionHoldDuration(string actionName)
        {
            CharacterInputFrame frame = CurrentFrame;
            return frame != null ? frame.GetActionHoldDuration(actionName) : 0f;
        }
    }
}