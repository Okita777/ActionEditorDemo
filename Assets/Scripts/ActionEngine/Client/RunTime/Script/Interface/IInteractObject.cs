using AsiActionEngine.RunTime;
using System;
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public interface IInteractObject
    {
        public enum EInteractPointType
        {
            [InspectorName("不对齐")] NotAlign,
            [InspectorName("对齐位置和朝向")] Align,
            [InspectorName("仅对齐位置")] OnlyAlignToPos,
            [InspectorName("面向交互位置")] OnlyLook,
            [InspectorName("对齐位置后面向交互位置")] AlignPosAndLook,
        }

        public enum EInteractType
        {
            [InspectorName("单位")] Unit,
            [InspectorName("掉落物")] DropItem,
            [InspectorName("场景道具")] Prop,
            [InspectorName("剧情触发区")] CutsceneTrigger,
        }

        Vector3 Point_Pos();//交互点位  最终和单位交互的位置(比如NPC为自身中心, 而宝箱或者门则是自身前方一些)
        Vector3 Point_Dir();//交互朝向  单位到达交互位置时最终要面向的朝向  vector3.zero代表无朝向限制
        float Point_Radius();//交互半径  以交互点位为中心扩散的交互半径  在这个半径内触发交互(相对Root偏移)
        int InteractActionID() { return -1; }//交互的Action名称  交互时要求播放的动画
        ActionEngine_Unit CurUnit() { return null; }//当前单位组件
        EInteractPointType AlignType() { return EInteractPointType.NotAlign; }//触发交互后,要求交互对象的对齐类型
        EInteractType InteractType();


        /// <summary>
        /// 触发交互时调用的函数
        /// </summary>
        /// <param name="_actionState">目标交互单位</param>
        void OnTrigger(ActionStateMachine _actionState) { }
    }
}