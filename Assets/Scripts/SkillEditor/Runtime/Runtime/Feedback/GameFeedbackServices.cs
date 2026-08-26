using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace AsiSkillEditor.RunTime
{
    public readonly struct VfxPlayArgs
    {
        public readonly GameObject Prefab;
        public readonly VfxPlaySpace Space;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;
        public readonly Transform FollowTarget;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly float Lifetime;
        public readonly bool UseUnscaledTime;
        public readonly bool AutoReleaseWhenFinished;

        public VfxPlayArgs(GameObject prefab, VfxPlaySpace space, Vector3 position, Quaternion rotation,
            Vector3 scale, Transform followTarget, Vector3 localPosition, Quaternion localRotation,
            float lifetime, bool useUnscaledTime, bool autoReleaseWhenFinished = false)
        {
            Prefab = prefab;
            Space = space;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            FollowTarget = followTarget;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            Lifetime = lifetime;
            UseUnscaledTime = useUnscaledTime;
            AutoReleaseWhenFinished = autoReleaseWhenFinished;
        }
    }

    public interface IVfxPlayback
    {
        bool IsAlive { get; }
    }

    public enum VfxStopBehavior
    {
        StopEmitting = 0,
        StopAndClear = 1,
    }

    public interface IVfxService
    {
        IVfxPlayback Play(in VfxPlayArgs args);
        void Stop(IVfxPlayback playback, VfxStopBehavior behavior, float tailTimeout = 0f);
        void Stop(GameObject prefab, Transform followTarget = null);
        void StopAll();
    }

    public readonly struct AudioPlayArgs
    {
        public readonly AudioClip Clip;
        public readonly AudioMixerGroup MixerGroup;
        public readonly AudioPlaySpace Space;
        public readonly Vector3 Position;
        public readonly Transform FollowTarget;
        public readonly float Volume;
        public readonly float Pitch;
        public readonly float SpatialBlend;
        public readonly float MinDistance;
        public readonly float MaxDistance;

        public AudioPlayArgs(AudioClip clip, AudioMixerGroup mixerGroup, AudioPlaySpace space,
            Vector3 position, Transform followTarget, float volume, float pitch,
            float spatialBlend, float minDistance, float maxDistance)
        {
            Clip = clip;
            MixerGroup = mixerGroup;
            Space = space;
            Position = position;
            FollowTarget = followTarget;
            Volume = volume;
            Pitch = pitch;
            SpatialBlend = spatialBlend;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
        }
    }

    public interface IAudioService
    {
        void Play(in AudioPlayArgs args);
        void Stop(AudioClip clip, Transform followTarget = null);
        void StopAll();
    }

    [DisallowMultipleComponent]
    public sealed class GameFeedbackServiceHost : MonoBehaviour
    {
        private static GameFeedbackServiceHost s_instance;
        private VfxService _vfxService;
        private AudioService _audioService;

        public static GameFeedbackServiceHost Instance => ResolveOrCreate();
        public IVfxService Vfx => _vfxService;
        public IAudioService Audio => _audioService;

        private void Awake()
        {
            s_instance = this;
            EnsureServices();
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        public static GameFeedbackServiceHost ResolveOrCreate()
        {
            if (s_instance != null)
            {
                s_instance.EnsureServices();
                return s_instance;
            }

            s_instance = FindObjectOfType<GameFeedbackServiceHost>();
            if (s_instance == null)
            {
                GameObject host = new GameObject("Game Feedback Services");
                s_instance = host.AddComponent<GameFeedbackServiceHost>();
            }

            s_instance.EnsureServices();
            return s_instance;
        }

        private void EnsureServices()
        {
            _vfxService ??= GetComponent<VfxService>() ?? gameObject.AddComponent<VfxService>();
            _audioService ??= GetComponent<AudioService>() ?? gameObject.AddComponent<AudioService>();
        }
    }

    [DisallowMultipleComponent]
    public sealed class VfxService : MonoBehaviour, IVfxService
    {
        private sealed class PoolEntry
        {
            public GameObject Prefab;
            public ObjectPool<PooledVfx> Pool;
        }

        private sealed class PooledVfx : MonoBehaviour, IVfxPlayback
        {
            public PoolEntry Entry;
            public ParticleSystem[] Particles;
            public Transform FollowTarget;
            public float RemainingLifetime;
            public bool UseUnscaledTime;
            public bool AutoReleaseWhenFinished;
            public bool IsStopping;
            public bool IsReleased = true;

            public bool IsAlive => !IsReleased;
        }

        private readonly Dictionary<GameObject, PoolEntry> _pools = new Dictionary<GameObject, PoolEntry>();
        private readonly List<PooledVfx> _active = new List<PooledVfx>();
        private Transform _poolRoot;

        private void Update()
        {
            float scaledDelta = Time.deltaTime;
            float unscaledDelta = Time.unscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                PooledVfx instance = _active[i];
                if (instance == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                if (instance.FollowTarget == null && instance.transform.parent != null && instance.transform.parent != transform)
                {
                    instance.transform.SetParent(null, true);
                }

                float deltaTime = instance.UseUnscaledTime ? unscaledDelta : scaledDelta;
                if (instance.IsStopping)
                {
                    instance.RemainingLifetime -= deltaTime;
                    if (!AreParticlesAlive(instance) || instance.RemainingLifetime <= 0f)
                    {
                        Release(instance);
                    }
                    continue;
                }

                if (instance.AutoReleaseWhenFinished)
                {
                    instance.RemainingLifetime -= deltaTime;
                    if (!AreParticlesAlive(instance) || instance.RemainingLifetime <= 0f)
                    {
                        Release(instance);
                    }
                    continue;
                }

                instance.RemainingLifetime -= deltaTime;
                if (instance.RemainingLifetime <= 0f)
                {
                    Release(instance);
                }
            }
        }

        private void OnDestroy()
        {
            StopAll();
        }

        public IVfxPlayback Play(in VfxPlayArgs args)
        {
            if (args.Prefab == null || (!args.AutoReleaseWhenFinished && args.Lifetime <= 0f))
            {
                return null;
            }

            PoolEntry entry = GetOrCreatePool(args.Prefab);
            PooledVfx instance = entry?.Pool.Get();
            if (instance == null || instance.Particles == null || instance.Particles.Length == 0)
            {
                if (instance != null)
                {
                    entry.Pool.Release(instance);
                }
                return null;
            }

            instance.FollowTarget = args.Space == VfxPlaySpace.FollowTarget ? args.FollowTarget : null;
            instance.RemainingLifetime = args.Lifetime;
            instance.UseUnscaledTime = args.UseUnscaledTime;
            instance.AutoReleaseWhenFinished = args.AutoReleaseWhenFinished;
            instance.IsStopping = false;
            instance.IsReleased = false;
            if (instance.FollowTarget != null)
            {
                instance.transform.SetParent(instance.FollowTarget, false);
                instance.transform.localPosition = args.LocalPosition;
                instance.transform.localRotation = args.LocalRotation;
            }
            else
            {
                instance.transform.SetParent(null, false);
                instance.transform.SetPositionAndRotation(args.Position, args.Rotation);
            }
            instance.transform.localScale = args.Scale;

            for (int i = 0; i < instance.Particles.Length; i++)
            {
                ParticleSystem particle = instance.Particles[i];
                if (particle == null) continue;
                ParticleSystem.MainModule main = particle.main;
                main.useUnscaledTime = args.UseUnscaledTime;
                particle.Clear(true);
                particle.Play(true);
            }

            _active.Add(instance);
            return instance;
        }

        public void Stop(IVfxPlayback playback, VfxStopBehavior behavior, float tailTimeout = 0f)
        {
            if (!(playback is PooledVfx instance) || instance.IsReleased || !_active.Contains(instance))
            {
                return;
            }

            if (behavior == VfxStopBehavior.StopAndClear)
            {
                Release(instance);
                return;
            }

            for (int i = 0; i < instance.Particles.Length; i++)
            {
                instance.Particles[i]?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            instance.IsStopping = true;
            instance.AutoReleaseWhenFinished = false;
            instance.RemainingLifetime = Mathf.Max(0.01f, tailTimeout);
        }

        public void Stop(GameObject prefab, Transform followTarget = null)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                PooledVfx instance = _active[i];
                if (instance != null && instance.Entry != null && instance.Entry.Prefab == prefab &&
                    (followTarget == null || instance.FollowTarget == followTarget))
                {
                    Release(instance);
                }
            }
        }

        public void StopAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null)
                {
                    Release(_active[i]);
                }
            }
            _active.Clear();
        }

        private PoolEntry GetOrCreatePool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out PoolEntry existing))
            {
                return existing;
            }

            EnsurePoolRoot();
            PoolEntry entry = new PoolEntry { Prefab = prefab };
            entry.Pool = new ObjectPool<PooledVfx>(
                () => CreateInstance(entry),
                instance => instance.gameObject.SetActive(true),
                instance =>
                {
                    instance.gameObject.SetActive(false);
                    instance.transform.SetParent(_poolRoot, false);
                },
                instance => Destroy(instance.gameObject),
                true, 2, 32);
            _pools.Add(prefab, entry);
            return entry;
        }

        private PooledVfx CreateInstance(PoolEntry entry)
        {
            GameObject instanceObject = Instantiate(entry.Prefab, _poolRoot);
            instanceObject.name = entry.Prefab.name + " (Pooled)";
            PooledVfx instance = instanceObject.AddComponent<PooledVfx>();
            instance.Entry = entry;
            instance.Particles = instanceObject.GetComponentsInChildren<ParticleSystem>(true);
            instanceObject.SetActive(false);
            return instance;
        }

        private void Release(PooledVfx instance)
        {
            if (instance == null || instance.IsReleased)
            {
                return;
            }

            _active.Remove(instance);
            for (int i = 0; i < instance.Particles.Length; i++)
            {
                instance.Particles[i]?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            instance.FollowTarget = null;
            instance.AutoReleaseWhenFinished = false;
            instance.IsStopping = false;
            instance.IsReleased = true;
            instance.transform.localScale = Vector3.one;
            instance.Entry.Pool.Release(instance);
        }

        private static bool AreParticlesAlive(PooledVfx instance)
        {
            if (instance == null || instance.Particles == null)
            {
                return false;
            }

            for (int i = 0; i < instance.Particles.Length; i++)
            {
                ParticleSystem particle = instance.Particles[i];
                if (particle != null && particle.IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsurePoolRoot()
        {
            if (_poolRoot != null) return;
            GameObject root = new GameObject("VFX Pool");
            root.transform.SetParent(transform, false);
            _poolRoot = root.transform;
        }
    }

    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        private sealed class PooledAudioSource : MonoBehaviour
        {
            public AudioSource Source;
            public Transform FollowTarget;
        }

        private readonly List<PooledAudioSource> _active = new List<PooledAudioSource>();
        private ObjectPool<PooledAudioSource> _pool;
        private Transform _poolRoot;

        private void Awake()
        {
            EnsurePool();
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                PooledAudioSource voice = _active[i];
                if (voice == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                if (voice.FollowTarget != null)
                {
                    voice.transform.position = voice.FollowTarget.position;
                }

                if (!voice.Source.isPlaying)
                {
                    Release(voice);
                }
            }
        }

        private void OnDestroy()
        {
            StopAll();
        }

        public void Play(in AudioPlayArgs args)
        {
            if (args.Clip == null)
            {
                return;
            }

            EnsurePool();
            PooledAudioSource voice = _pool.Get();
            AudioSource source = voice.Source;
            voice.FollowTarget = args.Space == AudioPlaySpace.FollowTarget ? args.FollowTarget : null;
            voice.transform.position = voice.FollowTarget != null ? voice.FollowTarget.position : args.Position;
            source.clip = args.Clip;
            source.outputAudioMixerGroup = args.MixerGroup;
            source.volume = Mathf.Clamp01(args.Volume);
            source.pitch = Mathf.Clamp(args.Pitch, 0.01f, 3f);
            source.spatialBlend = args.Space == AudioPlaySpace.TwoD ? 0f : Mathf.Clamp01(args.SpatialBlend);
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = Mathf.Max(0.01f, args.MinDistance);
            source.maxDistance = Mathf.Max(source.minDistance, args.MaxDistance);
            source.dopplerLevel = 0f;
            source.loop = false;
            source.Play();
            _active.Add(voice);
        }

        public void Stop(AudioClip clip, Transform followTarget = null)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                PooledAudioSource voice = _active[i];
                if (voice != null && voice.Source.clip == clip &&
                    (followTarget == null || voice.FollowTarget == followTarget))
                {
                    Release(voice);
                }
            }
        }

        public void StopAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null)
                {
                    Release(_active[i]);
                }
            }
            _active.Clear();
        }

        private void EnsurePool()
        {
            if (_pool != null) return;
            GameObject root = new GameObject("Audio Pool");
            root.transform.SetParent(transform, false);
            _poolRoot = root.transform;
            _pool = new ObjectPool<PooledAudioSource>(CreateVoice,
                voice => voice.gameObject.SetActive(true),
                voice =>
                {
                    voice.gameObject.SetActive(false);
                    voice.transform.SetParent(_poolRoot, false);
                },
                voice => Destroy(voice.gameObject), true, 8, 32);
        }

        private PooledAudioSource CreateVoice()
        {
            GameObject voiceObject = new GameObject("Pooled Audio Source");
            voiceObject.transform.SetParent(_poolRoot, false);
            PooledAudioSource voice = voiceObject.AddComponent<PooledAudioSource>();
            voice.Source = voiceObject.AddComponent<AudioSource>();
            voice.Source.playOnAwake = false;
            voiceObject.SetActive(false);
            return voice;
        }

        private void Release(PooledAudioSource voice)
        {
            _active.Remove(voice);
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Source.outputAudioMixerGroup = null;
            voice.Source.volume = 1f;
            voice.Source.pitch = 1f;
            voice.Source.spatialBlend = 0f;
            voice.Source.rolloffMode = AudioRolloffMode.Logarithmic;
            voice.Source.minDistance = 1f;
            voice.Source.maxDistance = 500f;
            voice.Source.dopplerLevel = 0f;
            voice.Source.loop = false;
            voice.FollowTarget = null;
            _pool.Release(voice);
        }
    }
}
