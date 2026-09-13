using AsiActionEngine.RunTime;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class PropCreate : MonoBehaviour
    {
        public enum ESlotType
        {
            MainWeapon = 0,
            SubWeapon = 1
        }

        [Header("装备ID")] public int propName;
        [Header("装备快捷键")] public KeyCode keyCode = KeyCode.E;
        [Header("交互半径(m)")] public float interactRange = 2;
        [Header("挂载挂点")] public ECharacteLimbType point = ECharacteLimbType.HelpPoint_WeaponR;
        [Header("占用槽位")] public ESlotType slotType = ESlotType.MainWeapon;


        [EditorProperty("目标道具(Prop)", EditorPropertyType.EEPT_ListProp)]
        public int DisPropName
        {
            get { return propName; }
            set { propName = value; }
        }

        private ActionEngine_Unit player => ActionEngineManager_Input.Instance.Player;
        private bool EquipComplete_Model = true;//装备加载完成
        private bool EquipComplete_Action = true;//动作模组加载完成
        private void Start()
        {
            //创建装备
            // ActionEngineManager_Unit.Instance.CreateProp(propName, _prop =>
            // {
            //     mProp = _prop;
            // });
        }

        public void Update()
        {
            //装备装载
            if (Input.GetKeyDown(keyCode) && EquipComplete_Model && EquipComplete_Action)
            {
                // EngineDebug.Log("尝试装备0");

                if (player is ActionEngine_Entity _entity)
                {
                    // EngineDebug.Log("尝试装备1");

                    if ((player.transform.position - transform.position).sqrMagnitude < interactRange * interactRange)
                    {
                        // EngineDebug.Log("尝试装备2");

                        //获取道具配置
                        ActionEnginLoadData.Instance.LoadInfo(EInfoType.PropWarp, _obj =>
                        {
                            EquipComplete_Model = false;
                            EquipComplete_Action = false;
                            if (_obj is PropWarp _warp)
                            {
                                //装备武器
                                _entity.EquipProp(propName, (int)slotType, point, _complet =>
                                {
                                    // EngineDebug.Log("成功装备到挂点: " + (ECharacteLimbType)complete.mPropWarp.DefaultPoint);
                                    EquipComplete_Model = true;
                                });

                                //装备动作模组
                                _entity.EquipActionInfo(_warp.DefaultAction, _loadComplet =>
                                {
                                    // EngineDebug.Log("成功装备模组");
                                    EquipComplete_Action = true;
                                });
                            }
                        }, propName);
                    }
                }
            }
        }
    }
}


