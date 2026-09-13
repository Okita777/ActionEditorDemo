
namespace AsiActionEngine.RunTime
{
    public partial class ActionStateMachine
    {
        //        //武器字典-武器类型
        //        private Dictionary<int, SItemWarp> PropWarp_Type_Dic = 
        //            new Dictionary<int, SItemWarp>(100);
        //        //武器字典-武器槽位
        //        private Dictionary<(ushort, ushort, byte), SItemWarp> PropWarp_Cell_Dic = 
        //            new Dictionary<(ushort, ushort, byte), SItemWarp>(100);
        //        //武器槽位字典
        //        private Dictionary<(ushort, ushort, byte), List<GEnum>> Cell_Dic = new Dictionary<(ushort, ushort, byte), List<GEnum>>(100);
        //        //当前已占用槽位
        //        private HashSet<(ushort, ushort, byte)> PropCell_List = new HashSet<(ushort, ushort, byte)>(100);
        //        //private Dictionary<(ushort, ushort, byte), SItemWarp> mEquipItems = new Dictionary<(ushort, ushort, byte), SItemWarp>();

        //        /// <summary>
        //        /// 从道具类型获取道具
        //        /// </summary>
        //        /// <param name="_type">类型</param>
        //        /// <param name="warp">返回的道具</param>
        //        /// <returns></returns>
        //        public bool GetPropWarpToType(PropWarp _type, out SItemWarp warp) => OnGetPropWarpToType(_type, out warp);
        //        /// <summary>
        //        /// 从槽位类型获取道具
        //        /// </summary>
        //        /// <param name="_type">槽位类型</param>
        //        /// <param name="warp">返回的道具</param>
        //        /// <returns></returns>
        //        public bool GetPropWarpToCell(GEnum _type, out SItemWarp warp) => OnGetPropWarpToCell(_type, out warp);
        //        public void SetPropWarp(PropWarp _type, SItemWarp warp, bool _usePropTypes2) => OnSetPropWarp(_type, warp, _usePropTypes2);
        //        //public void AddPropWarp(PropWarp _type, SItemWarp warp) => OnAddPropWarp(_type, warp);
        //        public void RemovePropWarp(PropWarp _type) => OnRemovePropWarp(_type);

        //        /// <summary>
        //        /// 检查槽位是否被占用
        //        /// </summary>
        //        /// <param name="_type"></param>
        //        /// <returns></returns>
        //        public bool CheckPropCell(GEnum _type) => PropCell_List.Contains((_type.mValueGroupIndex, _type.mValueIndex, _type.mSerValue));

        //        private void Init_PropCell()
        //        {
        //            PropWarp_Type_Dic.Clear();
        //            PropWarp_Cell_Dic.Clear();
        //            Cell_Dic.Clear();
        //            PropCell_List.Clear();
        //        }
        //        private bool OnGetPropWarpToType(PropWarp _type, out SItemWarp warp)
        //        {
        //            if(PropWarp_Type_Dic.TryGetValue(_type.GetHashCode(), out warp))
        //                return true;

        //            EngineDebug.GetGEnumNames(_type.PropType, out string _groupName, out string _gvName, out string _enumName);
        //            EngineDebug.LogError($"未装备过此类型装备[{_enumName}]  gv:[{_gvName}] ，<color=#ffcc00>卸载失败</color>");
        //            //EngineDebug.LogWarning($"<color=ffcc00>[武器获取失败，类型]</color>，" +
        //            //    $"从未加载过 GEnum:   GroupIndex[{_type.mValueGroupIndex}]  " +
        //            //    $"mValueIndex[{_type.mValueIndex}]  " +
        //            //    $"mSerValue[{_type.mSerValue}]");
        //            return false;
        //        }

        //        private bool OnGetPropWarpToCell(GEnum _type, out SItemWarp warp)
        //        {
        //            if (PropWarp_Cell_Dic.TryGetValue((_type.mValueGroupIndex, _type.mValueIndex, _type.mSerValue), out warp))
        //                return true;

        //            EngineDebug.LogWarning($"<color=ffcc00>[武器获取失败，槽位]</color>，" +
        //                $"未装备 GEnum:   GroupIndex[{_type.mValueGroupIndex}]  " +
        //                $"mValueIndex[{_type.mValueIndex}]  " +
        //                $"mSerValue[{_type.mSerValue}]");
        //            return false;
        //        }

        //        private void OnSetPropWarp(PropWarp _type, SItemWarp warp, bool _usePropTypes2)
        //        {
        //            //切换或添加武器
        //            if (!PropWarp_Type_Dic.TryAdd(_type.HashCode(), warp))
        //                PropWarp_Type_Dic[_type.HashCode()] = warp;

        //            List<GEnum> _nowCell = _usePropTypes2 ? _type.PropTypes2 : _type.PropTypes;//当前武器决定占用的槽位

        //            //槽位切换,删除旧武器槽位
        //            if (Cell_Dic.TryGetValue(_type.PropType.GetKey, out List<GEnum> _gEnum))
        //            {
        //                foreach (GEnum item in _gEnum)
        //                {
        //                    PropWarp_Cell_Dic.Remove(item.GetKey);
        //                }
        //                //槽位切换,切换至当前武器
        //                Cell_Dic[_type.PropType.GetKey] = _nowCell;
        //            }
        //            else
        //            {
        //                Cell_Dic.Add(_type.PropType.GetKey, _nowCell);
        //                //EngineDebug.LogError("武器占位槽切换失败");
        //            }
        //#if UNITY_EDITOR
        //            if(warp._prop is not null)
        //            {
        //                EngineDebug.GetGEnumNames(_type.PropType, out string _groupName2, out string _gvName2, out string _enumName2);
        //                EngineDebug.Log($"成功装备[{_enumName2}]  gv:[{_gvName2}]  Name[{warp._prop.gameObject.name}]，<color=#ffcc00>成功装备</color>");
        //            }
        //#endif

        //            foreach (GEnum item in _nowCell)
        //            {
        //                if(PropWarp_Cell_Dic.TryAdd(item.GetKey, warp))
        //                {
        //                    //EngineDebug.GetGEnumNames(item, out string _groupName, out string _gvName, out string _enumName);
        //                    //EngineDebug.LogError($"装备[{_enumName}]  gv:[{_gvName}] ，<color=#ffcc00>成功装备</color>");
        //                }
        //            }
        //        }
        //        //private void OnAddPropWarp(PropWarp _type, SItemWarp warp)
        //        //{
        //        //    PropWarp_Type_Dic.Add(_type.PropType.GetKey, warp);

        //        //    //添加武器占位槽
        //        //    if (!Cell_Dic.TryAdd(_type.PropType.GetKey, _type.PropTypes))
        //        //    {
        //        //        Cell_Dic[_type.PropType.GetKey] = _type.PropTypes;
        //        //        EngineDebug.LogError("武器占位槽添加失败");
        //        //    }

        //        //    foreach (GEnum item in _type.PropTypes)
        //        //    {
        //        //        PropWarp_Cell_Dic.TryAdd(item.GetKey, warp);
        //        //    }
        //        //}
        //        private void OnRemovePropWarp(PropWarp _type)
        //        {
        //            int _hashCode = _type.HashCode();
        //            if(PropWarp_Type_Dic.TryGetValue(_hashCode, out SItemWarp warp))
        //            {
        //                PropWarp_Type_Dic.Remove(_hashCode);
        //            }
        //            else
        //            {
        //                EngineDebug.GetGEnumNames(_type.PropType, out string _groupName, out string _gvName, out string _enumName);
        //                EngineDebug.LogError($"未装备过此类型装备[{_enumName}]  gv:[{_gvName}] ，<color=#ffcc00>卸载失败</color>");
        //            }

        //            if (Cell_Dic.TryGetValue(_type.PropType.GetKey, out List<GEnum> _gEnum))
        //            {
        //                foreach (GEnum item in _gEnum)
        //                {
        //                    PropWarp_Cell_Dic.Remove(item.GetKey);
        //                }
        //                Cell_Dic.Remove(_type.PropType.GetKey);
        //            }
        //            //else
        //            //{
        //            //    EngineDebug.GetGEnumNames(_type, out string _groupName, out string _gvName, out string _enumName);
        //            //    EngineDebug.LogError($"未装备过此类型装备[{_enumName}]  gv:[{_gvName}] ，<color=#ffcc00>卸载失败</color>");
        //            //}
        //        }



        //        public (ushort, ushort, byte) GetValue(GEnum _enum)
        //        {
        //            return (_enum.mValueGroupIndex, _enum.mValueIndex, _enum.mSerValue);
        //        }
    }
}