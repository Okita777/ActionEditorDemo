using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class PlayerChange : MonoBehaviour
    {
        public float ChangeRange = 2;
        public bool InheritanceCam = true;
        public KeyCode ChangeKey = KeyCode.V;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(ChangeKey))
            {
                Vector3 _pos = ActionEngineManager_Input.Instance.Player.transform.position;
                List<ActionEngine_Unit> _units = ActionEngineManager_Unit.Instance.Units;

                float _minDis = int.MaxValue;
                int _findID = -1;
                int _nowID = _units.IndexOf(ActionEngineManager_Input.Instance.Player);
                for (int i = 0; i < _units.Count; i++)
                {
                    if (i != _nowID)
                    {
                        float _nowDis = (_units[i].transform.position - _pos).sqrMagnitude;
                        if (_nowDis < _minDis)
                        {
                            _findID = i;
                            _minDis = _nowDis;
                        }
                    }
                }

                if (_minDis < (ChangeRange * ChangeRange))
                {
                    ActionEngineManager_Input.Instance.ChangePlayer(_units[_findID], InheritanceCam);
                }
            }
        }
    }
}

