using System.Collections.Generic;

namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 蓝图求值时的局部参数上下文。
    /// 由 GraphEvent_NoValue_* 在 value() 调用前设置，求值完毕后清除。
    /// </summary>
    public static class BluePrintLocalContext
    {
        private class localListParams
        {
            public List<int> s_IntParams = null;
            public List<float> s_FloatParams = null;
            public List<bool> s_BoolParams = null;
            public List<string> s_StringParams = null;
        }
        private static List<localListParams> mlocalListParams = new List<localListParams>(24);
        private static int NowID = -1;
        public static void Set(List<int> intParams, List<float> floatParams, List<bool> boolParams, List<string> stringParams)
        {
            if (mlocalListParams.Count <= NowID + 1) mlocalListParams.Add(new localListParams());

            NowID++;
            mlocalListParams[NowID].s_IntParams = intParams;
            mlocalListParams[NowID].s_FloatParams = floatParams;
            mlocalListParams[NowID].s_BoolParams = boolParams;
            mlocalListParams[NowID].s_StringParams = stringParams;
        }

        public static void Clear()
        {
            //NowID = 0;
            NowID--;
        }

        public static int GetInt(int index)
        {
            localListParams _localListParams = mlocalListParams[NowID];
            List<int> s_IntParams = _localListParams.s_IntParams;
            if (s_IntParams == null || index < 0 || index >= s_IntParams.Count) return 0;
            return s_IntParams[index];
        }

        public static float GetFloat(int index)
        {
            localListParams _localListParams = mlocalListParams[NowID];
            List<float> s_FloatParams = _localListParams.s_FloatParams;
            if (s_FloatParams == null || index < 0 || index >= s_FloatParams.Count) return 0f;
            return s_FloatParams[index];
        }

        public static bool GetBool(int index)
        {
            localListParams _localListParams = mlocalListParams[NowID];
            List<bool> s_BoolParams = _localListParams.s_BoolParams;
            if (s_BoolParams == null || index < 0 || index >= s_BoolParams.Count) return false;
            return s_BoolParams[index];
        }

        public static string GetString(int index)
        {
            localListParams _localListParams = mlocalListParams[NowID];
            List<string> s_StringParams = _localListParams.s_StringParams;
            if (s_StringParams == null || index < 0 || index >= s_StringParams.Count) return string.Empty;
            return s_StringParams[index];
        }

        public static void SetInt(int index, int value)
        {
            localListParams _localListParams = mlocalListParams[NowID];
            List<int> s_IntParams = _localListParams.s_IntParams;
            if (s_IntParams == null || index < 0) return;
            while (s_IntParams.Count <= index) s_IntParams.Add(0);
            s_IntParams[index] = value;
        }

        public static void SetFloat(int index, float value)
        {
            localListParams _localListParams = mlocalListParams[NowID];
            List<float> s_FloatParams = _localListParams.s_FloatParams;
            if (s_FloatParams == null || index < 0) return;
            while (s_FloatParams.Count <= index) s_FloatParams.Add(0f);
            s_FloatParams[index] = value;
        }

        public static void SetBool(int index, bool value)
        {
            localListParams _localListParams = mlocalListParams[NowID];
            List<bool> s_BoolParams = _localListParams.s_BoolParams;
            if (s_BoolParams == null || index < 0) return;
            while (s_BoolParams.Count <= index) s_BoolParams.Add(false);
            s_BoolParams[index] = value;
        }

        public static void SetString(int index, string value)
        {
            localListParams _localListParams = mlocalListParams[NowID];
            List<string> s_StringParams = _localListParams.s_StringParams;
            if (s_StringParams == null || index < 0) return;
            while (s_StringParams.Count <= index) s_StringParams.Add(string.Empty);
            s_StringParams[index] = value;
        }
    }
}
