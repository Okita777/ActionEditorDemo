using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_LightGroup : MonoBehaviour
    {
        public Transform parent;
        private Vector3 m_LocalPosition;
        private Transform m_BorrowedLightsRoot;
        private Transform m_BorrowedLightsOriginalParent;

        private Transform m_OriginalParent;
        private Vector3 m_OriginalLocalPosition;
        private Quaternion m_OriginalLocalRotation;
        private Vector3 m_OriginalLocalScale;
        private Transform m_OriginalParentField;
        private bool m_WasAddedByCaller;
        private bool m_OriginalCaptured;

        public void ResetPosition(Vector3 target)
        {
            parent = null;
            m_LocalPosition = Vector3.zero;
            transform.localPosition = Vector3.zero;
            transform.position = target;
        }

        private void FixedUpdate()
        {
            if (!parent)
            {
                return;
            }

            transform.position = parent.position + m_LocalPosition;
        }

        public void Init(Transform playerRoot, Transform playerLightsRoot, bool wasAddedByCaller)
        {
            if (!playerRoot)
            {
                return;
            }

            if (!m_OriginalCaptured)
            {
                m_OriginalParent = transform.parent;
                m_OriginalLocalPosition = transform.localPosition;
                m_OriginalLocalRotation = transform.localRotation;
                m_OriginalLocalScale = transform.localScale;
                m_OriginalParentField = parent;
                m_WasAddedByCaller = wasAddedByCaller;
                m_OriginalCaptured = true;
            }

            parent = playerRoot;
            m_LocalPosition = transform.localPosition;

            m_BorrowedLightsOriginalParent = playerLightsRoot.parent;
            m_BorrowedLightsRoot = playerLightsRoot;

            playerLightsRoot.SetParent(transform, false);
            transform.SetParent(playerRoot.transform.parent, false);
            transform.position = parent.position + m_LocalPosition;
        }

        /// <summary>
        /// 在 Destroy 之前调用：归还 playerLightsRoot 到原父节点，
        /// 避免 DestroyImmediate 连带销毁场景常驻灯光节点。
        /// </summary>
        public void ReleaseBorrowedLights()
        {
            if (m_BorrowedLightsRoot != null && m_BorrowedLightsOriginalParent != null)
                m_BorrowedLightsRoot.SetParent(m_BorrowedLightsOriginalParent, false);
            m_BorrowedLightsRoot = null;
            m_BorrowedLightsOriginalParent = null;
        }

        /// <summary>
        /// PlayerObject.OnDestroy 调用。
        /// 还原顺序：归还借用灯 → 还原本节点 transform 父子与 local 三件套 → 还原 public parent 字段。
        /// 仅当本组件是调用方运行时 AddComponent 上去的（wasAddedByCaller=true）才 Destroy(this)；
        /// 否则保留组件（视为 prefab 预配资产，不能销毁）。
        /// 保证 unit 回 AE 池时子树拓扑/组件/Transform/字段与首次进入 Init 之前一致。
        /// </summary>
        public void RestoreOriginalAndDispose()
        {
            ReleaseBorrowedLights();

            if (m_OriginalCaptured)
            {
                transform.SetParent(m_OriginalParent, false);
                transform.localPosition = m_OriginalLocalPosition;
                transform.localRotation = m_OriginalLocalRotation;
                transform.localScale = m_OriginalLocalScale;
                parent = m_OriginalParentField;
            }
            else
            {
                parent = null;
            }

            m_LocalPosition = Vector3.zero;

            bool shouldDispose = m_WasAddedByCaller;

            m_OriginalCaptured = false;
            m_OriginalParent = null;
            m_OriginalParentField = null;
            m_WasAddedByCaller = false;

            if (shouldDispose)
            {
                // 必须用 DestroyImmediate 而不是 Destroy：
                // Destroy(this) 是帧末延迟销毁，但 AE 对象池可能在同一帧
                // 把同一个 unit GameObject 复用给复活后的新玩家。
                // 新玩家 InitLightGroup 调用 GetComponent 时仍会拿到这个待销毁的旧组件，
                // 进而走入 wasNewlyAdded=false 分支跳过 AddComponent，
                // 帧末旧组件被真正销毁后骨骼上就什么 ActionEngine_LightGroup 都没有了。
                DestroyImmediate(this);
            }
        }
    }
}
