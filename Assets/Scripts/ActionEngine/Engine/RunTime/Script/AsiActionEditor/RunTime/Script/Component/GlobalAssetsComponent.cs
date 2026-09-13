using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public class GlobalAssetsComponent : MonoBehaviour
    {
        public CurveDictionary curvesMap = new CurveDictionary();
        public GradientDictionary gradientsMap = new GradientDictionary();
        public ColorDictionary colorsMap = new ColorDictionary();
    }

    [System.Serializable] public class CurveDictionary : SerializedDictionary<string, AnimationCurve> { }
    [System.Serializable] public class GradientDictionary : SerializedDictionary<string, Gradient> { }
    [System.Serializable] public class ColorDictionary : SerializedDictionary<string, Color> { }
}