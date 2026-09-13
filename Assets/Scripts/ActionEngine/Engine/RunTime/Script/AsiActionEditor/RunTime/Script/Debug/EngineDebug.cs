using System;
using System.Collections.Generic;
using AsiActionEngine.RunTime.Graph;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public partial class EngineDebug
    {
        private const string color1 = "#ffcc00";
        private const string color2 = "#ff0000";
        private const string color3 = "#00FFFF";
        private const string SceneDrawEditorDebugPreferenceKey = "ScenceDraw_EditorDB";

        //private static List<string> scenceLog = new List<string>(16);
#if UNITY_EDITOR
        private static Dictionary<int, List<string>> scenceLogDic = new Dictionary<int, List<string>>(3);
#endif

        public delegate void Delegate_GetGEnumNames(ActionStatePart _Part, GEnum _GEnum, out string _groupName, out string _gvName, out string _enumName);
        public static Delegate_GetGEnumNames Delegate_GetGENameCallBack;

        public static Action<string, LogType> Delegate_LogOutput;
        public static bool GetGEnumNames(ActionStatePart _Part, GEnum _GEnum, out string _groupName, out string _gvName, out string _enumName)
        {
            if (Delegate_GetGENameCallBack is null)
            {
                _groupName = _gvName = _enumName = string.Empty;
                return false;
            }

            Delegate_GetGENameCallBack.Invoke(_Part, _GEnum, out _groupName, out _gvName, out _enumName);
            return true;
        }
        public static string GetEventPath()
        {
            ActionStateInfo actionStateInfo = EngineResourcesManager.Instance.Active_ActionStateInfo;
            ActionState actionState = EngineResourcesManager.Instance.Active_ActionState;
            ActionEvent actionEvent = EngineResourcesManager.Instance.Active_ActionEvent;

            string _str = "";
            if (actionStateInfo is not null)
                _str += $"ActionStateInfo:[<color={color3}>{actionStateInfo.ActionGroupName}</color>(<color={color1}>{actionStateInfo.ActionGroupID}</color>)]";
            if (actionState is not null)
                _str += $".Action[<color={color3}>{actionState.Name}</color>(<color={color1}>{actionState.ID}</color>)]";
            if (actionEvent is not null)
                _str += $".Event[<color={color1}>{actionEvent.EventData.GetType().Name}</color>]";
            return _str;
        }

        public static void Log(string _string)
        {
#if UNITY_EDITOR
            AddScenceLog(0, _string);
            if (Delegate_LogOutput != null)
                Delegate_LogOutput(_string, LogType.Log);
            else if (PlayerPrefs.GetInt(SceneDrawEditorDebugPreferenceKey, 0) > 0)
                Debug.Log(_string);
#else
            Delegate_LogOutput?.Invoke(_string, LogType.Log);
#endif
        }
        public static void LogWarning(string _string)
        {
#if UNITY_EDITOR
            AddScenceLog(1, _string);
            if (Delegate_LogOutput != null)
                Delegate_LogOutput(_string, LogType.Warning);
            else if (PlayerPrefs.GetInt(SceneDrawEditorDebugPreferenceKey, 0) > 0)
                Debug.LogWarning(_string);
#else
            Delegate_LogOutput?.Invoke(_string, LogType.Warning);
#endif
        }
        public static void LogError(string _string)
        {
#if UNITY_EDITOR
            AddScenceLog(2, _string);
            if (Delegate_LogOutput != null)
                Delegate_LogOutput(_string, LogType.Error);
            else
                Debug.LogError(_string);
#else
            Delegate_LogOutput?.Invoke(_string, LogType.Error);
#endif
        }

        public static bool IsBlueprintDebugOutputEnabled()
        {
#if UNITY_EDITOR
            return Delegate_LogOutput != null
                || PlayerPrefs.GetInt(SceneDrawEditorDebugPreferenceKey, 0) > 0;
#else
            return Delegate_LogOutput != null;
#endif
        }

        //public static void LogToScence(string _str)
        //{
        //    scenceLog.Add(_str);
        //}
#if UNITY_EDITOR

        public static void ResetScenceLog()
        {
            foreach (var item in scenceLogDic)
            {
                item.Value.Clear();
            }
        }
        public static Dictionary<int, List<string>> ScenceLogList()
        {
            return scenceLogDic;
        }

        private static void AddScenceLog(int _key, string _str)
        {
            if (scenceLogDic.TryGetValue(_key, out List<string> _value))
            {
                _value.Add(_str);
            }
            else
            {
                List<string> _newList = new List<string>(16);
                _newList.Add(_str);
                scenceLogDic.Add(_key, _newList);
            }
        }
#endif

        public static void DrawLine(Vector3 start, Vector3 end, Color color, float life = 0)
        {
#if UNITY_EDITOR
            EngineScenceDraw.Line(start, end, color, life);
#endif
        }

        public static void DrawBox(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float life = 0)
        {
#if UNITY_EDITOR
            EngineScenceDraw.Box(position, rotation, scale, color, life);
#endif
        }

        public static void DrawSphere(Vector3 pos, float radius, Color color, float life = 0)
        {
#if UNITY_EDITOR
            EngineScenceDraw.Sphere(pos, Quaternion.identity, radius, color, life);
#endif
        }
        public static void DrawSolidArc(Vector3 pos, Vector3 axisY, Vector3 axisZ, float angle, float radius, Color color, float life = 0)
        {
#if UNITY_EDITOR
            EngineScenceDraw.SolidArc(pos, axisY, axisZ, angle, radius, color, life);
#endif
        }
        public static void DrawCapsule(Vector3 startPos, Vector3 endPos, float radius, Color color, float life = 0)
        {
#if UNITY_EDITOR
            EngineScenceDraw.Capsule(startPos, endPos, radius, color, life);
#endif
        }

        public static bool DisplayDialog(string title, string message, string ok, string cancel = "")
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.DisplayDialog(title, message, ok, cancel);
#else
            return false;
#endif

        }

        public static string DebugActionStatePart(ActionStatePart _part)
        {
#if UNITY_EDITOR
            if (_part.ActionStateMachine.CurUnit == null)
            {
                return $"Action:[{_part.CurrentActionState.Name}]  ActionGroup[{_part.ActionStateMachine.ActionGroupID}]";
            }
            else
            {
                return $"Action:[{_part.CurrentActionState.Name}]  ActionGroup[{_part.ActionStateMachine.ActionGroupID}]  " +
                    $"Obj[{_part.ActionStateMachine.CurUnit.gameObject.name}" +
                    $"(<color=#ffcc00>{_part.ActionStateMachine.CurUnit.transform.GetSiblingIndex()}</color>)]";
            }
#endif
            return string.Empty;
        }

        public static void DebugUnitGruphError(BluePrint_Unit _unit)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                if (_unit is GraphEvent_TrackData_Attacker)
                {
                    EngineDebug.LogWarning("单位节点为空了, 此节点为【单位_攻击者】\n可能原因：还未发生过战斗相关交互");
                }
                else if (_unit is GraphEvent_TrackData_OnHiter)
                {
                    EngineDebug.LogWarning("单位节点为空了, 此节点为【单位_命中者】\n可能原因：还未发生过战斗相关交互");
                }
                else
                {
                    EngineDebug.LogWarning($"单位节点为空了, 此节点为【{_unit.GetType().Name}】\n可能原因：还未初始化过");
                }
            }
#endif
        }
    }
}