using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AsiActionEngine.RunTime
{
    public partial class EngineResourcesManager : SingletonBase<EngineResourcesManager>
    {
        #region Editor调试用
#if UNITY_EDITOR
        private Transform mSkillTransform = null;
        public Transform SkillTransform
        {
            get
            {
                if (mSkillTransform is null)
                {
                    mSkillTransform = GameObject.Find("/技能实例预览")?.transform;
                }
                return mSkillTransform;
            }
        }
        public bool isPlaying { get; private set; }
        //public EngineResourcesManager()
        //{
        //    EditorFuntion();
        //}
#endif
        #endregion

        private void EditorFuntion()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                isPlaying = state == UnityEditor.PlayModeStateChange.EnteredPlayMode;
                //Debug.LogError("当前模式: " + UnityEditor.PlayModeStateChange.EnteredPlayMode);
            };
#endif
        }
        public EngineResourcesManager()
        {
            EditorFuntion();
        }

        public delegate void DLoadComponent(Type _componentType, string _path, Action<Component> _callback, int _defalutConst,
            int _maxConst, float _onDestoryTime, int objPoolParent, bool _isNew);
        public delegate bool DResetComponentLife(Component _target, float _life);
        public delegate bool DReleaseComponent(string _assetKey, Component _target);
        public delegate void DClearAllObjPool();
        public delegate void DAsyncLoadObj(string _key, Action<Object> _loadCallBack);
        public delegate void DAsyncLoadBinary(string _assetKey, Action<object> _onGetAsset);

        //public EngineResourcesManager(DLoadComponent<Transform> aa, DAsyncLoadObj asyncLoadObj = null, DAsyncLoadBinary asyncLoadBinary = null)
        //{
        //    EditorFuntion();
        //}


        private DLoadComponent LoadComponent_D = null;
        private DResetComponentLife ResetComponentLife_D = null;
        private DReleaseComponent ReleaseComponent_D = null;
        private DClearAllObjPool ClearAllObjPool_D = null;
        private DAsyncLoadObj AsyncLoadObj_D = null;
        private DAsyncLoadBinary AsyncLoadBinary_D = null;
        private bool AsyncLoadObj_Enble = false, AsyncLoadBinary_Enble = false;
        private bool LoadComponent_Enble = false, ResetComponentLife_Enble = false, ReleaseComponent_Enble = false, ClearAllObjPool_Enble = false;
        public EngineResourcesManager(DAsyncLoadObj asyncLoadObj, DAsyncLoadBinary asyncLoadBinary = null)
        {
            EditorFuntion();
            SetAsyncLoadFuntion(asyncLoadObj, asyncLoadBinary);
        }

        public void SetAsyncLoadFuntion(
            DAsyncLoadObj asyncLoadObj,
            DAsyncLoadBinary asyncLoadBinary = null,
            DLoadComponent loadComponent = null,
            DResetComponentLife resetComponentLife = null,
            DReleaseComponent releaseComponent = null,
            DClearAllObjPool clearAllObjPool = null)
        {
            if (asyncLoadObj is not null)
            {
                AsyncLoadObj_Enble = true;
                AsyncLoadObj_D = asyncLoadObj;
            }
            if (asyncLoadBinary is not null)
            {
                AsyncLoadBinary_Enble = true;
                AsyncLoadBinary_D = asyncLoadBinary;
            }
            if (loadComponent is not null)
            {
                LoadComponent_Enble = true;
                LoadComponent_D = loadComponent;
            }
            if (resetComponentLife is not null)
            {
                ResetComponentLife_Enble = true;
                ResetComponentLife_D = resetComponentLife;
            }
            if (releaseComponent is not null)
            {
                ReleaseComponent_Enble = true;
                ReleaseComponent_D = releaseComponent;
            }
            if (clearAllObjPool is not null)
            {
                ClearAllObjPool_Enble = true;
                ClearAllObjPool_D = clearAllObjPool;
            }
        }

        public ActionStatePart GetPreActionStatePart() => OnGetPreActionStatePart();

        public string GetEventName(IActionEventData _event) => OnGetEventName(_event);

        public ActionMachineTime MachineTime = new ActionMachineTime(0, 0, 0, 0);
        public Vector3[] Arr_Vector3 = new Vector3[512];
        /// <summary>
        /// 资源管理初始化
        /// </summary>
        /// <param name="_LoadAsync">加载方法 (必须异步)</param>
        /// <param name="_ObjectPoolParent"></param>
        public void Init(IResourceLoader _LoadAsync, Transform _defaultPoolParent, Dictionary<int, Transform> _ObjectPoolParent) =>
            OnInit(_LoadAsync, _defaultPoolParent, _ObjectPoolParent);

        /// <summary>
        /// 需要每帧更新逻辑，用来定时释放资源等
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update(float _deltaTime) => OnUpdate(_deltaTime);

        /// <summary>
        /// 生成Component对象，自动创建对象池管理，并定时回收
        /// </summary>
        /// <param name="_path">路径或者Key</param>
        /// <param name="_callback">加载完成的回调</param>
        /// <param name="_defalutConst">对象池默认生成数量</param>
        /// <param name="_maxConst">对象池最大生成数量</param>
        /// <param name="_onDestoryTime">自动销毁时间(由对象池回收)</param>
        public void CreactObjToComponent<T>(string _path, Action<Component> _callback, int _defalutConst = 3,
            int _maxConst = 10, float _onDestoryTime = -1, int objPoolParent = -1, bool _isNew = false) where T : Component
        {
            if (LoadComponent_Enble)
            {
                LoadComponent_D(typeof(T), _path, _callback, _defalutConst, _maxConst, _onDestoryTime, objPoolParent, _isNew);
                return;
            }
            OnCreactObjToComponent<T>(_path, _callback, _defalutConst, _maxConst, _onDestoryTime, objPoolParent, _isNew);
        }

        /// <summary>
        /// 重置资产寿命
        /// </summary>
        /// <param name="_target">重置寿命的目标</param>
        /// <param name="_life">重置的寿命</param>
        /// <returns>是否已经被销毁</returns>
        public bool ResetLife(UnityEngine.Component _target, float _life)
        {
            if (ResetComponentLife_Enble)
            {
                return ResetComponentLife_D(_target, _life);
            }
            return OnResetLife(_target, _life);
        }

        /// <summary>
        /// 删除对象（从对象池内释放）
        /// </summary>
        /// <param name="_assetKey">对象加载路径或者Key，用来查找对应的对象池</param>
        /// <param name="_target">要删除的目标对象</param>
        /// <returns>如果找不到对象池就返回false</returns>
        public bool RemoveComponent(string _assetKey, Component _target)
        {
            if (ReleaseComponent_Enble)
            {
                return ReleaseComponent_D(_assetKey, _target);
            }
            return OnRemoveComponent(_assetKey, _target);
        }

        /// <summary>
        /// 异步加载Object
        /// </summary>
        /// <param name="_key">资源路径或者key</param>
        /// <param name="_loadCallBack">加载结束后返回</param>
        public void AsyncLoadObj(string _key, Action<Object> _loadCallBack)
        {
            if (AsyncLoadObj_Enble)
            {
                AsyncLoadObj_D(_key, _loadCallBack);
                return;
            }
            OnAsyncLoadObj(_key, _loadCallBack);
        }

        /// <summary>
        /// 异步加载二进制数据
        /// </summary>
        /// <param name="_assetKey">路径或者Key</param>
        /// <param name="_onGetAsset">加载的二进制数据</param>
        public void AsyncLoadBinary(string _assetKey, Action<object> _onGetAsset)
        {
            if (AsyncLoadBinary_Enble)
            {
                AsyncLoadBinary_D(_assetKey, _onGetAsset);
                return;
            }
            OnAsyncLoadBinary(_assetKey, _onGetAsset);
        }


        public string GetRuntimePath(string _assetKey) => m_LoadAsync.GetRunTimePath(_assetKey);

        public void ClearAllObjPool()
        {
            if (ClearAllObjPool_Enble)
            {
                ClearAllObjPool_D();
                return;
            }
            OnClearAllObjPool();
        }
        public void ClearLoadBinary(string _assetKey) => m_LoadAsync.ClearLoadBinary(_assetKey);
        public void ClearLoadAssets(string _assetKey) => m_LoadAsync.ClearLoadAssets(_assetKey);
        public void ClearLoadAssetsEditor(string _assetKey) => m_LoadAsync.ClearLoadAssetsEditor(_assetKey);
        public ActionEngine_Unit Player => m_LoadAsync.Player();
        public void DestoryUnit(ActionEngine_Unit _Unit) => m_LoadAsync.DestoryUnit(_Unit);
        public void LoadUnit(int assetKey, Action<TargetUnit> _callback, EUnitType _type)
            => m_LoadAsync.LoadUnit(assetKey, _callback, _type);
        public void LoadSkill(int skillID, Action<ActionEngine_Skill> _callback)
            => m_LoadAsync.LoadSkill(skillID, _callback);
        public Dictionary<ushort, EngineGValue> GetEngineGValue() => m_LoadAsync.GetEngineGValue();

        public string DataName_Unit => m_LoadAsync.DataName_Unit();
        public string DataName_Action => m_LoadAsync.DataName_Action();
        public string DataName_Skill => m_LoadAsync.DataName_Skill();
        public string DataName_Cam => m_LoadAsync.DataName_Cam();
        public string DataName_Module => m_LoadAsync.DataName_Module();
        public string DataName_Item => m_LoadAsync.DataName_Item();
        public string DataName_Gvalue => m_LoadAsync.DataName_Gvalue();
        public string DataName_Equation => m_LoadAsync.DataName_Equation();
    }
}