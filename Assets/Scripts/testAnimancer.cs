using Animancer;
using UnityEngine;

/// <summary>
/// 最小 Animancer 动画过渡测试。
/// 按 T 从头播放起始动画；起始动画播放完毕后，自动过渡到目标动画。
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public sealed class testAnimancer : MonoBehaviour
{
	[Header("Animations")]
	[SerializeField] private AnimationClip _sourceAnimation = null;
	[SerializeField] private AnimationClip _targetAnimation = null;

	[Header("Transition")]
	[SerializeField, Min(0f)] private float _fadeDuration = 0.25f;
	[SerializeField] private FadeMode _fadeMode = FadeMode.FixedDuration;
	[SerializeField, Min(0f)] private float _sourceStartTime = 0f;
	[SerializeField, Min(0f)] private float _targetStartTime = 0f;
	[SerializeField] private float _playbackSpeed = 1f;
	[SerializeField] private bool _targetLoop = true;

	[Header("Controls")]
	[SerializeField] private KeyCode _playSourceKey = KeyCode.T;
	[SerializeField] private bool _autoTransition = true;
	[SerializeField] private bool _resetTransformWhenPlayingSource = true;

	[Header("Runtime Observation")]
	[SerializeField] private string _currentPhase = "Not Started";
	[SerializeField] private float _sourceTime;
	[SerializeField] private float _sourceWeight;
	[SerializeField] private float _targetTime;
	[SerializeField] private float _targetWeight;
	[SerializeField] private Vector3 _frameWorldDisplacement;
	[SerializeField] private float _frameWorldYaw;
	[SerializeField] private Vector3 _totalWorldDisplacement;
	[SerializeField] private float _totalWorldYaw;

	private AnimancerComponent _animancer;
	private AnimancerState _sourceState;
	private AnimancerState _targetState;
	private Vector3 _initialPosition;
	private Quaternion _initialRotation;
	private Vector3 _previousPosition;
	private Quaternion _previousRotation;
	private bool _hasStarted;
	private bool _hasTransitioned;
	private bool _targetReachedEnd;
	private float _sourceRemainingTime;
	private readonly object _sourceStateKey = new object();
	private readonly object _targetStateKey = new object();

	private void Awake()
	{
		_animancer = GetComponent<AnimancerComponent>();
		_initialPosition = transform.position;
		_initialRotation = transform.rotation;
		ResetObservationBaseline();
	}

	private void Update()
	{
		if (Input.GetKeyDown(_playSourceKey))
		{
			PlaySource();
		}

		UpdateSourcePlayback();
		UpdateTargetPlayback();
		UpdateStateObservation();
	}

	private void LateUpdate()
	{
		Vector3 currentPosition = transform.position;
		Quaternion currentRotation = transform.rotation;

		_frameWorldDisplacement = currentPosition - _previousPosition;
		_frameWorldYaw = Mathf.DeltaAngle(_previousRotation.eulerAngles.y, currentRotation.eulerAngles.y);
		_totalWorldDisplacement = currentPosition - _initialPosition;
		_totalWorldYaw = Mathf.DeltaAngle(_initialRotation.eulerAngles.y, currentRotation.eulerAngles.y);

		_previousPosition = currentPosition;
		_previousRotation = currentRotation;
	}

	[ContextMenu("Play Source")]
	private void PlaySource()
	{
		if (_sourceAnimation == null)
		{
			Debug.LogWarning("testAnimancer：请先在 Inspector 中拖入起始动画。", this);
			return;
		}

		if (_resetTransformWhenPlayingSource)
		{
			transform.SetPositionAndRotation(_initialPosition, _initialRotation);
		}

		// NamedAnimancerComponent/HybridAnimancerComponent 默认使用 clip.name 作为 Key。
		// 不同 FBX 中的动画子资源经常都叫 "Scene"，必须为两个测试状态提供独立 Key。
		_sourceState = _animancer.States.GetOrCreate(_sourceStateKey, _sourceAnimation);
		_sourceState = _animancer.Play(_sourceState);
		_sourceState.Time = Mathf.Clamp(_sourceStartTime, 0f, _sourceAnimation.length);
		_sourceState.Speed = _playbackSpeed;
		float remainingClipTime = _playbackSpeed >= 0f
			? _sourceAnimation.length - _sourceState.Time
			: _sourceState.Time;
		_sourceRemainingTime = Mathf.Max(0f, remainingClipTime) /
			Mathf.Max(0.0001f, Mathf.Abs(_playbackSpeed));

		_targetState = null;
		_hasStarted = true;
		_hasTransitioned = false;
		_targetReachedEnd = false;
		_currentPhase = $"Source: {_sourceAnimation.name}";
		ResetObservationBaseline();
	}

	[ContextMenu("Transition To Target")]
	private void TransitionToTarget()
	{
		if (!_hasStarted || _sourceState == null)
		{
			Debug.LogWarning("testAnimancer：请先播放起始动画，再测试过渡。", this);
			return;
		}

		if (_targetAnimation == null)
		{
			Debug.LogWarning("testAnimancer：请先在 Inspector 中拖入目标动画。", this);
			return;
		}

		_targetState = _animancer.States.GetOrCreate(_targetStateKey, _targetAnimation);
		_targetState = _fadeDuration > 0f
			? _animancer.Play(_targetState, _fadeDuration, _fadeMode)
			: _animancer.Play(_targetState);
		_targetState.Time = Mathf.Clamp(_targetStartTime, 0f, _targetAnimation.length);
		_targetState.Speed = _playbackSpeed;

		_hasTransitioned = true;
		_targetReachedEnd = false;
		_currentPhase = $"Transition: {_sourceAnimation.name} -> {_targetAnimation.name}";
	}

	private void UpdateSourcePlayback()
	{
		if (!_autoTransition || !_hasStarted || _hasTransitioned || _sourceState == null)
		{
			return;
		}

		_sourceRemainingTime -= Time.deltaTime;
		if (_sourceRemainingTime <= 0f)
		{
			_sourceRemainingTime = 0f;
			TransitionToTarget();
		}
	}

	private void UpdateTargetPlayback()
	{
		if (!_hasTransitioned || _targetReachedEnd || _targetState == null)
		{
			return;
		}

		bool reachedEnd = _targetState.Speed >= 0f
			? _targetState.NormalizedTime >= 1f
			: _targetState.NormalizedTime <= 0f;
		if (!reachedEnd)
		{
			return;
		}

		if (_targetLoop)
		{
			float length = Mathf.Max(_targetState.Length, 0.0001f);
			float overflow = _targetState.Speed >= 0f
				? Mathf.Max(0f, (float)_targetState.Time - length)
				: Mathf.Max(0f, -(float)_targetState.Time);
			_targetState.Time = _targetState.Speed >= 0f ? overflow : length - overflow;
			_targetState.IsPlaying = true;
			_currentPhase = $"Target Loop: {_targetAnimation.name}";
			return;
		}

		_targetReachedEnd = true;
		_targetState.Time = _targetState.Speed >= 0f ? _targetState.Length : 0f;
		_targetState.IsPlaying = false;
		_currentPhase = $"Target End: {_targetAnimation.name}";
	}

	private void UpdateStateObservation()
	{
		_sourceTime = _sourceState != null ? (float)_sourceState.Time : 0f;
		_sourceWeight = _sourceState != null ? _sourceState.Weight : 0f;
		_targetTime = _targetState != null ? (float)_targetState.Time : 0f;
		_targetWeight = _targetState != null ? _targetState.Weight : 0f;

		if (_hasTransitioned && !_targetReachedEnd && _targetState != null && _targetState.Weight >= 0.999f)
		{
			_currentPhase = _targetLoop
				? $"Target Loop: {_targetAnimation.name}"
				: $"Target: {_targetAnimation.name}";
		}
	}

	private void ResetObservationBaseline()
	{
		_previousPosition = transform.position;
		_previousRotation = transform.rotation;
		_frameWorldDisplacement = Vector3.zero;
		_frameWorldYaw = 0f;
		_totalWorldDisplacement = transform.position - _initialPosition;
		_totalWorldYaw = Mathf.DeltaAngle(_initialRotation.eulerAngles.y, transform.rotation.eulerAngles.y);
	}
}
