using AsiActionEngine.RunTime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AsiTimeLine.RunTime.IInteractObject;

namespace AsiTimeLine.RunTime
{
    public partial class ActionEngineUtility
    {
        [System.Serializable]
        public class InputRayCastGV
        {
            public GPoint mMousePos = new GPoint(true);//鼠标在地面上的位置
            public GPoint mInteractPoint = new GPoint(true);//交互点位置
            public GPoint mInteractObjPoint = new GPoint(true);//交互对象位置
            public GUnit mSetTarget = new GUnit(true);
            public GEnum mSetType = new GEnum(0, true);
            public GEnum mInteractType = new GEnum(0, true);
            public GBool mIsGround = new GBool(true);
            public GBool mIsCanInteract = new GBool(true);
            public GInt mInteractActionID = new GInt(0, true);
            public GFloat mInteractRange = new GFloat(0, true);

            public void InitGVState()
            {
                mMousePos.mType = true;
                mInteractPoint.mType = true;
                mInteractObjPoint.mType = true;
                mInteractType.mType = true;
                mSetTarget.mType = true;
                mSetType.mType = true;
                mIsGround.mType = true;
                mIsCanInteract.mType = true;
                mInteractActionID.mType = true;
                mInteractRange.mType = true;
            }
        }

        private static IInteractObject mInteractObject;
        private static ActionEngine_Unit player => ActionEngineManager_Input.Instance.Player;
        private static ActionStatePart part => player.ActionStateMachine.FirstStatePart;
        private static RaycastHit RaycastHit;
        private static bool interactIsValid = false;
        private static Transform interactTrans = null;
        public static void UpdateRayCast(Ray ray, InputRayCastGV GV, float SphereRadius,
            LayerMask FirstMask, LayerMask DefaultRayMask, LayerMask GroundMask, LayerMask SphereMask)
        {
            if (player == null) return;
            GV.InitGVState();
            //第一层射线，优先获取交互对象
            if (Physics.Raycast(ray, out RaycastHit, 100, FirstMask))
            {
                //if (RaycastHit.collider.TryGetComponent(out IInteractObject interactObject))
                if(TryGetInteractObject(RaycastHit.collider.transform, out IInteractObject interactObject, out Transform interactObjectTransform))
                {
                    SetInteractObject(interactObject, interactObjectTransform, GV);
                }
                else
                {
                    DebugInteractError(RaycastHit.collider.gameObject);
                    SetInteractObject(null, null, GV);
                }
            }
            else
            {
                if (Physics.Raycast(ray, out RaycastHit, 100, DefaultRayMask))
                {
                    //if (RaycastHit.collider.TryGetComponent(out IInteractObject interactObject))
                    if (TryGetInteractObject(RaycastHit.collider.transform, out IInteractObject interactObject, out Transform interactObjectTransform))
                    {
                        //射线获取到了交互对象
                        SetInteractObject(interactObject, interactObjectTransform, GV);
                    }
                    else
                    {
                        //if(mGroundMask.co)
                        if ((GroundMask.value & (1 << RaycastHit.collider.gameObject.layer)) != 0)
                        {
                            //当前检测到的是地面
                            int castNumber = Physics.OverlapSphereNonAlloc(RaycastHit.point, SphereRadius,
                                EngineResourcesManager.Instance.Colliders, SphereMask);

                            //float minDis = float.MaxValue;
                            //IInteractObject selectInteractObj = null;
                            //Transform selectTrans = null;
                            //for (int i = 0; i < castNumber; i++)
                            //{
                            //    Collider curCollider = EngineResourcesManager.Instance.Colliders[i];
                            //    if (curCollider.TryGetComponent(out interactObject))
                            //    {
                            //        float nowDis = (curCollider.transform.position - RaycastHit.point).sqrMagnitude;
                            //        if (nowDis < minDis)
                            //        {
                            //            selectInteractObj = interactObject;
                            //            selectTrans = curCollider.transform;
                            //            minDis = nowDis;
                            //        }
                            //    }
                            //}
                            //SetInteractObject(selectInteractObj, selectTrans, GV);

                            if(Physics.SphereCast(ray.origin,SphereRadius, ray.direction, out RaycastHit, 100, SphereMask))
                            {
                                Transform nowTarget = RaycastHit.collider.transform;

                                if (TryGetInteractObject(RaycastHit.collider.transform, out interactObject, out Transform sphereInteractObjectTransform))
                                //if (nowTarget.TryGetComponent(out interactObject))
                                {
                                    SetInteractObject(interactObject, sphereInteractObjectTransform, GV);
                                }
                                else
                                {
                                    SetInteractObject(null, null, GV);
                                }
                            }
                            else
                            {
                                SetInteractObject(null, null, GV);
                            }
                        }
                        else
                        {
                            //检测到碰撞体，却未找到交互对象，抛出错误
                            DebugInteractError(RaycastHit.collider.gameObject);
                            SetInteractObject(null, null, GV);
                        }
                    }
                }
            }

            //获取鼠标位置
            if (Physics.Raycast(ray, out RaycastHit, 100, GroundMask))
            {
                GV.mMousePos.SetValue(part, new PointData(RaycastHit.point, Quaternion.identity));
                GV.mIsGround.SetValue(part, true);
            }
            else
            {
                GV.mIsGround.SetValue(part, false);
            }

            if (interactIsValid)
            {
                Quaternion rot = Quaternion.LookRotation(mInteractObject.Point_Dir());
                GV.mInteractPoint.SetValue(part, new PointData(mInteractObject.Point_Pos(), rot));
                GV.mInteractObjPoint.SetValue(part, new PointData(interactTrans.position, interactTrans.rotation));
            }
        }

        private static void DebugInteractError(GameObject targetObj)
        {
            EngineDebug.LogError($"当前交互层配置错误!!，请检查当前对象层级是否正确，或交互组件是否丢失!!" +
                $"\n错误对象 <color=#ffcc00>[{targetObj.name}]</color> [{LayerMask.LayerToName(targetObj.layer)}]");
        }

        private static void SetInteractObject(IInteractObject interactObject, Transform mInteractTrans, InputRayCastGV GV)
        {
            if (mInteractObject != interactObject)
            {
                //设置检测对象
                mInteractObject = interactObject;

                //设置到角色内部缓存
                if(player.ActionStateMachine.TryGetStaticLogic(out Ex_InteractObjState exInteract, nameof(Ex_InteractObjState)))
                {
                    exInteract.UpdateData(interactObject, mInteractTrans);
                }

                interactIsValid = interactObject is not null;
                //Debug.LogWarning($"交互对象是否有效 [{interactIsValid}]  [{interactTrans.gameObject.name}]");
                interactTrans = mInteractTrans;
                GV.mIsCanInteract.SetValue(part, interactIsValid);
                if (interactIsValid)
                {
                    //设置交互类型
                    GV.mSetType.SetValue(part, (byte)interactObject.InteractType());

                    //如果交互对象是Unit，则写入交互单位
                    if (interactObject.InteractType() == EInteractType.Unit)
                    {
                        //EngineDebug.LogError($"<color=#ffcc00>获取的单位[{interactObject.CurUnit().gameObject.name}]");
                        GV.mSetTarget.SetValue(part, interactObject.CurUnit());
                    }
                    else
                    {
                        GV.mSetTarget.SetValue(part, null);
                    }

                    //设置交互对象半径
                    GV.mInteractRange.SetValue(part, interactObject.Point_Radius());

                    //设置交互对象Action
                    GV.mInteractActionID.SetValue(part, interactObject.InteractActionID());

                    //对齐方式
                    GV.mInteractType.SetValue(part, (byte)interactObject.AlignType());

                    //当前选中的对象回调
                    InteractObjChange?.Invoke(interactTrans);
                    //Debug.LogWarning($"<color=#ffcc00>交互对象有效 [{interactIsValid}]  [{interactTrans.gameObject.name}]");
                }
                else
                {
                    GV.mSetTarget.SetValue(part, null);

                    //取消选中对象
                    InteractObjChange?.Invoke(null);
                    //Debug.LogWarning($"<color=#ff0000>交互对象无效");
                }
            }
        }

        private static bool TryGetInteractObject(Transform targetTransform, out IInteractObject interactObject, out Transform interactObjectTransform)
        {
            if (targetTransform.TryGetComponent(out interactObject))
            {
                interactObjectTransform = targetTransform;
                return true;
            }
            var parentBehaviours = targetTransform.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < parentBehaviours.Length; i++)
            {
                if (parentBehaviours[i] is IInteractObject parentInteractObject)
                {
                    interactObject = parentInteractObject;
                    interactObjectTransform = parentBehaviours[i].transform;
                    return true;
                }
            }
            if (targetTransform.TryGetComponent(out TargetUnit targetUnit))
            {
                if (targetUnit.GetUnit().TryGetComponent(out interactObject))
                {
                    interactObjectTransform = targetUnit.GetUnit().transform;
                    return true;
                }
            }
            interactObject = null;
            interactObjectTransform = null;
            return false;
        }
    }


}