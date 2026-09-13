using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace AsiTimeLine.RunTime
{
    public class UnitClothPool
    {
        public enum ClothType
        {
            [Description("头饰")] 头饰, //Hat
            [Description("头发")] 头发, //Hair
            [Description("头")] 头, //Head
            [Description("衣服")] 衣服, //Clothes
            [Description("裤子")] 裤子, //Trousers
            [Description("手套")] 手套, //Glove
            [Description("鞋子")] 鞋子, //Boots
            [Description("脚")] 脚, //Foot
            [Description("护腕")] 护腕, //Bracers
            [Description("耳朵")] 耳朵, //ear
        }

        #region 单例和构造体
        private static UnitClothPool _Instance;

        public static UnitClothPool Instance
        {
            get
            {
                if (_Instance is null)
                {
                    _Instance = new UnitClothPool();
                }
                return _Instance;
            }
        }

        [System.Serializable]
        public struct CheckClothInfo
        {
            public GameObject mMainCloth;
            public ClothInfo mClothJoint;

            public CheckClothInfo(GameObject _mMainCloth, ClothInfo _mClothJoint)
            {
                mMainCloth = _mMainCloth;
                mClothJoint = _mClothJoint;
            }
        }
        #endregion

        private Dictionary<string, string> mCloth = new Dictionary<string, string>();
        private Dictionary<string, ClothInfo> mClothInfo = new Dictionary<string, ClothInfo>();
        private Dictionary<int, CheckClothInfo> mCheckClothInfos = new Dictionary<int, CheckClothInfo>();

        public void AddClothPath(string _name, string _path)
        {
            if (!mCloth.TryAdd(_name, _path))
            {
                // EngineDebug.LogError($"服装加载路径追加时出现错误: [{_name}]已被加载过");
            }
        }
        public void EquipCloth(string _name, ActionEngine_Unit _unit, int _clothType, Action<GameObject> _loadCallback)
        {
            CheckEquipCloth(_clothType);

            if (mClothInfo.TryGetValue(_name, out ClothInfo _clothInfo))
            {
                GameObject _cloth = _clothInfo.mMesh.Get();

                CheckClothInfo _checkClothInfo = new CheckClothInfo(_cloth, _clothInfo);
                CheckEquipCloth(_clothType, _checkClothInfo);

                OnEquip(_cloth, _unit, _clothInfo);
                _loadCallback(_cloth);
            }
            else
            {
                if (mCloth.TryGetValue(_name, out string _path))
                {
                    EngineResourcesManager.Instance.AsyncLoadObj(_path, (o =>
                    {
                        GameObject _gameObject = (GameObject)o;
                        ObjectPool<GameObject> _objectPool = new ObjectPool<GameObject>
                        (
                            () =>
                            {
                                return Object.Instantiate(_gameObject);
                            },
                            (GameObject _target) =>
                            {
                                _target.SetActive(true);
                            },
                            (GameObject _target) =>
                            {
                                _target.SetActive(false);
                            },
                            (GameObject _target) =>
                            {
                                Object.Destroy(_target);
                            },
                            true,
                            1,
                            100
                        );
                        GameObject _nowCloth = _objectPool.Get();

                        ClothInfo _clothInfo = CreactClothInfo(_nowCloth, _unit, _objectPool);

                        CheckClothInfo _checkClothInfo = new CheckClothInfo(_nowCloth, _clothInfo);
                        CheckEquipCloth(_clothType, _checkClothInfo);

                        mClothInfo.Add(_name, _clothInfo);
                        OnEquip(_nowCloth, _unit, _clothInfo);
                        _loadCallback(_nowCloth);
                    }));
                }
                else
                {
                    EngineDebug.LogWarning($"不存在此服装: {_name}");
                }
            }
        }

        public void Undress(int _clothType)
        {
            CheckEquipCloth(_clothType);
        }

        private bool OnEquip(GameObject _gameObject, ActionEngine_Unit _unit, ClothInfo _clothInfo)
        {
            Transform _transform = _gameObject.transform;
            int _length = _transform.childCount - 1;
            for (int i = _length; i >= 0; i--)
            {
                if (_transform.GetChild(i).TryGetComponent(out SkinnedMeshRenderer _meshRenderer))
                {
                    if (_unit.ActionStateMachine.TryGetComponent(out EquipCostume_Target _target, nameof(EquipCostume_Target)))
                    {
                        if (_target.ClothBones.TryGetValue(_clothInfo.mSkinRootName, out Transform _root))
                        {
                            _meshRenderer.rootBone = _root;
                        }
                        else
                        {
                            EngineDebug.LogError($"服装装配错误！！！！ 没有在角色骨骼上找到【{_clothInfo.mSkinRootName}】");
                            return false;
                        }

                        // EngineDebug.Log($"未存在的骨骼: {_clothInfo.mSkinBones.Length}");
                        _clothInfo.mSkinSetParent.Clear();
                        List<Transform> _allSkinBone = new List<Transform>();
                        List<Transform> syncParents = new List<Transform>();
                        for (int j = 0; j < _clothInfo.mSkinBones.Length; j++)
                        {
                            string _boneName = _clothInfo.mSkinBones[j];

                            if (_target.ClothBones.TryGetValue(_boneName, out _transform))
                            {
                                _allSkinBone.Add(_transform);
                            }
                            else
                            {
                                _allSkinBone.Add(_meshRenderer.bones[j]);
                            }

                            Transform _meshBoneParent = _meshRenderer.bones[j].parent;
                            if (!_meshRenderer.bones.Contains(_meshBoneParent))
                            {
                                if (!syncParents.Contains(_meshBoneParent))
                                {
                                    syncParents.Add(_meshBoneParent);
                                }
                            }

                        }//转移权重

                        foreach (var syncParent in syncParents)
                        {
                            Transform _syncParent = syncParent.parent;
                            if (_target.ClothBones.TryGetValue(syncParent.name, out Transform _targetTrans))
                            {
                                _clothInfo.mSkinSetParent.Add(syncParent, _syncParent);
                                syncParent.SetParent(_targetTrans);
                                syncParent.position = _targetTrans.position;
                                syncParent.rotation = _targetTrans.rotation;
                            }
                        }

                        _meshRenderer.bones = _allSkinBone.ToArray();
                        return true;
                    }
                }
            }

            EngineDebug.LogError("服装装配错误！！！！ 没有找到【SkinnedMeshRenderer】");
            return false;
        }

        private ClothInfo CreactClothInfo(GameObject _gameObject, ActionEngine_Unit _unit, ObjectPool<GameObject> _objectPool)
        {
            Transform _transform = _gameObject.transform;
            int _length = _transform.childCount - 1;
            for (int i = _length; i >= 0; i--)
            {
                if (_transform.GetChild(i).TryGetComponent(out SkinnedMeshRenderer _meshRenderer))
                {
                    if (_unit.ActionStateMachine.TryGetComponent(out EquipCostume_Target _target, nameof(EquipCostume_Target)))
                    {
                        //收集所有参与蒙皮的骨骼名
                        List<string> _skinBoneNames = new List<string>();
                        foreach (var VARIABLE in _meshRenderer.bones)
                        {
                            _skinBoneNames.Add(VARIABLE.name);
                        }

                        //收集角色不具备的骨骼的统一父级
                        List<string> clothBoneParent = new List<string>();
                        foreach (var VARIABLE in _meshRenderer.bones)
                        {
                            if (!_target.ClothBones.ContainsKey(VARIABLE.name))
                            {
                                string _newBoneParent = VARIABLE.parent.name;
                                if (!clothBoneParent.Contains(_newBoneParent))
                                {
                                    if (_target.ClothBones.ContainsKey(_newBoneParent))
                                    {
                                        clothBoneParent.Add(_newBoneParent);
                                    }
                                }
                            }
                        }
                        return new ClothInfo(_objectPool, _skinBoneNames.ToArray(),
                            clothBoneParent.ToArray(), _meshRenderer.rootBone.name);
                    }
                }
            }
            EngineDebug.LogError("服装装配错误！！！！ 请仔细检查资源");
            return null;
        }

        private void CheckEquipCloth(int _clothType, CheckClothInfo _checkClothInfo)
        {
            if (!mCheckClothInfos.ContainsKey(_clothType))
            {
                mCheckClothInfos.Add(_clothType, _checkClothInfo);
            }
            else
            {
                EngineDebug.LogError("角色着装的时候，发现有未脱下的服装");
            }
        }

        //卸载同类型的装备
        private void CheckEquipCloth(int _clothType)
        {
            if (mCheckClothInfos.ContainsKey(_clothType))
            {
                CheckClothInfo _clothInfo = mCheckClothInfos[_clothType];
                foreach (var VARIABLE in _clothInfo.mClothJoint.mSkinSetParent)
                {
                    VARIABLE.Key.SetParent(VARIABLE.Value);
                }
                _clothInfo.mClothJoint.mMesh.Release(_clothInfo.mMainCloth);
                mCheckClothInfos.Remove(_clothType);
            }
        }
    }
    public class ClothInfo
    {
        public ObjectPool<GameObject> mMesh;
        public string[] mSkinBones;
        public string[] mSkinBones_New;
        public string mSkinRootName;
        public Dictionary<Transform, Transform> mSkinSetParent = new Dictionary<Transform, Transform>();

        public ClothInfo(ObjectPool<GameObject> _mMesh, string[] _mSkinBones, string[] _mSkinBones_New, string _mSkinRootName)
        {
            mMesh = _mMesh;
            mSkinBones = _mSkinBones;
            mSkinBones_New = _mSkinBones_New;
            mSkinRootName = _mSkinRootName;
        }
    }
}