using System.Collections.Generic;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 单个单位的 GV 同步状态：收集本端写入、打包成批次、应用远端批次。
    ///
    /// 与位姿、动作同步一样按单位划分实例，而不是做成全局管理器——GV 属于单位状态机，
    /// 权威归属、生命周期、观察者范围都跟着 <see cref="AeNetUnit"/> 走。
    ///
    /// 收集与应用是两个独立能力：服务端权威单位在服务端收集、在客户端应用，
    /// 客户端权威单位则相反，因此 <see cref="Attach"/> 用 collect 参数区分，
    /// 只有需要产出的那一侧才订阅池的变更。
    /// </summary>
    public sealed class AeNetGValueSync
    {
        private static readonly AeNetGValueScalar[] c_EmptyScalars = new AeNetGValueScalar[0];
        private static readonly AeNetGValueArray[] c_EmptyArrays = new AeNetGValueArray[0];

        private readonly HashSet<(EGValueType, ushort, ushort)> mDirty =
            new HashSet<(EGValueType, ushort, ushort)>();

        private readonly List<AeNetGValueScalar> mScalarBuffer = new List<AeNetGValueScalar>();
        private readonly List<AeNetGValueArray> mArrayBuffer = new List<AeNetGValueArray>();

        private ActionStateMachine mMachine;
        private GValuePool mPool;
        private bool mCollecting;

        /// <summary>正在写入远端值。收集回调据此跳过，否则"收到即重发"会在两端之间来回弹。</summary>
        private bool mApplying;

        public bool IsAttached => mPool != null;

        /// <summary>
        /// 绑定到一个单位的状态机。<paramref name="collect"/> 为 true 时订阅池变更，
        /// 本端成为该单位 GV 的产出方。
        /// </summary>
        public void Attach(ActionEngine_Unit unit, bool collect)
        {
            Detach();

            ActionStateMachine machine = unit?.ActionStateMachine;
            if (machine?.GValuePool == null) return;

            mMachine = machine;
            mPool = machine.GValuePool;
            mCollecting = collect;

            if (collect) mPool.OnValueChanged += OnPoolValueChanged;
        }

        public void Detach()
        {
            if (mPool != null && mCollecting) mPool.OnValueChanged -= OnPoolValueChanged;

            mMachine = null;
            mPool = null;
            mCollecting = false;
            mApplying = false;
            mDirty.Clear();
        }

        private void OnPoolValueChanged(EGValueType type, ushort group, ushort id)
        {
            if (mApplying) return;
            if (!GValuePool.IsSyncableType(type)) return;
            if (!mPool.IsNetSync(type, group, id)) return;

            // 只记键不记值：同一 tick 内同一槽位写多次时，发出去的应当是 flush 那一刻的终值
            mDirty.Add((type, group, id));
        }

        /// <summary>取走自上次调用以来的变更。无变更时返回 false，调用方据此跳过整个 RPC。</summary>
        public bool TryTakeDelta(out AeNetGValueBatch batch)
        {
            batch = default;
            if (mPool == null || mDirty.Count == 0) return false;

            mScalarBuffer.Clear();
            mArrayBuffer.Clear();

            foreach ((EGValueType type, ushort group, ushort id) in mDirty)
            {
                Pack(type, group, id);
            }
            mDirty.Clear();

            return TryFlushBuffers(out batch);
        }

        /// <summary>
        /// 打包池中全部可同步条目，供中途加入的观察者补齐。
        /// 顺带清空脏集合：快照已经覆盖了它们，再发一次增量是纯粹的重复。
        /// </summary>
        public bool TryBuildSnapshot(out AeNetGValueBatch batch)
        {
            batch = default;
            if (mPool == null) return false;

            mScalarBuffer.Clear();
            mArrayBuffer.Clear();
            mPool.VisitSyncableEntries(Pack);
            mDirty.Clear();

            return TryFlushBuffers(out batch);
        }

        public void ApplyBatch(in AeNetGValueBatch batch)
        {
            if (mPool == null) return;

            mApplying = true;
            try
            {
                AeNetGValueScalar[] scalars = batch.Scalars;
                if (scalars != null)
                {
                    for (int i = 0; i < scalars.Length; i++) ApplyScalar(scalars[i]);
                }

                AeNetGValueArray[] arrays = batch.Arrays;
                if (arrays != null)
                {
                    for (int i = 0; i < arrays.Length; i++) ApplyArray(arrays[i]);
                }
            }
            finally
            {
                mApplying = false;
            }
        }

        private bool TryFlushBuffers(out AeNetGValueBatch batch)
        {
            if (mScalarBuffer.Count == 0 && mArrayBuffer.Count == 0)
            {
                batch = default;
                return false;
            }

            batch = new AeNetGValueBatch
            {
                Scalars = mScalarBuffer.Count > 0 ? mScalarBuffer.ToArray() : c_EmptyScalars,
                Arrays = mArrayBuffer.Count > 0 ? mArrayBuffer.ToArray() : c_EmptyArrays,
            };
            return true;
        }

        /// <summary>
        /// 读取一个槽位的当前值并塞进缓冲。这里再查一次同步标记是给快照路径用的：
        /// 增量路径在收集时已经过滤，快照路径遍历的是整个池。
        /// </summary>
        private void Pack(EGValueType type, ushort group, ushort id)
        {
            if (!mPool.IsNetSync(type, group, id)) return;

            switch (type)
            {
                case EGValueType.GInt:
                    AddScalar(type, group, id, mPool.GetInt(group, id), 0f);
                    break;

                case EGValueType.GFloat:
                    AddScalar(type, group, id, 0, mPool.GetFloat(group, id));
                    break;

                case EGValueType.GBool:
                    AddScalar(type, group, id, mPool.GetBool(group, id) ? 1 : 0, 0f);
                    break;

                case EGValueType.GEnum:
                    AddScalar(type, group, id, mPool.GetEnum(group, id), 0f);
                    break;

                case EGValueType.GGroupInt:
                    mArrayBuffer.Add(new AeNetGValueArray
                    {
                        Type = (byte)type,
                        Group = group,
                        Id = id,
                        IntArray = mPool.GetGroupInt(group, id),
                    });
                    break;

                case EGValueType.GGroupFloat:
                    mArrayBuffer.Add(new AeNetGValueArray
                    {
                        Type = (byte)type,
                        Group = group,
                        Id = id,
                        FloatArray = mPool.GetGroupFloat(group, id),
                    });
                    break;

                case EGValueType.GGroupBool:
                    mArrayBuffer.Add(new AeNetGValueArray
                    {
                        Type = (byte)type,
                        Group = group,
                        Id = id,
                        BoolArray = mPool.GetGroupBool(group, id),
                    });
                    break;
            }
        }

        private void AddScalar(EGValueType type, ushort group, ushort id, int intVal, float floatVal)
        {
            mScalarBuffer.Add(new AeNetGValueScalar
            {
                Type = (byte)type,
                Group = group,
                Id = id,
                IntVal = intVal,
                FloatVal = floatVal,
            });
        }

        /// <summary>
        /// 写入远端标量值，并补发状态机层面的变更通知。
        ///
        /// 走池而不走 <c>GInt.SetValue</c> 是因为远端只传了 (类型, 组, 序号)，没有 GValue 对象可用；
        /// 代价是绕过了 <c>SendChangeMessage_*</c>，所以这里手动补一次——否则接收端挂在
        /// GV 变更上的监听轨道（Event_OnGIntChanged 等）会对同步来的值毫无反应。
        /// </summary>
        private void ApplyScalar(in AeNetGValueScalar item)
        {
            ushort group = item.Group;
            ushort id = item.Id;

            switch ((EGValueType)item.Type)
            {
                case EGValueType.GInt:
                {
                    int old = mPool.GetInt(group, id);
                    mPool.SetInt(group, id, item.IntVal);
                    mMachine?.SendChangeMessage_GInt(group, id, old, item.IntVal);
                    break;
                }

                case EGValueType.GFloat:
                {
                    float old = mPool.GetFloat(group, id);
                    mPool.SetFloat(group, id, item.FloatVal);
                    mMachine?.SendChangeMessage_GFloat(group, id, old, item.FloatVal);
                    break;
                }

                case EGValueType.GBool:
                {
                    bool value = item.IntVal != 0;
                    bool old = mPool.GetBool(group, id);
                    mPool.SetBool(group, id, value);
                    mMachine?.SendChangeMessage_GBool(group, id, old, value);
                    break;
                }

                case EGValueType.GEnum:
                {
                    byte value = (byte)item.IntVal;
                    byte old = mPool.GetEnum(group, id);
                    mPool.SetEnum(group, id, value);
                    mMachine?.SendChangeMessage_GEnum(group, id, old, value);
                    break;
                }
            }
        }

        /// <summary>
        /// 写入远端数组值。必须拷贝：Host 进程内服务端与客户端是同一份托管堆，
        /// FishNet 对 clientHost 的下行不经过序列化，直接存引用会让两端的池共用一个数组，
        /// 之后任一端原地改元素都会静默污染另一端。
        /// </summary>
        private void ApplyArray(in AeNetGValueArray item)
        {
            switch ((EGValueType)item.Type)
            {
                case EGValueType.GGroupInt:
                    if (item.IntArray == null) return;
                    mPool.SetGroupInt(item.Group, item.Id, Copy(item.IntArray));
                    break;

                case EGValueType.GGroupFloat:
                    if (item.FloatArray == null) return;
                    mPool.SetGroupFloat(item.Group, item.Id, Copy(item.FloatArray));
                    break;

                case EGValueType.GGroupBool:
                    if (item.BoolArray == null) return;
                    mPool.SetGroupBool(item.Group, item.Id, Copy(item.BoolArray));
                    break;
            }
        }

        private static T[] Copy<T>(T[] source)
        {
            T[] result = new T[source.Length];
            System.Array.Copy(source, result, source.Length);
            return result;
        }
    }
}
