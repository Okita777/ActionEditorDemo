using AsiActionEngine.RunTime;
using System.Collections.Generic;
using UnityEngine;
using static AsiTimeLine.RunTime.IInteractObject;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_InteractRayCast : MonoBehaviour
    {
        [Header("第一层射线判断(无穿透)")] public LayerMask mFristRayMask;
        [Header("默认判断，未射中交互对象时进行二次检测")] public LayerMask mDefaultRayMask;
        [Header("地面检测层")] public LayerMask mGroundMask;
        [Header("球形检测层")] public LayerMask mSphereMask;
        [Header("球形检测半径")] public float mSphereRadius;

        [SerializeField] [HideInInspector] private ActionEngineUtility.InputRayCastGV GVData = new ActionEngineUtility.InputRayCastGV();

        [EditorProperty("[Point] 鼠标在地面的位置", EditorPropertyType.EEPT_GPoint, LabelWidth = 150)]
        public GPoint MousePos
        {
            get { return GVData.mMousePos; }
            set { GVData.mMousePos = value; }
        }

        [EditorProperty("[Point] 交互点位置", EditorPropertyType.EEPT_GPoint, LabelWidth = 150)]
        public GPoint InteractPoint
        {
            get { return GVData.mInteractPoint; }
            set { GVData.mInteractPoint = value; }
        }

        [EditorProperty("[Float] 触发交互的半径", EditorPropertyType.EEPT_Float, LabelWidth = 150)]
        public GFloat InteractRange
        {
            get { return GVData.mInteractRange; }
            set { GVData.mInteractRange = value; }
        }

        [EditorProperty("[Point] 交互对象位置", EditorPropertyType.EEPT_GPoint, LabelWidth = 150)]
        public GPoint InteractObjPoint
        {
            get { return GVData.mInteractObjPoint; }
            set { GVData.mInteractObjPoint = value; }
        }

        [EditorProperty("[Enum] 交互对象类型", EditorPropertyType.EEPT_Enum, LabelWidth = 150)]
        public GEnum SetType
        {
            get { return GVData.mSetType; }
            set { GVData.mSetType = value; }
        }

        [EditorProperty("[Enum] 对齐类型", EditorPropertyType.EEPT_Enum, LabelWidth = 150)]
        public GEnum InteractType
        {
            get { return GVData.mInteractType; }
            set { GVData.mInteractType = value; }
        }

        [EditorProperty("[Unit] 写入的单位(可空)", EditorPropertyType.EEPT_GUnit, LabelWidth = 150)]
        public GUnit SetTarget
        {
            get { return GVData.mSetTarget; }
            set { GVData.mSetTarget = value; }
        }

        [EditorProperty("[Bool] 是否有可交互对象", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public GBool IsCanInteract
        {
            get { return GVData.mIsCanInteract; }
            set { GVData.mIsCanInteract = value; }
        }

        [EditorProperty("[Bool] 是否检测到地面", EditorPropertyType.EEPT_Bool, LabelWidth = 150)]
        public GBool IsGround
        {
            get { return GVData.mIsGround; }
            set { GVData.mIsGround = value; }
        }

        [EditorProperty("[Int] 交互所需的ActionID", EditorPropertyType.EEPT_GInt, LabelWidth = 150)]
        public GInt InteractActionID
        {
            get { return GVData.mInteractActionID; }
            set { GVData.mInteractActionID = value; }
        }

        //private IInteractObject[] interactObjects = new IInteractObject[20];
        //private int interactObjectsCount = 0;

        private Ray rayData;

        private void Update()
        {
            rayData = Camera.main.ScreenPointToRay(Input.mousePosition);

            ActionEngineUtility.UpdateRayCast(rayData, GVData, mSphereRadius, mFristRayMask, mDefaultRayMask, mGroundMask, mSphereMask);
        }

        
    }
}