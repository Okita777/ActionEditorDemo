using AsiActionEngine.RunTime;
using AsiActionEngine.RunTime.Graph;

namespace AsiTimeLine.Editor
{
    public class CreactBluePrint
    {
        public static IProperty CreateBluePrint(string bluePrintName)
        {
            switch (bluePrintName)
            {
                case nameof(GraphEvent_TrackData_Transform_CamMain):
                    return new GraphEvent_TrackData_Transform_CamMain();
                case nameof(GraphEvent_TrackData_Vector_InputDir):
                    return new GraphEvent_TrackData_Vector_InputDir();
                case nameof(GraphEvent_TrackData_Unit_Player):
                    return new GraphEvent_TrackData_Unit_Player();
                case nameof(GraphEvent_Other_Float_Distance):
                    return new GraphEvent_Other_Float_Distance();
                case nameof(GraphEvent_Other_Float_UnitMoveSpeed):
                    return new GraphEvent_Other_Float_UnitMoveSpeed();
                case nameof(GraphEvent_Math_RangeDis_GGPoint):
                    return new GraphEvent_Math_RangeDis_GGPoint();
                case nameof(GraphEvent_Math_RangeDis_GGTransform):
                    return new GraphEvent_Math_RangeDis_GGTransform();
                case nameof(GraphEvent_Math_RangeDis_GGUnit):
                    return new GraphEvent_Math_RangeDis_GGUnit();
                case nameof(GraphEvent_Math_RangeAngle_GGPoint):
                    return new GraphEvent_Math_RangeAngle_GGPoint();
                case nameof(GraphEvent_Math_RangeAngle_GGTransform):
                    return new GraphEvent_Math_RangeAngle_GGTransform();
                case nameof(GraphEvent_Math_RangeAngle_GGUnit):
                    return new GraphEvent_Math_RangeAngle_GGUnit();
                case nameof(GraphEvent_Math_Sort_GroupUnit):
                    return new GraphEvent_Math_Sort_GroupUnit();
                case nameof(GraphEvent_Math_Range_Point):
                    return new GraphEvent_Math_Range_Point();
                case nameof(GraphEvent_TrackData_AllUnit):
                    return new GraphEvent_TrackData_AllUnit();
                case nameof(GraphEvent_TrackData_CheckNavMesh):
                    return new GraphEvent_TrackData_CheckNavMesh();
                case nameof(GraphEvent_TrackData_CheckInputKey):
                    return new GraphEvent_TrackData_CheckInputKey();
                case nameof(GraphEvent_TrackData_GetSkillToTag):
                    return new GraphEvent_TrackData_GetSkillToTag();
                case nameof(GraphEvent_TrackData_GetUnitToTag):
                    return new GraphEvent_TrackData_GetUnitToTag();
                case nameof(GraphEvent_TrackData_GetActionListSlotID):
                    return new GraphEvent_TrackData_GetActionListSlotID();
                case nameof(GraphEvent_TrackData_CheckSlotHasAG):
                    return new GraphEvent_TrackData_CheckSlotHasAG();
                case nameof(GraphEvent_TrackData_GetActionGroupIDBySlotID):
                    return new GraphEvent_TrackData_GetActionGroupIDBySlotID();
                case nameof(GraphEvent_TrackData_GetSkillToUnitTag):
                    return new GraphEvent_TrackData_GetSkillToUnitTag();
                case nameof(GraphEvent_TrackData_GetSkillToUnitActionID):
                    return new GraphEvent_TrackData_GetSkillToUnitActionID();
                case nameof(GraphEvent_Math_Range_Transform):
                    return new GraphEvent_Math_Range_Transform();
                case nameof(GraphEvent_Math_Range_Unit):
                    return new GraphEvent_Math_Range_Unit();
                case nameof(GraphEvent_Other_Float_Dot):
                    return new GraphEvent_Other_Float_Dot();
                case nameof(GraphEvent_Other_GetUnitID):
                    return new GraphEvent_Other_GetUnitID();
                case nameof(GraphEvent_Other_Vector3_RotVector):
                    return new GraphEvent_Other_Vector3_RotVector();
                case nameof(GraphEvent_Other_Vector3_Angle):
                    return new GraphEvent_Other_Vector3_Angle();
                case nameof(GraphEvent_Math_For_GroupPoint):
                    return new GraphEvent_Math_For_GroupPoint();
                case nameof(GraphEvent_Math_CustomFor_PointData):
                    return new GraphEvent_Math_CustomFor_PointData();
                case nameof(GraphEvent_Math_Add_GroupUnit_Group):
                    return new GraphEvent_Math_Add_GroupUnit_Group();
                case nameof(GraphEvent_Math_Add_GroupUnit_Unit):
                    return new GraphEvent_Math_Add_GroupUnit_Unit();
                case nameof(GraphEvent_Math_Sub_GroupUnit_Group):
                    return new GraphEvent_Math_Sub_GroupUnit_Group();
                case nameof(GraphEvent_Math_Sub_GroupUnit_Unit):
                    return new GraphEvent_Math_Sub_GroupUnit_Unit();
                case nameof(GraphEvent_Math_Contains_GroupUnit_Unit):
                    return new GraphEvent_Math_Contains_GroupUnit_Unit();
                case nameof(GraphEvent_Math_Clear_GroupUnit):
                    return new GraphEvent_Math_Clear_GroupUnit();
                case nameof(GraphEvent_Math_For_GroupTransform):
                    return new GraphEvent_Math_For_GroupTransform();
                case nameof(GraphEvent_Math_For_GroupUnit):
                    return new GraphEvent_Math_For_GroupUnit();
                case nameof(GraphEvent_Math_For_GroupInt):
                    return new GraphEvent_Math_For_GroupInt();
                case nameof(GraphEvent_Math_For_GroupFloat):
                    return new GraphEvent_Math_For_GroupFloat();
                case nameof(GraphEvent_Math_For_GroupBool):
                    return new GraphEvent_Math_For_GroupBool();
                case nameof(GraphEvent_Math_For_GroupString):
                    return new GraphEvent_Math_For_GroupString();
            }

            return null;
        }
    }
}