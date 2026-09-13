using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class GameObjectDestory : MonoBehaviour
    {
        private void OnEnable()
        {
#if  UNITY_EDITOR
            DestroyImmediate(gameObject);
#else
            Destroy(gameObject);
#endif
        }
    }
}