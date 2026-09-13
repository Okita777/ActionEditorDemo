using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        private Dictionary<(int, int), Component> mInstanceObjects = new Dictionary<(int, int), Component>();
        //private readonly int mActionPoolMaxSize = 20;//可继承的Action最大数量
        //private readonly int mEventPooklMaxSize = 5;//同一类型中可继承的最大事件数量

        private Dictionary<int, EventPool> mEventDic = new Dictionary<int, EventPool>();//Event对象池，不管理回收

        private ActionEvent[] mActionPool;//Action对象池
        private HashSet<byte> mActionPoolHashList_has;
        private HashSet<byte> mActionPoolHashList;
        private List<byte> mActionPoolList;
        private int mActionPoolID = 0;

        public bool InstanceComponent_TryGet(int id, int eventID, out Component _obj)
        {
            return mInstanceObjects.TryGetValue((id, eventID), out _obj);
        }
        public void InstanceComponent_Add(int id, int eventID, Component _obj)
        {
            mInstanceObjects.TryAdd((id, eventID), _obj);
        }
        public void InstanceComponent_Remove(int id, int eventID)
        {
            mInstanceObjects.Remove((id, eventID));
        }

        private void Init_EventPool()
        {
            mActionPool = new ActionEvent[MotionEngineConst.ActionPoolMaxSize];
            mActionPoolHashList_has = new HashSet<byte>(mActionPool.Length);
            mActionPoolHashList = new HashSet<byte>(mActionPool.Length);
            mActionPoolList = new List<byte>(mActionPool.Length);
            for (int i = 0; i < mActionPool.Length; i++)
            {
                ActionEvent _newEvent = new ActionEvent();
                _newEvent.IsTem = true;
                byte _id = (byte)i;
                _newEvent.TemID = _id;
                mActionPoolHashList.Add(_id);
                mActionPoolList.Add(_id);
                mActionPool[i] = _newEvent;
            }//新建action对象池
        }

        public ActionEvent CreactAction(ActionEvent _action, float _time, string _actionName)
        {
            if (mActionPoolList.Count < 1)
            {
#if UNITY_EDITOR
                string _parentName = (CurUnit.transform.parent is not null ? $"  [{CurUnit.transform.parent.name}]" : "");
                string _debugError = $"复制的Action超出上限,回收无效临时Action [<color=#ffcc00>{CurUnit.gameObject.name}</color>] [{CurUnit.transform.GetSiblingIndex()}]" + _parentName;
                foreach (ActionEvent item in mActionPool)
                {
                    _debugError += $"\nEventType[{item.EventData.GetType().Name}] [{item.TemID}]  EdiHer[{item.EditorInheritable}]  Her[{item.Inheritable}]";
                }
                EngineDebug.LogWarning(_debugError);
#endif

                //回收ActionEvent
                mActionPoolHashList_has.Clear();
                foreach (ActionStatePart _part in AllActionStatePart)
                {
                    foreach (ActionEvent _event in _part.CurrentActionEvents)
                    {
                        mActionPoolHashList_has.Add(_event.TemID);
                    }
                }

                for (int i = 0; i < mActionPool.Length; i++)
                {
                    byte _id = (byte)i;
                    if (!mActionPoolHashList_has.Contains(_id))
                    {
                        mActionPoolList.Add(_id);
                        mActionPoolHashList.Add(_id);
                    }
                }

                if (mActionPoolList.Count < 1)
                {
#if UNITY_EDITOR
                    EngineDebug.LogError("预留的Action长度不足，已超出承受范围");
#endif
                    mActionPoolList.Add(0);
                    mActionPoolHashList.Add(0);
                }
                //return mActionPool[0];
            }
            byte _getID = mActionPoolList[^1];
            mActionPoolList.RemoveAt(mActionPoolList.Count - 1);
            mActionPoolHashList.Remove(_getID);

            ActionEvent _returnActionEvent = mActionPool[_getID];
            _returnActionEvent = mActionPool[_getID].CloneTo(_action);
            _returnActionEvent.EventData = _action.EventData.Clone(CreactEventDataTo(_action.EventData));

            //if (EngineResourcesManager.Instance.Player == CurUnit)
            //{
            //    if (_action.EventData.GetEvenType() == 20)
            //    {
            //        EngineDebug.LogError($"生成Action实例[{_action.EventData.GetEvenType()}]" +
            //        $"\nD[{_action.Duration}]");
            //    }
            //}

            //_returnActionEvent.EditorInheritable = true;
            //_returnActionEvent.Inheritable = _action.Inheritable;
            //if (_action.EventData.GetEvenType() == -(int)EEvenTypeInternal.EET_SkillEntity)
            //{
            //    EngineDebug.Log("生成技能实例: " + );
            //}

            ////循环利用
            //if (mActionPoolID < MotionEngineConst.ActionPoolMaxSize - 1)
            //{
            //    mActionPoolID++;
            //}
            //else
            //{
            //    mActionPoolID = 0;
            //}
            //if (_returnActionEvent.EventData.GetEvenType() == 20)
            //{
            //    Debug.LogWarning($"创建了 [<color=#ffcc00>{_returnActionEvent.EventData.GetType().Name}</color>]  [{_returnActionEvent.TemID}]" +
            //        $"\nAction[{_actionName}]  Time[{_time}] EdiHer[{_returnActionEvent.EditorInheritable}]  Her[{_returnActionEvent.Inheritable}]");
            //}
            return _returnActionEvent;
        }
        public void DestoryAction(ActionEvent _action)
        {
            if (_action.IsTem)
            {
                //if (_action.EventData.GetEvenType() == 20)
                //{
                //    Debug.LogWarning($"释放了 [<color=#ff0000>{_action.EventData.GetType().Name}</color>] [{_action.TemID}]");
                //}

                if (!mActionPoolHashList.Contains(_action.TemID))
                {
                    mActionPoolHashList.Add(_action.TemID);
                    mActionPoolList.Add(_action.TemID);
                }
                else
                {
                    EngineDebug.LogWarning($"尝试释放已经释放过的Action实例 [{_action.EventData.GetType().Name}] [{_action.EventData.GetEvenType()}]");
                }
            }
            //else
            //{
            //    if (_action.EventData.GetEvenType() == 20)
            //    {
            //        Debug.LogError($"释放错误！！！！！ [<color=#ff0000>{_action.EventData.GetType().Name}</color>]");
            //    }
            //}
        }

        public IActionEventData CreactEventDataTo(IActionEventData _actionEventData)
        {
            EventPool _eventPool = null;
            if (!mEventDic.TryGetValue(_actionEventData.GetEvenType(), out _eventPool))
            {
                _eventPool = new EventPool(MotionEngineConst.EventPooklMaxSize, _actionEventData);
                mEventDic.Add(_actionEventData.GetEvenType(), _eventPool);
            }
            return _eventPool.GetEvent(_actionEventData);
        }
    }

    public class EventPool
    {
        private int mPoolID;
        private int mMaxSize;
        private IActionEventData[] mActionEvent;

        public EventPool(int _maxSize, IActionEventData _actionEventData)
        {
            mPoolID = 0;
            mMaxSize = _maxSize;
            mActionEvent = new IActionEventData[_maxSize];
            for (int i = 0; i < mActionEvent.Length; i++)
            {
                mActionEvent[i] = _actionEventData.Clone(_actionEventData.Creact());
            }
        }

        public IActionEventData GetEvent(IActionEventData _actionEventData)
        {
            IActionEventData _retrunValue = null;

            //mActionEvent[mPoolID] = _actionEventData.Clone(mActionEvent[mPoolID]);
            _retrunValue = mActionEvent[mPoolID];


            mPoolID++;
            if (mPoolID < mMaxSize - 1)
            {
                mPoolID++;
            }
            else
            {
                mPoolID = 0;
            }

            return _retrunValue;
        }
    }
}