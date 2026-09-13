using System.Collections.Generic;
using AsiActionEngine.RunTime;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 服务端侧的玩家意图缓冲与注入。
    ///
    /// 注入一律走按键链路（<c>SetKeyDown</c> / <c>SendKeyDown</c>）而非直接 <c>ChangeAction</c>：
    /// 后者跳过打断条件校验，客户端报什么动作服务端就播什么，权威只剩形式。
    /// 走按键则由服务端自己的打断系统判定该输入在当前状态下是否成立。
    /// </summary>
    public sealed class AeNetIntentQueue
    {
        /// <summary>积压上限。超过后一次多消费若干条追赶，否则客户端领先量会永久转成操作延迟。</summary>
        private const int MaxBacklog = 6;

        /// <summary>追赶后保留的缓冲深度，用于吸收抖动。</summary>
        private const int TargetBacklog = 2;

        private readonly Queue<AeNetIntentData> mQueue = new Queue<AeNetIntentData>();
        private uint mLastConsumedTick;
        private bool mHasLastIntent;
        private AeNetIntentData mLastIntent;

        public int Backlog => mQueue.Count;

        /// <summary>已消费到的客户端 tick。owner 拿它从本地预测历史里取出同一时刻的位置做对账。</summary>
        public uint LastConsumedTick => mLastConsumedTick;

        /// <summary>是否已消费过至少一条意图。未消费时 <see cref="LastConsumedTick"/> 无意义。</summary>
        public bool HasConsumed => mHasLastIntent;

        public void Enqueue(in AeNetIntentData intent)
        {
            // 与已消费的 tick 重叠说明是重发或迟到，再注入会让按键边沿重复触发
            if (mHasLastIntent && intent.ClientTick <= mLastConsumedTick) return;

            mQueue.Enqueue(intent);
        }

        public void Clear()
        {
            mQueue.Clear();
            mLastConsumedTick = 0;
            mHasLastIntent = false;
            mLastIntent = default;
        }

        /// <summary>
        /// 推进一个服务端 tick。缓冲为空时沿用上一条意图的移动与视角，但不重放按键边沿
        /// （重放会让一次攻击键在丢包期间连续触发）。
        /// </summary>
        public void ApplyTick(ActionStateMachine machine)
        {
            if (machine == null) return;

            if (mQueue.Count < 1)
            {
                if (mHasLastIntent) ApplyMotionOnly(machine, mLastIntent);
                return;
            }

            int consume = 1;
            if (mQueue.Count > MaxBacklog)
            {
                consume = mQueue.Count - TargetBacklog;
            }

            for (int i = 0; i < consume; i++)
            {
                AeNetIntentData intent = mQueue.Dequeue();
                Apply(machine, intent);
                mLastConsumedTick = intent.ClientTick;
                mLastIntent = intent;
                mHasLastIntent = true;
            }
        }

        private static void ApplyMotionOnly(ActionStateMachine machine, in AeNetIntentData intent)
        {
            if (intent.HasMoveInput)
            {
                machine.SetMoveInput(intent.MoveDir, intent.MoveDirCam);
            }
            else
            {
                machine.SetMoveInputStop();
            }
            machine.SetMouseXY(intent.MouseRot);
            machine.GetCharacterFor = intent.CharacterFor;
            machine.SetCamRot(intent.CamRot);
        }

        private static void Apply(ActionStateMachine machine, in AeNetIntentData intent)
        {
            // 先落移动与视角再放按键：打断条件与技能朝向会读取当前输入方向
            ApplyMotionOnly(machine, intent);

            AeNetInputEvent[] events = intent.KeyEvents;
            if (events == null) return;

            for (int i = 0; i < events.Length; i++)
            {
                AeNetInputEvent keyEvent = events[i];
                if (string.IsNullOrEmpty(keyEvent.KeyName)) continue;

                switch (keyEvent.Kind)
                {
                    case EActionInputKind.KeyDown:
                        machine.SetKeyDown(keyEvent.KeyName, keyEvent.Payload);
                        break;
                    case EActionInputKind.KeyUp:
                        machine.SetKeyUp(keyEvent.KeyName);
                        break;
                    case EActionInputKind.SendKey:
                        machine.SendKeyDown(keyEvent.KeyName, keyEvent.Payload);
                        break;
                }
            }
        }
    }
}
