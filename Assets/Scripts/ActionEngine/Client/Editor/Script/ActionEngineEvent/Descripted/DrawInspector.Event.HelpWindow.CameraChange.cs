using AsiActionEditor_Ex.RunTime;
using AsiActionEngine.RunTime;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public partial class DrawInspectorHelpWindow
    {
        private static Vector2 m_CamHelpScrollPos;
        private static bool m_CamFoldOverview = true;
        private static bool m_CamFoldProperties = true;
        private static bool m_CamFoldLifecycle = true;
        private static bool m_CamFoldUseCases = true;
        private static bool m_CamFoldNotes = true;

        private static void DrawCameraChange(Event_CameraChange _cameraChange)
        {
            HelpWindowDrawUtility.UpdateAnimTime();

            m_CamHelpScrollPos = EditorGUILayout.BeginScrollView(m_CamHelpScrollPos);

            HelpWindowDrawUtility.DrawTitle("Camera Icon", "相机跳转事件", "Event_CameraChange", new Color(0.4f, 0.85f, 1f));
            DrawCam_Overview();
            DrawCam_Properties(_cameraChange);
            DrawCam_Lifecycle();
            DrawCam_UseCases();
            DrawCam_Notes();

            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        #region CameraChange Sections

        private static void DrawCam_Overview()
        {
            HelpWindowDrawUtility.DrawOverview(ref m_CamFoldOverview,
                "在动作时间轴上控制 Cinemachine 虚拟相机的切换。\n\n" +
                "支持两种触发模式：\n" +
                "  ● 直接触发 — 事件进入时根据 GValue 条件切换相机\n" +
                "  ● 命中触发 — 攻击命中目标时才切换相机\n\n" +
                "可配合 GValue 条件系统实现复杂的相机切换逻辑，并支持 LookAt 目标切换，\n" +
                "实现锁定镜头、特写镜头等效果。");
        }

        private static void DrawCam_Properties(Event_CameraChange _cameraChange)
        {
            m_CamFoldProperties = HelpWindowDrawUtility.DrawSectionHeader("  属性说明", m_CamFoldProperties, new Color(0.8f, 0.6f, 0.1f));
            if (!m_CamFoldProperties) return;

            HelpWindowDrawUtility.DrawPropertyItem(
                "仅当前单位为Player（操作对象）时有效",
                "开启后，此事件仅在当前操作单位是 Player（玩家控制的角色）时生效。\n" +
                "其他 NPC 或技能实体执行到此事件时会被直接跳过。",
                "用于[<color=#ffcc00>过滤非玩家单位</color>]，避免 AI 单位执行相同 Action 时也触发相机切换。",
                _cameraChange.OnlyPlayer ? "当前：已开启" : "当前：已关闭",
                _cameraChange.OnlyPlayer ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.6f, 0.6f, 0.6f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "命中时进入相机",
                "开启后，相机切换不在事件 Enter 时触发，而是注册 OnHit 回调，\n" +
                "等待攻击命中目标时才执行相机切换。\n\n" +
                "• 需要当前 Action 中有 Ex_AttackBox 组件支持\n" +
                "• Enter 时注册回调，Exit 时自动解除\n" +
                "• 命中模式下 Update 阶段不会做额外检测",
                "适用于[<color=#ffcc00>命中瞬间特写</color>]等效果，如处决、暴击镜头。",
                _cameraChange.OnHitter ? "当前：命中触发模式" : "当前：直接触发模式",
                _cameraChange.OnHitter ? new Color(1f, 0.6f, 0.2f) : new Color(0.5f, 0.7f, 1f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "相机进入条件（GValue_Ratio）",
                "基于 GValue 的条件判断系统，用于控制相机切换的前置条件。\n\n" +
                "• 支持多个条件组合（全部满足 / 任一满足）\n" +
                "• 条件列表为空时默认通过\n" +
                "• 支持 GBool, GInt, GFloat, GEnum, GString 等类型比较\n" +
                "• 非命中模式下，Update 阶段持续检测，条件变化时自动切换",
                "例：[<color=#ffcc00>锁定目标</color>]时进入锁定相机（通过 GBool 判断锁定状态）。",
                _cameraChange.Ratio.GValue_RatioPart.Count > 0
                    ? $"当前：已配置 {_cameraChange.Ratio.GValue_RatioPart.Count} 个条件"
                    : "当前：无条件（始终通过）",
                new Color(0.8f, 0.8f, 0.3f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "进入时触发相机",
                "要切换到的 Cinemachine 虚拟相机的数组索引。\n" +
                "对应 CameraControl 组件上 allCinemachine[] 数组中的序号。\n\n" +
                "• 索引 0 通常为默认主相机\n" +
                "• 不同索引对应不同的虚拟相机预设（如特写、俯瞰、肩部视角等）",
                null,
                $"当前值：{_cameraChange.EnterCam}",
                new Color(0.5f, 0.8f, 1f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "将相机绑定点来源设为自身",
                "控制相机 Follow 跟随点的获取来源：\n\n" +
                "• 关闭 → 从 Player（玩家角色）的 CharacterConfig 获取挂点\n" +
                "• 开启 → 从当前执行事件的单位自身获取挂点\n\n" +
                "技能实体（IsTem=true）时，会追溯到源单位的 ActionStateMachine。",
                "当[<color=#ffcc00>技能实体或召唤物</color>]需要以自身为中心做相机特写时开启。",
                _cameraChange.ChangeCamPoint ? "当前：使用自身挂点" : "当前：使用玩家挂点",
                _cameraChange.ChangeCamPoint ? new Color(0.3f, 0.9f, 0.6f) : new Color(0.7f, 0.7f, 0.9f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "进入相机绑定点位",
                "相机 Follow 目标的角色挂点位置 (ECharacteLimbType)。\n" +
                "从绑定点来源的 CharacterConfig.HelpPointDic 中查找对应的 Transform。\n\n" +
                "常用相机挂点：\n" +
                "  Cam_Main  — 主相机点（通常在角色身后上方）\n" +
                "  Cam_Look  — 注视点\n" +
                "  Cam_Ani_A — 动画相机点A\n" +
                "  Cam_Ani_B — 动画相机点B",
                null,
                $"当前：{(ECharacteLimbType)_cameraChange.EnterPoint}",
                new Color(0.6f, 0.8f, 1f));

            HelpWindowDrawUtility.DrawPropertyItem(
                "切换LookAt",
                "开启后，切换相机时将 LookAt 指向指定目标单位的挂点。\n\n" +
                "• 命中模式 → LookAt 指向被命中单位的[<color=#ffcc00>目标挂点</color>]\n" +
                "• 直接模式 → LookAt 指向 GUnit 引用单位的[<color=#ffcc00>目标挂点</color>]\n" +
                "• 如果目标单位没有对应挂点，会回退到目标 Transform",
                "实现[<color=#ffcc00>看向目标</color>]效果，如锁定视角、对话镜头。",
                _cameraChange.SwitchLookAt ? "当前：已开启 LookAt 切换" : "当前：LookAt 跟随 Follow 点",
                _cameraChange.SwitchLookAt ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.6f, 0.6f, 0.6f));

            if (_cameraChange.SwitchLookAt)
            {
                HelpWindowDrawUtility.DrawPropertyItem(
                    "    目标挂点",
                    "LookAt 目标单位上的挂点位置。\n" +
                    "从目标单位的 CharacterConfig.HelpPointDic 中查找对应的 Transform。",
                    null,
                    $"当前：{_cameraChange.TargetPoint}",
                    new Color(0.6f, 0.8f, 1f));
            }

            HelpWindowDrawUtility.DrawPropertyItem(
                "退出时回到默认相机",
                "开启后，事件退出（Exit）时自动切换回默认相机配置：\n" +
                "  相机索引 → 0\n" +
                "  跟随挂点 → Player 的 Cam_Main\n\n" +
                "• 仅在事件期间确实切换过相机（isEnterCam=true）时才执行恢复\n" +
                "• 被打断退出时也会恢复",
                "多数情况应[<color=#ffcc00>保持开启</color>]，除非后续有其他相机事件接管。",
                _cameraChange.ExitCam ? "当前：退出时恢复" : "当前：退出时保持",
                _cameraChange.ExitCam ? new Color(0.3f, 0.9f, 0.3f) : new Color(1f, 0.6f, 0.3f));

            GUILayout.Space(6);
        }

        private static void DrawCam_Lifecycle()
        {
            m_CamFoldLifecycle = HelpWindowDrawUtility.DrawSectionHeader("  执行流程", m_CamFoldLifecycle, new Color(0.3f, 0.7f, 0.4f));
            if (!m_CamFoldLifecycle) return;

            float flowAlpha = 0.7f + Mathf.PingPong(HelpWindowDrawUtility.AnimTime * 0.8f, 0.3f);

            HelpWindowDrawUtility.DrawFlowPhase("Enter（进入）", new Color(0.3f, 0.8f, 0.4f, flowAlpha), new[]
            {
                "① 检查 [仅Player有效] → 非玩家操作对象则直接跳过",
                "② 检查 [切换LookAt] 时 GUnit 是否有效",
                "③ 分支判断：",
                "   ├─ [命中时进入相机] = 开 → 注册 OnHit 回调，等待命中事件",
                "   └─ [命中时进入相机] = 关 → 检查 [相机进入条件]",
                "       └─ 条件满足 → 执行相机切换"
            });

            HelpWindowDrawUtility.DrawFlowArrow();

            HelpWindowDrawUtility.DrawFlowPhase("Update（持续更新）", new Color(0.8f, 0.7f, 0.2f, flowAlpha), new[]
            {
                "● 仅 [命中时进入相机] = 关 时工作",
                "● 每帧检测 [相机进入条件] 状态",
                "● 检测条件状态变化：false → true 时切换相机",
                "● 已切换的相机不会重复切换"
            });

            HelpWindowDrawUtility.DrawFlowArrow();

            HelpWindowDrawUtility.DrawFlowPhase("Exit（退出）", new Color(0.8f, 0.3f, 0.3f, flowAlpha), new[]
            {
                "① [命中时进入相机] 模式 → 移除 OnHit 事件回调",
                "② 检查 [退出时回到默认相机] 且已切换过相机：",
                "   └─ 切换回默认相机（索引 0，Player 的 Cam_Main 挂点）"
            });

            GUILayout.Space(6);
        }

        private static void DrawCam_UseCases()
        {
            m_CamFoldUseCases = HelpWindowDrawUtility.DrawSectionHeader("  使用场景", m_CamFoldUseCases, new Color(0.7f, 0.4f, 0.8f));
            if (!m_CamFoldUseCases) return;

            HelpWindowDrawUtility.DrawUseCaseItem(
                "d_SceneViewCamera",
                "技能特写",
                "释放技能时切换到特写相机，技能结束后自动恢复默认。\n" +
                "配置：<color=#ffcc00>命中时进入相机</color>=关, <color=#ffcc00>进入时触发相机</color>=特写相机索引, <color=#ffcc00>退出时回到默认相机</color>=开");

            HelpWindowDrawUtility.DrawUseCaseItem(
                "AnimationClip Icon",
                "命中镜头",
                "攻击命中瞬间切换到击杀/特效镜头。\n" +
                "配置：<color=#ffcc00>命中时进入相机</color>=开, <color=#ffcc00>切换LookAt</color>=开, <color=#ffcc00>目标挂点</color>=对应位置");

            HelpWindowDrawUtility.DrawUseCaseItem(
                "d_FilterByType",
                "条件相机",
                "配合 GValue 条件，在特定状态下自动切换。例如[<color=#ffcc00>锁定目标</color>]后进入锁定相机。\n" +
                "配置：<color=#ffcc00>命中时进入相机</color>=关, <color=#ffcc00>相机进入条件</color> 配置 GBool 条件");

            HelpWindowDrawUtility.DrawUseCaseItem(
                "d_RotateTool",
                "锁定注视",
                "相机跟随自身但 LookAt 指向目标，实现[<color=#ffcc00>看向目标</color>]的锁定效果。\n" +
                "配置：<color=#ffcc00>切换LookAt</color>=开, GUnit=目标, <color=#ffcc00>目标挂点</color>=Head/Chest");

            GUILayout.Space(6);
        }

        private static void DrawCam_Notes()
        {
            m_CamFoldNotes = HelpWindowDrawUtility.DrawSectionHeader("  注意事项", m_CamFoldNotes, new Color(0.9f, 0.35f, 0.3f));
            if (!m_CamFoldNotes) return;

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Warning,
                "[进入时触发相机] 索引必须在 CameraControl.allCinemachine 数组范围内，越界会导致空引用。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Warning,
                "[命中时进入相机] 模式需要当前 Action 中存在 Ex_AttackBox 组件，否则无法注册命中回调。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Info,
                "[切换LookAt] 需要目标单位有有效的 CharacterConfig 组件，且 HelpPointDic 中包含对应挂点。\n" +
                "如果挂点不存在，会回退到目标 Transform 作为 LookAt。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Info,
                "[退出时回到默认相机] 恢复的默认相机固定为：相机索引 0 + Player 的 Cam_Main 挂点。\n" +
                "如果需要切换到其他相机，应关闭此选项并使用后续事件接管。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Info,
                "非命中模式下（[命中时进入相机]=关），Update 阶段会持续检测 [相机进入条件]。\n" +
                "条件从 false → true 时会触发相机切换，适合做状态驱动的相机逻辑。");

            HelpWindowDrawUtility.DrawNoteItem(MessageType.Warning,
                "事件回调必须成对注册/移除。OnHit 回调在 Enter 注册、Exit 移除，\n" +
                "中途被打断退出也能正确清理（Exit 中有 isCallBack 判断）。");

            GUILayout.Space(6);
        }

        #endregion
    }
}
