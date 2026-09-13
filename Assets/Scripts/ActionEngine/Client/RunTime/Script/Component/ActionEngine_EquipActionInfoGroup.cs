using System.Collections.Generic;
using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    [System.Serializable]
    public class EquipActionListEntry
    {
        public bool enabled = true;
        public int actionGroupId;
        public int SlotID;
    }

    public class ActionEngine_EquipActionInfoGroup : MonoBehaviour
    {
        public TargetUnit targetUnit;
        public bool AutoEquip = true;
        public bool useKey = false;
        public KeyCode equipKey;
        [HideInInspector] public List<EquipActionListEntry> ActionList = new List<EquipActionListEntry>();

        private void Start()
        {
            if (targetUnit == null)
            {
                ActionEngineManager_Input.Instance.WaitPlayerLoad(unit =>
                {
                    targetUnit = unit;
                    if (AutoEquip)
                    {
                        EquipActionInfo();
                    }
                });
            }
            else
            {
                if (AutoEquip)
                {
                    EquipActionInfo();
                }
            }
        }

        private void Update()
        {
            if (useKey && Input.GetKeyDown(equipKey))
            {
                EquipActionInfo();
            }
        }

        public void EquipActionInfo()
        {
            if (targetUnit == null)
            {
                return;
            }

            var unit = targetUnit.GetUnit();
            if (unit == null)
            {
                return;
            }

            if (unit.GetSource is not ActionEngine_Entity entity)
            {
                return;
            }

            if (ActionList == null || ActionList.Count == 0)
            {
                return;
            }

            foreach (EquipActionListEntry entryPart in ActionList)
            {
                EquipActionListEntry entry = entryPart;
                if (entry == null || !entry.enabled)
                {
                    continue;
                }

                int item = entry.actionGroupId;
                ActionEnginLoadData.Instance.LoadInfo(EInfoType.UnitAction, target =>
                {
                    if (target is ActionStateInfo)
                    {
                        entity.EquipActionInfo(item, _complet => {
                            entity.EquipActionListDic(item, entry.SlotID);
                        });
                    }
                }, item);
            }
        }
    }
}
