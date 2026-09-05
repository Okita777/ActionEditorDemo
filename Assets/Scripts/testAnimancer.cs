using Animancer;
using UnityEngine;

/// <summary>
/// 最小 Animancer 播放测试。将目标动画拖入 Inspector，运行时按 T 从头播放。
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public sealed class testAnimancer : MonoBehaviour
{
	[SerializeField] private AnimationClip _targetAnimation;
	[SerializeField] private KeyCode _playKey = KeyCode.T;

	private AnimancerComponent _animancer;

	private void Awake()
	{
		_animancer = GetComponent<AnimancerComponent>();
	}

	private void Update()
	{
		if (!Input.GetKeyDown(_playKey))
		{
			return;
		}

		if (_targetAnimation == null)
		{
			Debug.LogWarning("testAnimancer：请先在 Inspector 中拖入目标动画。", this);
			return;
		}

		AnimancerState state = _animancer.Play(_targetAnimation);
		state.Time = 0f;
		state.Speed = 1f;
	}
}
