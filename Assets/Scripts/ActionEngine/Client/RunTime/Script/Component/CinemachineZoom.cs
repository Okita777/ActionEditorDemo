using AsiActionEngine.RunTime;
#if Cinemachine
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;
#else
        using Cinemachine;
#endif
#endif
using UnityEngine;

namespace AsiTimeLine.RunTime
{
    public class CinemachineZoom : MonoBehaviour
    {
#if Cinemachine
        public CinemachineVirtualCamera[] cinemachines = new CinemachineVirtualCamera[0];
        public Vector2 zoomRange = Vector2.zero;
        public float zoomSpeed = 2;
        public float default_zoom;

        [HideInInspector][SerializeField] private GValue_SetFloat cam_zoom = new GValue_SetFloat();

        [EditorProperty("将相机缩放写入Gvalue", EditorPropertyType.EEPT_SetGFloat, LabelWidth = 130)]
        public GValue_SetFloat Cam_zoom
        {
            get { return cam_zoom; }
            set { cam_zoom = value; }
        }

        private float zoomDelta;
        private CameraControl mainCam = null;
        private CinemachineFramingTransposer[] transposer = new CinemachineFramingTransposer[0];
        private void Start()
        {
            mainCam = GetComponent<CameraControl>();
            transposer = new CinemachineFramingTransposer[cinemachines.Length];
            for (int i = 0; i < cinemachines.Length; i++)
            {
                transposer[i] = cinemachines[i].GetCinemachineComponent<CinemachineFramingTransposer>();
            }
            // throw new NotImplementedException();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (mainCam.stateMachine == null) return;
#endif

            if (Cursor.visible) return;
            default_zoom -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            default_zoom = Mathf.Clamp(default_zoom, zoomRange.x, zoomRange.y);
            if (default_zoom != zoomDelta)
            {
                foreach (var _cinemachine in transposer)
                {
                    _cinemachine.m_CameraDistance = default_zoom;
                }
                zoomDelta = default_zoom;
                if (cam_zoom.m_IsSet)
                {
                    cam_zoom.Set(mainCam.stateMachine.AllActionStatePart[0], default_zoom);
                }
            }
        }
#endif
    }
}