using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public class DestoryToRuntime : MonoBehaviour
    {
        private void Start()
        {
            if (TryGetComponent(out ActionEngine_Unit _unit))
            {
                _unit.SelfDestroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}