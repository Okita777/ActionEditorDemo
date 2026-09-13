using System;
using System.Collections.Generic;
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public interface INodeEdiDataHolder
    {
        NodeEdiData NodeEdiData { get; set; }
        List<int> LocalIntParams { get; }
        List<float> LocalFloatParams { get; }
        List<bool> LocalBoolParams { get; }
        List<string> LocalStringParams { get; }
    }

    // #if UNITY_EDITOR
    //Editor专用的序列化字典
    [Serializable] public class InterrupOffset : SerializedDictionary<string, int> { }
    // #endif

    //Config用到的序列化字典
    [Serializable] public class CharacterLimbDic : SerializedDictionary<ECharacteLimbType, Transform> { }

    [Serializable] public class CinemachineDic : SerializedDictionary<string, Behaviour[]> { }

    [Serializable] public class AudioSourceDic : SerializedDictionary<string, AudioSource> { }
    [Serializable] public class TrackColorDic : SerializedDictionary<string, TrackDrawData> { }

    [Serializable]
    public class SerAnimationCurve : SerializedAnimCurve
    {
        public SerAnimationCurve Clone()
        {
            SerAnimationCurve _new = new SerAnimationCurve();
            _new.CopyFrom(this);
            return _new;
        }
    }

    [Serializable] public class GraphEdiDataDic : SerializedDictionary<string, AudioSource> { }
    [Serializable] public class DicStringID : SerializedDictionary<int, string> { }
    [Serializable] public class DicStringS : SerializedDictionary<string, string> { }
    [Serializable] public class DicGVType : SerializedDictionary<EGValueType, List<string>> { }


    [Serializable]
    public struct NodePos
    {
        public float x;
        public float y;
        public NodePos(float x, float y) { this.x = x; this.y = y; }
        public static implicit operator Vector2(NodePos p) => new Vector2(p.x, p.y);
        public static implicit operator NodePos(Vector2 v) => new NodePos(v.x, v.y);
    }

    [Serializable]
    public class BluePrintComment
    {
        public string text = "注释";
        public NodePos position;
        public NodePos size = new NodePos(180, 60);
        public int colorIndex;
    }

    [Serializable]
    public class BluePrintGroup
    {
        public string title = "节点组";
        public NodePos position;
        public NodePos size = new NodePos(400, 300);
        public int colorIndex;
    }

    public enum EBluePrintLocalParamType { Int, Float, Bool, String }

    [Serializable]
    public class BluePrintLocalParamDef
    {
        public string paramName = "NewParam";
        public EBluePrintLocalParamType paramType = EBluePrintLocalParamType.Int;
        public bool drawToInspector;

        public BluePrintLocalParamDef() { }

        public BluePrintLocalParamDef(string name, EBluePrintLocalParamType type)
        {
            paramName = name;
            paramType = type;
        }

        public BluePrintLocalParamDef Clone()
        {
            return new BluePrintLocalParamDef
            {
                paramName = paramName,
                paramType = paramType,
                drawToInspector = drawToInspector
            };
        }
    }

    [Serializable]
    public class NodeEdiData
    {
        public List<NodePos> nodePositions = new List<NodePos>();
        public string nodeTitle = "蓝图功能描述";
        public string nodeToolTip = "蓝图功能的详细描述";
        public List<BluePrintComment> comments = new List<BluePrintComment>();
        public List<BluePrintGroup> groups = new List<BluePrintGroup>();
        public List<BluePrintLocalParamDef> localParams = new List<BluePrintLocalParamDef>();
        public NodePos viewPosition;
        public float viewZoom = 1f;

        public NodeEdiData Clone()
        {
            NodeEdiData _node = new NodeEdiData();
            _node.nodeTitle = this.nodeTitle;
            _node.nodeToolTip = this.nodeToolTip;
            _node.nodePositions = new List<NodePos>(nodePositions);
            _node.viewPosition = this.viewPosition;
            _node.viewZoom = this.viewZoom;
            foreach (var c in comments)
                _node.comments.Add(new BluePrintComment
                    { text = c.text, position = c.position, size = c.size, colorIndex = c.colorIndex });
            foreach (var g in groups)
                _node.groups.Add(new BluePrintGroup
                    { title = g.title, position = g.position, size = g.size, colorIndex = g.colorIndex });
            foreach (var p in localParams)
                _node.localParams.Add(p.Clone());
            return _node;
        }

        public void SetNewPos(NodeEdiData newPos)
        {
            nodePositions.Clear();
            nodePositions.AddRange(newPos.nodePositions);
        }

        public int GetLocalParamCount(EBluePrintLocalParamType type)
        {
            int count = 0;
            foreach (var p in localParams)
                if (p.paramType == type) count++;
            return count;
        }

        public int GetTypedIndex(int globalIndex)
        {
            if (globalIndex < 0 || globalIndex >= localParams.Count) return -1;
            var target = localParams[globalIndex];
            int typedIndex = 0;
            for (int i = 0; i < globalIndex; i++)
                if (localParams[i].paramType == target.paramType) typedIndex++;
            return typedIndex;
        }

        public int GetGlobalIndex(EBluePrintLocalParamType type, int typedIndex)
        {
            int count = 0;
            for (int i = 0; i < localParams.Count; i++)
            {
                if (localParams[i].paramType == type)
                {
                    if (count == typedIndex) return i;
                    count++;
                }
            }
            return -1;
        }
    }

    [Serializable]
    public struct TrackDrawData
    {
        public Color Color;
        public int Style;
        public TrackDrawData(Color _color, int _style)
        {
            Color = _color;
            Style = _style;
        }
    }
}