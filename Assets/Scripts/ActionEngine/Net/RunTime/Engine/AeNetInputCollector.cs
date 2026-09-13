using System.Collections.Generic;
using AsiActionEngine.RunTime;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 把 owner 本地预测单位的输入采集成可上行的意图。
    ///
    /// 只能挂在本地预测单位上。服务端 headless 单位被注入意图时同样会触发
    /// <see cref="ActionStateMachine.InputKeyObserved"/>，若误挂会把注入结果当成新输入再次上行，形成回环。
    /// </summary>
    public sealed class AeNetInputCollector
    {
        private readonly List<AeNetInputEvent> mPending = new List<AeNetInputEvent>(8);
        private ActionStateMachine mMachine;

        public bool IsAttached => mMachine != null;

        public void Attach(ActionEngine_Unit unit)
        {
            ActionStateMachine machine = unit?.ActionStateMachine;
            if (machine == null) return;

            Detach();
            mMachine = machine;
            mMachine.InputKeyObserved += OnInputKeyObserved;
        }

        public void Detach()
        {
            if (mMachine == null) return;

            mMachine.InputKeyObserved -= OnInputKeyObserved;
            mMachine = null;
            mPending.Clear();
        }

        private void OnInputKeyObserved(EActionInputKind kind, string keyName, int payload)
        {
            mPending.Add(new AeNetInputEvent
            {
                Kind = kind,
                KeyName = keyName,
                Payload = payload,
            });
        }

        /// <summary>
        /// 打包本 tick 意图并清空已累积的边沿。
        ///
        /// 边沿跨帧累积而非按帧取样：渲染帧率高于 tick 率时，同一 tick 内会落入多帧的按键，
        /// 只取最后一帧会丢掉中间的按下-抬起序列。
        /// </summary>
        public AeNetIntentData Collect(uint clientTick)
        {
            AeNetIntentData intent = new AeNetIntentData
            {
                ClientTick = clientTick,
            };

            if (mMachine != null)
            {
                intent.HasMoveInput = mMachine.IsMoveInput;
                intent.MoveDir = mMachine.PlayerInputMoveDir;
                intent.MoveDirCam = mMachine.PlayerInputMoveDir_Cam;
                // 三个朝向源都要带上：转身事件按 ERotType 分别取用，少任何一个都会让服务端转到错误角度
                intent.MouseRot = mMachine.GetCamPointRot();
                intent.CharacterFor = mMachine.GetCharacterFor;
                intent.CamRot = mMachine.GetCamRot();
            }

            if (mPending.Count > 0)
            {
                intent.KeyEvents = mPending.ToArray();
                mPending.Clear();
            }

            return intent;
        }
    }
}
