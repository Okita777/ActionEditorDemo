using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace AsiActionEngine.RunTime
{
    public partial class EngineResourcesManager : SingletonBase<EngineResourcesManager>
    {
        #region MyStruct
        private struct AutoDestory
        {
            public Component m_target;
            public ObjectPool<Component> m_objectPool;
            public float m_Life;

            public AutoDestory(Component target, ObjectPool<Component> objectPool, float _life)
            {
                m_target = target;
                m_objectPool = objectPool;
                m_Life = _life;
            }

            public bool UpdateLife(float delta)
            {
                m_Life -= delta;
                return m_Life < 0;
            }

            public void ReSetLife(float _life)
            {
                m_Life = _life;
            }
        }

        private struct WaitLoadObj
        {
            public Action<Component> callback;
            public float destoryTime;

            public WaitLoadObj(Action<Component> _callback, float _destoryTime)
            {
                callback = _callback;
                destoryTime = _destoryTime;
            }
        }

        private sealed class PendingLoad
        {
            public readonly int Epoch;
            public readonly string Path;
            public readonly List<WaitLoadObj> Waiters = new List<WaitLoadObj>();
            public bool IsCancelled;
            public bool IsCompleted;

            public PendingLoad(int epoch, string path)
            {
                Epoch = epoch;
                Path = path;
            }

            public void Cancel()
            {
                IsCancelled = true;
            }
        }
        #endregion

        public Dictionary<string, ObjectPool<Component>> ObjectPool => m_ObjectPool;
        public Dictionary<int, Transform> ObjectPoolParentCache => m_ObjectPoolParentCache;

        private readonly Dictionary<string, ObjectPool<Component>> m_ObjectPool = new Dictionary<string, ObjectPool<Component>>();
        private readonly Dictionary<string, PendingLoad> m_WaitLoad = new Dictionary<string, PendingLoad>();
        private readonly Dictionary<Component, AutoDestory> m_ConDestorData = new Dictionary<Component, AutoDestory>();
        private readonly Dictionary<int, string> DicActionEventNames = new Dictionary<int, string>();
        private readonly List<Component> m_DestoryDicKey = new List<Component>();
        private static Transform m_DefaultPoolParent = null;
        private IResourceLoader m_LoadAsync = null;
        private Dictionary<int, Transform> m_ObjectPoolParentCache = new Dictionary<int, Transform>();
        private int m_PoolEpoch;
        private bool m_IsClearing;

        private void OnInit(IResourceLoader _LoadAsync, Transform _defaultPoolParent, Dictionary<int, Transform> _ObjectPoolParentCache)
        {
            if (_LoadAsync is null)
            {
                throw new ArgumentNullException(nameof(_LoadAsync));
            }

            ClearObjectPools();
            m_LoadAsync = _LoadAsync;
            m_DefaultPoolParent = _defaultPoolParent;
            m_ObjectPoolParentCache = _ObjectPoolParentCache;
            m_LoadAsync.Init();

            InitLoadData();
        }

        private void OnUpdate(float deltaTime)
        {
            for (int i = m_DestoryDicKey.Count - 1; i >= 0; i--)
            {
                Component _key = m_DestoryDicKey[i];
                AutoDestory _autoDestory = m_ConDestorData[_key];
                if (_autoDestory.UpdateLife(deltaTime))
                {
                    m_DestoryDicKey.RemoveAt(i);
                    m_ConDestorData.Remove(_autoDestory.m_target);
                    _autoDestory.m_objectPool.Release(_autoDestory.m_target);
                    i--;
                }
                else
                {
                    m_ConDestorData[_key] = _autoDestory;
                }
            }
        }

        private void OnClearAllObjPool()
        {
            ClearObjectPools();
        }

        private void ClearObjectPools()
        {
            if (m_IsClearing)
            {
                return;
            }

            m_IsClearing = true;
            unchecked
            {
                m_PoolEpoch++;
            }

            List<ObjectPool<Component>> poolsToClear = new List<ObjectPool<Component>>(m_ObjectPool.Count);
            try
            {
                foreach (KeyValuePair<string, PendingLoad> item in m_WaitLoad)
                {
                    item.Value.Cancel();
                }
                m_WaitLoad.Clear();

                foreach (KeyValuePair<string, ObjectPool<Component>> item in m_ObjectPool)
                {
                    poolsToClear.Add(item.Value);
                }
                m_ObjectPool.Clear();

                m_ConDestorData.Clear();
                m_DestoryDicKey.Clear();

                for (int i = 0; i < poolsToClear.Count; i++)
                {
                    try
                    {
                        poolsToClear[i].Clear();
                    }
                    catch (Exception exception)
                    {
                        EngineDebug.LogError($"[EngineResourcesManager] Pool clear failed: {exception}");
                    }
                }
            }
            finally
            {
                m_IsClearing = false;
            }
        }

        private void OnCreactObjToComponent<T>(string _path, Action<Component> _callback,
            int _defalutConst = 1, int _maxConst = 3, float _onDestoryTime = -1, int _poolParent = -1, bool _isNew = false) where T : Component
        {
            if (string.IsNullOrEmpty(_path))
            {
                InvokeWaiterCallback(_callback, null, _path);
                return;
            }

            if (m_IsClearing)
            {
                EngineDebug.LogWarning($"[EngineResourcesManager] Component request rejected during pool clear path={_path}");
                InvokeWaiterCallback(_callback, null, _path);
                return;
            }

            if (m_ObjectPool.TryGetValue(_path, out ObjectPool<Component> _objectPool))
            {
                int requestEpoch = m_PoolEpoch;
                Component _component = GetComponentFromPool(_objectPool, _path);
                if (_component == null)
                {
                    InvokeWaiterCallback(_callback, null, _path);
                    return;
                }

                AddAutoDestory(_component, _objectPool, _onDestoryTime);
                InvokeWaiterCallback(_callback, _component, _path);
                if (requestEpoch != m_PoolEpoch ||
                    !m_ObjectPool.TryGetValue(_path, out ObjectPool<Component> currentPool) ||
                    !ReferenceEquals(currentPool, _objectPool))
                {
                    DestroyComponentSafely(_component);
                    return;
                }

                if (_component != null)
                {
                    _component.gameObject.SetActive(true);
                }
                return;
            }

            if (m_WaitLoad.TryGetValue(_path, out PendingLoad existingPending))
            {
                existingPending.Waiters.Add(new WaitLoadObj(_callback, _onDestoryTime));
                return;
            }

            PendingLoad pending = new PendingLoad(m_PoolEpoch, _path);
            pending.Waiters.Add(new WaitLoadObj(_callback, _onDestoryTime));
            m_WaitLoad.Add(_path, pending);

            if (_isNew)
            {
                CompletePendingLoad<T>(pending, null, true, _defalutConst, _maxConst, _poolParent);
                return;
            }

            if (m_LoadAsync is null)
            {
                EngineDebug.LogError($"[EngineResourcesManager] Async resource loader is not configured path={_path}");
                CompletePendingWithNull(pending);
                return;
            }

            OnAsyncLoadObj(_path, loadedObject =>
            {
                CompletePendingLoad<T>(pending, loadedObject, false, _defalutConst, _maxConst, _poolParent);
            });
        }

        private void CompletePendingLoad<T>(PendingLoad pending, Object loadedObject, bool createWithoutPrefab,
            int defaultCapacity, int maxSize, int poolParent) where T : Component
        {
            if (!IsPendingCurrent(pending))
            {
                // IResourceLoader exposes no request handle; releasing by path could cancel a newer generation.
                return;
            }

            GameObject prefab = null;
            if (!createWithoutPrefab)
            {
                prefab = loadedObject as GameObject;
                if (prefab == null)
                {
                    EngineDebug.LogWarning($"[EngineResourcesManager] Loaded asset is not a valid GameObject path={pending.Path}");
                    CompletePendingWithNull(pending);
                    return;
                }

                if (!prefab.TryGetComponent(out T _))
                {
                    EngineDebug.LogError($"[EngineResourcesManager] Prefab is missing component type={typeof(T).Name} path={pending.Path}");
                    CompletePendingWithNull(pending);
                    return;
                }
            }

            if (!TryResolvePoolParent(poolParent, pending.Path, out Transform resolvedParent))
            {
                CompletePendingWithNull(pending);
                return;
            }

            ObjectPool<Component> pool;
            try
            {
                pool = CreateComponentPool<T>(pending.Path, prefab, resolvedParent, defaultCapacity, maxSize);
            }
            catch (Exception exception)
            {
                EngineDebug.LogError($"[EngineResourcesManager] Pool creation failed path={pending.Path}: {exception}");
                CompletePendingWithNull(pending);
                return;
            }

            if (!DetachPendingForCompletion(pending))
            {
                pool.Clear();
                return;
            }

            m_ObjectPool.Add(pending.Path, pool);
            for (int i = 0; i < pending.Waiters.Count; i++)
            {
                if (!IsPoolCurrent(pending, pool))
                {
                    break;
                }

                WaitLoadObj waiter = pending.Waiters[i];
                Component component = GetComponentFromPool(pool, pending.Path);
                if (component == null)
                {
                    InvokeWaiterCallback(waiter.callback, null, pending.Path);
                    continue;
                }

                AddAutoDestory(component, pool, waiter.destoryTime);
                InvokeWaiterCallback(waiter.callback, component, pending.Path);

                if (!IsPoolCurrent(pending, pool))
                {
                    DestroyComponentSafely(component);
                    break;
                }

                if (component != null)
                {
                    component.gameObject.SetActive(true);
                }
            }
        }

        private ObjectPool<Component> CreateComponentPool<T>(string path, GameObject prefab, Transform poolParent,
            int defaultCapacity, int maxSize) where T : Component
        {
            return new ObjectPool<Component>(
                () =>
                {
                    GameObject createdObject;
                    if (prefab == null)
                    {
                        createdObject = new GameObject(path);
                        createdObject.transform.SetParent(poolParent);
                        return createdObject.AddComponent<T>();
                    }

                    createdObject = Object.Instantiate(prefab, poolParent);
                    T component = createdObject.GetComponent<T>();
                    if (component == null)
                    {
                        EngineDebug.LogError($"[EngineResourcesManager] Instantiated object is missing component type={typeof(T).Name} path={path}");
                        DestroyGameObjectSafely(createdObject);
                        return null;
                    }
                    createdObject.SetActive(false);
                    return component;
                },
                component =>
                {
                    if (component != null)
                    {
                        component.gameObject.SetActive(false);
                    }
                },
                component =>
                {
                    if (component != null)
                    {
                        component.gameObject.SetActive(false);
                    }
                },
                DestroyComponentSafely,
                true,
                defaultCapacity,
                maxSize);
        }

        private bool TryResolvePoolParent(int poolParent, string path, out Transform resolvedParent)
        {
            if (poolParent < 0)
            {
                resolvedParent = m_DefaultPoolParent;
                return true;
            }

            if (m_ObjectPoolParentCache != null &&
                m_ObjectPoolParentCache.TryGetValue(poolParent, out resolvedParent) &&
                resolvedParent != null)
            {
                return true;
            }

            resolvedParent = null;
            EngineDebug.LogError($"[EngineResourcesManager] Pool parent was not found id={poolParent} path={path}");
            return false;
        }

        private bool IsPendingCurrent(PendingLoad pending)
        {
            return pending != null &&
                   !pending.IsCancelled &&
                   !pending.IsCompleted &&
                   pending.Epoch == m_PoolEpoch &&
                   m_WaitLoad.TryGetValue(pending.Path, out PendingLoad current) &&
                   ReferenceEquals(current, pending);
        }

        private bool DetachPendingForCompletion(PendingLoad pending)
        {
            if (!IsPendingCurrent(pending))
            {
                return false;
            }

            pending.IsCompleted = true;
            m_WaitLoad.Remove(pending.Path);
            return true;
        }

        private bool IsPoolCurrent(PendingLoad pending, ObjectPool<Component> pool)
        {
            return pending.Epoch == m_PoolEpoch &&
                   !m_IsClearing &&
                   m_ObjectPool.TryGetValue(pending.Path, out ObjectPool<Component> currentPool) &&
                   ReferenceEquals(currentPool, pool);
        }

        private void CompletePendingWithNull(PendingLoad pending)
        {
            if (!DetachPendingForCompletion(pending))
            {
                return;
            }

            for (int i = 0; i < pending.Waiters.Count; i++)
            {
                if (pending.Epoch != m_PoolEpoch || m_IsClearing)
                {
                    break;
                }
                InvokeWaiterCallback(pending.Waiters[i].callback, null, pending.Path);
            }
        }

        private static Component GetComponentFromPool(ObjectPool<Component> pool, string path)
        {
            try
            {
                return pool.Get();
            }
            catch (Exception exception)
            {
                EngineDebug.LogError($"[EngineResourcesManager] Pool get failed path={path}: {exception}");
                return null;
            }
        }

        private void AddAutoDestory(Component component, ObjectPool<Component> pool, float life)
        {
            if (life <= 0 || component == null)
            {
                return;
            }

            AutoDestory autoDestory = new AutoDestory(component, pool, life);
            m_DestoryDicKey.Add(component);
            m_ConDestorData.Add(component, autoDestory);
        }

        private static void InvokeWaiterCallback(Action<Component> callback, Component component, string path)
        {
            if (callback is null)
            {
                return;
            }

            try
            {
                callback(component);
            }
            catch (Exception exception)
            {
                EngineDebug.LogError($"[EngineResourcesManager] Component callback failed path={path}: {exception}");
            }
        }

        private static void DestroyComponentSafely(Component component)
        {
            if (component is null)
            {
                return;
            }

            if (component is ActionEngine_Unit unit)
            {
                unit.SelfDestroy(component.gameObject);
                return;
            }

            DestroyGameObjectSafely(component.gameObject);
        }

        private static void DestroyGameObjectSafely(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(gameObject);
                return;
            }
#endif
            Object.Destroy(gameObject);
        }

        // Reset delayed recycle lifetime.
        private bool OnResetLife(UnityEngine.Component _target, float _life)
        {
            if (m_ConDestorData.TryGetValue(_target, out AutoDestory _autoDestory))
            {
                _autoDestory.ReSetLife(_life);
                m_ConDestorData[_target] = _autoDestory;
                return true;
            }
            return false;
        }

        private string OnGetEventName(IActionEventData _event)
        {
            string _returnValue;
            if (!DicActionEventNames.TryGetValue(_event.GetEvenType(), out _returnValue))
            {
                _returnValue = _event.GetType().Name;
                _returnValue = _returnValue.Replace("Event", "EET");
                DicActionEventNames.Add(_event.GetEvenType(), _returnValue);
            }
            return _returnValue;
        }

        private bool OnRemoveComponent(string _path, UnityEngine.Component _target)
        {
            if (m_ObjectPool.TryGetValue(_path, out ObjectPool<Component> _objectPool))
            {
                _objectPool.Release(_target);
                return true;
            }
            return false;
        }

        private void OnAsyncLoadObj(string _key, Action<Object> _loadCallBack)
        {
            m_LoadAsync.LoadAssets(_key, _loadCallBack);
        }

        // Editor-only asset loading.
#if UNITY_EDITOR
        public void LoaderObj_Editor(string _path, Action<Object> _loadCallBack, bool ondebug = true)
        {
            m_LoadAsync.LoadAssetsEditor(_path, _loadCallBack, ondebug);
        }
#else
        public void LoaderObj_Editor(string _path, Action<Object> _loadCallBack)
        {
            _loadCallBack(null);
        }
#endif
    }
}
