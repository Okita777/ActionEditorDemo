using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class EquipCostume_Default : MonoBehaviour
    {
        public List<UnitClothPool.ClothType> mClothType = new List<UnitClothPool.ClothType>();
        public List<string> mClothName = new List<string>();
        private float mDelayTime = 1.0f;

        private bool mInit = false;

        private void Update()
        {
            if (mInit)
            {
                return;
            }

            mDelayTime -= Time.deltaTime;
            if (mDelayTime > 0) return;
            for (int i = 0; i < mClothType.Count; i++)
            {
                ActionEngine_Unit _player = ActionEngineManager_Input.Instance.Player;
                UnitClothPool.Instance.EquipCloth(mClothName[i], _player, (int)mClothType[i], (GameObject _cloth) =>
                {
                    // EngineDebug.Log($"已完成装备");
                });
            }

            mInit = true;
        }
    }
}