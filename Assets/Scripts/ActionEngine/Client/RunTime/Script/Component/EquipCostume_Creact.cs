using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class EquipCostume_Creact : MonoBehaviour
    {


        public string mName;
        public float mInteractDis = 5;
        public KeyCode mKey = KeyCode.F;
        public UnitClothPool.ClothType mClothType;
        public bool PickUp = false;

        [Space(20)]
        public string PlayAction;

        // public 

        private void Update()
        {
            ActionEngine_Unit _player = ActionEngineManager_Input.Instance.Player;
            if (_player is null) return;
            if ((_player.transform.position - transform.position).sqrMagnitude < mInteractDis * mInteractDis)
            {
                if (Input.GetKeyDown(mKey))
                {
                    UnitClothPool.Instance.EquipCloth(mName, _player, (int)mClothType, (GameObject _gameobject) =>
                    {
                        if (!string.IsNullOrEmpty(PlayAction))
                        {
                            _player.ActionStateMachine.ChangeAction(PlayAction, 200, 0);
                        }
                        if (PickUp) gameObject.SetActive(false);
                        // EngineDebug.Log("成功着装");
                    });
                }
            }
        }
    }
}