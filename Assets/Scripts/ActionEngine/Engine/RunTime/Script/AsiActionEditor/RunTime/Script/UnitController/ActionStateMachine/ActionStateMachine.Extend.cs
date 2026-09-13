using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        #region Delegate
        private delegate bool actionCheck();
        private delegate float actionRetrunF();
        private delegate float[] actionRetrunAF();
        #endregion

        private Dictionary<string, Component> MonoComponent = new Dictionary<string, Component>();
        private Dictionary<string, ActionLogics> ActionLogics = new Dictionary<string, ActionLogics>();
        private Dictionary<string, StaticActionLogics> StaticActionLogics = new Dictionary<string, StaticActionLogics>();

        private HashSet<string> VisitedComponent = new HashSet<string>();
        private List<int> ActionLable = new List<int>();

        public List<int> GetActionLableList => ActionLable;

        private void Update_Extend(float _deltaTime)
        {
            foreach (var _actionLogic in ActionLogics)
            {
                _actionLogic.Value.Update(this, _deltaTime);
            }//每帧都跑的逻辑器
            foreach (var _actionLogic in StaticActionLogics)
            {
                _actionLogic.Value.IsEnble = false;
            }//关闭静态的逻辑器
        }

        private void LateUpdate_Extend(float _deltaTime)
        {
            foreach (var _actionLogic in ActionLogics)
            {
                _actionLogic.Value.LateUpdate(this, _deltaTime);
            }//每帧都跑的逻辑器
        }

        /// <summary>
        /// 消耗极低，如果使用RD看到高消耗警告，只是因为此函数有Debug
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetComponent<T>(out T _value, string _key, Transform _target = null) where T : Component
        {
            string _typeName = _key;
            //if (string.IsNullOrEmpty(_key))
            //{
            //    _typeName = typeof(T).Name;
            //    //_typeName = nameof(T);
            //    //EngineDebug.LogWarning($"泛型类型名称 [{_typeName}]");
            //}

            if (MonoComponent.ContainsKey(_typeName))
            {
                _value = MonoComponent[_typeName] as T;
                return true;
            } //如果字典已经有了这个类型，就直接OUT
            else
            {
                if (!VisitedComponent.Contains(_typeName))
                {
                    VisitedComponent.Add(_typeName);
                    Transform _targetN = (_target == null) ? CurUnit.transform : _target;
                    if (_targetN.TryGetComponent(out T _componet))
                    {
                        MonoComponent.Add(_typeName, _componet);
                        _value = _componet;
                        return true;
                    }
                    EngineDebug.LogWarning($"[<color=#FFCC00>{_typeName}</color>] 组件获取失败!! [<color=#ffcc00>{CurUnit.gameObject.name}</color>]");
                } //检查是否尝试获取过，如果尝试过就不再进行获取。【主要为了避免多次调用 TryGetComponent 】
            } //如果字典没有这个类型，就尝试添加

            _value = null;
            return false;
        }

        public bool TryGetLogic<T>(out T _value, string _key) where T : ActionLogics, new()
        {
            string _typeName = _key;
            if (string.IsNullOrEmpty(_key))
            {
                _typeName = typeof(T).Name;
            }

            if (ActionLogics.ContainsKey(_typeName))
            {
                _value = ActionLogics[_typeName] as T;
                return true;
            }
            else
            {
                _value = new T();
                _value.StateMachine = this;
                ActionLogics.Add(_typeName, _value);
                _value.Start(this);
                return true;
            }

            _value = null;
            return false;
        }

        public bool TryGetStaticLogic<T>(out T _value, string _key) where T : StaticActionLogics, new()
        {
            string _typeName = _key;
            if (string.IsNullOrEmpty(_key))
            {
                _typeName = typeof(T).Name;
            }

            if (StaticActionLogics.ContainsKey(_typeName))
            {
                _value = StaticActionLogics[_typeName] as T;
                _value.OnGet(this);
                if (!_value.IsEnble)
                {
                    _value.OnUpdate(this);
                    _value.IsEnble = true;
                }
                return true;
            }
            else
            {
                _value = new T();
                _value.StateMachine = this;
                StaticActionLogics.Add(_typeName, _value);
                _value.OnStart(this);
                _value.OnGet(this);
                _value.OnUpdate(this);
                _value.IsEnble = true;
                return true;
            }

            _value = null;
            return false;
        }
        public void AddActionLable(int _lable)
        {
            if (!ActionLable.Contains(_lable))
            {
                ActionLable.Add(_lable);
            }
        }
        public void RemoveActionLable(int _lable)
        {
            if (ActionLable.Contains(_lable))
            {
                ActionLable.Remove(_lable);
            }
        }
        public void RemoveAllActionLable()
        {
            ActionLable.Clear();
        }
        public bool CheckActionLable(int _lable)
        {
            return ActionLable.Contains(_lable);
        }
        public string GetActionLable(int _lable)
        {
            return mActionStateInfo.mActionLable[_lable];
        }

        public void CleartComponent_Pool()
        {
            MonoComponent.Clear();
            VisitedComponent.Clear();
        }
    }

    public class ActionLogics
    {
        public ActionStateMachine StateMachine = null;
        public virtual void Start(ActionStateMachine _actionState) { }

        public virtual void Update(ActionStateMachine _actionState, float _deltaTime) { }

        public virtual void LateUpdate(ActionStateMachine _actionState, float _deltaTime) { }
    }

    public abstract class StaticActionLogics
    {
        public bool IsEnble = false;
        public ActionStateMachine StateMachine = null;

        /// <summary>
        /// 仅在生成时执行一次
        /// </summary>
        /// <param name="_actionState"></param>
        public virtual void OnStart(ActionStateMachine _actionState) { }
        /// <summary>
        /// 在每次【TryGetStaticLogic】时执行, 生成时【OnStart】先于【OnGet】执行
        /// </summary>
        /// <param name="_actionState"></param>
        public virtual void OnGet(ActionStateMachine _actionState) { }
        /// <summary>
        /// 仅在【TryGetStaticLogic】时执行， 且每帧只会执行一次，直到下一帧才允许重新执行
        /// </summary>
        /// <param name="_actionState"></param>
        public virtual void OnUpdate(ActionStateMachine _actionState) { }
    }
}