using AsiActionEngine.RunTime.Graph;

namespace AsiTimeLine.Editor
{
    public class DrawBluePrintName
    {
        public static string GetName(string name)
        {
            switch (name)
            {
                case nameof(GraphEvent_TrackData_Transform_CamMain):
                    return "相机 (Transform)";
                case nameof(GraphEvent_TrackData_Vector_InputDir):
                    return "输入方向 (Vector3)";
                case nameof(GraphEvent_TrackData_Unit_Player):
                    return "玩家单位 (Unit)";
                case nameof(GraphEvent_Other_Float_Distance):
                    return "距离 (float)";
                case nameof(GraphEvent_Other_Float_UnitMoveSpeed):
                    return "单位移动速度 (float)";
                case nameof(GraphEvent_Math_RangeDis_GGPoint):
                    return "距离筛选 (GGPoint)";
                case nameof(GraphEvent_Math_RangeDis_GGTransform):
                    return "距离筛选 (GGTransform)";
                case nameof(GraphEvent_Math_RangeDis_GGUnit):
                    return "距离筛选 (GGUnit)";
                case nameof(GraphEvent_Math_RangeAngle_GGPoint):
                    return "角度筛选 (GGPoint)";
                case nameof(GraphEvent_Math_RangeAngle_GGTransform):
                    return "角度筛选 (GGTransform)";
                case nameof(GraphEvent_Math_RangeAngle_GGUnit):
                    return "角度筛选 (GGUnit)";
                case nameof(GraphEvent_TrackData_AllUnit):
                    return "场景中所有单位(GroupUnit)";
                case nameof(GraphEvent_TrackData_CheckNavMesh):
                    return "检查目标点是否在导航网格内(Bool)";
                case nameof(GraphEvent_TrackData_CheckInputKey):
                    return "检查玩家按键(Bool)";
                case nameof(GraphEvent_TrackData_GetSkillToTag):
                    return "按标签获取技能(GroupUnit)";
                case nameof(GraphEvent_TrackData_GetUnitToTag):
                    return "按标签获取单位(GroupUnit)";
                case nameof(GraphEvent_TrackData_GetActionListSlotID):
                    return "按ActionListID获取槽位ID(Int)";
                case nameof(GraphEvent_TrackData_CheckSlotHasAG):
                    return "检查目标Slot是否存在AG(Bool)";
                case nameof(GraphEvent_TrackData_GetActionGroupIDBySlotID):
                    return "按槽位ID获取ActionGroupID(Int)";
                case nameof(GraphEvent_TrackData_GetSkillToUnitTag):
                    return "从单位上按标签获取技能(GroupUnit)";
                case nameof(GraphEvent_TrackData_GetSkillToUnitActionID):
                    return "从单位上按ID获取技能(GroupUnit)";
                case nameof(GraphEvent_Math_Range_Point):
                    return "从数组取最终对象 (Point)";
                case nameof(GraphEvent_Math_Range_Transform):
                    return "从数组取最终对象 (Transform)";
                case nameof(GraphEvent_Math_Range_Unit):
                    return "从数组取最终对象 (Unit)";
                case nameof(GraphEvent_Math_Add_GroupUnit_Group):
                    return "数学运算/加 (GroupUnit)";
                case nameof(GraphEvent_Math_Add_GroupUnit_Unit):
                    return "数学运算/加 (Unit)";
                case nameof(GraphEvent_Math_Sub_GroupUnit_Group):
                    return "数学运算/减 (GroupUnit)";
                case nameof(GraphEvent_Math_Sub_GroupUnit_Unit):
                    return "数学运算/减 (Unit)";
                case nameof(GraphEvent_Math_Contains_GroupUnit_Unit):
                    return "判断是否包含 (Bool)";
                case nameof(GraphEvent_Math_Clear_GroupUnit):
                    return "清空数组 (GroupUnit)";
                case nameof(GraphEvent_Math_For_GroupPoint):
                    return "自定条件筛选_loop (GroupPoint)";
                case nameof(GraphEvent_Math_CustomFor_PointData):
                    return "自定义循环 (PointData)";
                case nameof(GraphEvent_Math_For_GroupUnit):
                    return "自定条件筛选_loop (GroupUnit)";
                case nameof(GraphEvent_Math_For_GroupTransform):
                    return "自定条件筛选_loop (GroupTransform)";
                case nameof(GraphEvent_Math_For_GroupInt):
                    return "自定条件筛选_loop (GroupInt)";
                case nameof(GraphEvent_Math_For_GroupFloat):
                    return "自定条件筛选_loop (GroupFloat)";
                case nameof(GraphEvent_Math_For_GroupBool):
                    return "自定条件筛选_loop (GroupBool)";
                case nameof(GraphEvent_Math_For_GroupString):
                    return "自定条件筛选_loop (GroupString)";
                case nameof(GraphEvent_Math_Sort_GroupUnit):
                    return "自定条件排序_loop (GroupUnit)";
                case nameof(GraphEvent_Other_Float_Dot):
                    return "点积 (float)";
                case nameof(GraphEvent_Other_GetUnitID):
                    return "获取单位ID";
                case nameof(GraphEvent_Other_Vector3_RotVector):
                    return "旋转Vector3 (Vector3)";
                case nameof(GraphEvent_Other_Vector3_Angle):
                    return "角度差 (float)";
            }
            return string.Empty;
        }
    }
}