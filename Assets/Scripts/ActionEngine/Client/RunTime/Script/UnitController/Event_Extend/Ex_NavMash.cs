using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.AI;

namespace AsiTimeLine.RunTime
{
    public class Ex_NavMash : StaticActionLogics
    {
        private NavMeshPath mNavMeshPath = new NavMeshPath();
        private NavMeshHit NavMeshHit = new NavMeshHit();

        //获取路径点
        public bool SetPath(GGroupPoint _gGroupPoint, PointData _startPos, PointData _targetPos, int areaMask = NavMesh.AllAreas)
        {
            List<PointData> _pointData = _gGroupPoint.GetValue(StateMachine.FirstStatePart);
            NavigationQueryApiRuntime.CalculatePath(
                _startPos.pos,
                _targetPos.pos,
                areaMask,
                mNavMeshPath);
            _pointData.Clear();
            Vector3[] pathPoints = EngineResourcesManager.Instance.Arr_Vector3;
            int pathGroupConst = mNavMeshPath.GetCornersNonAlloc(pathPoints);

            //从末端开始加路径点
            _pointData.Add(_targetPos);
            Vector3 _lastPos = _targetPos.pos;
            for (int i = pathGroupConst - 2; i > 0; i--)
            {
                Quaternion _rot = Quaternion.LookRotation(_lastPos - pathPoints[i]);
                _lastPos = pathPoints[i];
                _pointData.Add(new PointData(pathPoints[i], _rot));
            }
            //_pointData.Add(_startPos);
            return true;

            EngineDebug.LogError($"没有设定过初始的Nav对象[<color=#ffcc00>{StateMachine.CurUnit.gameObject.name}</color>]");
            return false;
        }

        public bool CheckPointToNavMash(Vector3 _targetPos, float _maxDistance, int areaMask = NavMesh.AllAreas)
        {
            //return true;

            if (NavigationQueryApiRuntime.SamplePosition(
                    _targetPos,
                    out NavMeshHit,
                    _maxDistance,
                    areaMask))
            {
                //Debug.LogWarning("检查到在范围内");
                return true;
            }
            return false;
        }
        public bool CheckPointToNavMash(Vector3 _targetPos, float _maxDistance, out NavMeshHit _navMeshHit, int areaMask = NavMesh.AllAreas)
        {
            if (NavigationQueryApiRuntime.SamplePosition(
                    _targetPos,
                    out _navMeshHit,
                    _maxDistance,
                    areaMask))
            {
                //Debug.LogWarning("检查到在范围内");
                return true;
            }

            //if (NavMesh.GetAreaNames().Length > 0)
            //{
            //    string names = $"找到了<color=ffcc00>{NavMesh.GetAreaNames().Length}</color>个导航网格 MaxDistance[{_maxDistance}]";
            //    for (int i = 0; i < NavMesh.GetAreaNames().Length; i++)
            //    {
            //        string item = NavMesh.GetAreaNames()[i];
            //        names += $"\n导航网格名称: {item} Cost[{NavMesh.GetAreaCost(i)}]";
            //    }
            //    //foreach (var item in NavMesh.GetAreaNames())
            //    //{
            //    //    names += $"\n导航网格名称: {item}";
            //    //}
            //    //NavMesh.
            //    EngineDebug.LogError(names);
            //}
            //else
            //{
            //    EngineDebug.LogError($"未找到任何导航网格");
            //}

            //EngineDebug.LogError($"<color=#ff0000>没有找到寻路网格</color>[{_targetPos}]");
            //EngineDebug.DrawSphere(_targetPos, 2.2f, Color.black, 10);
            return false;
        }
    }
}
