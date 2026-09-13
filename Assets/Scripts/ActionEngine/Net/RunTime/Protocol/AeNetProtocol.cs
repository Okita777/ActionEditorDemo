using System;
using AsiActionEngine.RunTime;
using FishNet.Broadcast;
using UnityEngine;

namespace AsiTimeLine.Net
{
    /// <summary>
    /// 联网单位在玩法上的角色。决定 <see cref="AeNetUnit"/> 各生命周期分支的建体形态。
    /// </summary>
    public enum EAeNetUnitRole : byte
    {
        /// <summary>玩家：服务端建 headless 单位跑权威逻辑，owner 客户端另建本地预测体。</summary>
        Player = 0,

        /// <summary>非玩家：服务端建 headless 单位跑逻辑，客户端建 RemoteProxy 表现体。</summary>
        Npc = 1,
    }

    /// <summary>
    /// 一次输入边沿。<see cref="Payload"/> 的含义随 <see cref="Kind"/> 变化，见 <see cref="EActionInputKind"/>。
    ///
    /// 传按键名而非动作 ID：服务端必须重跑打断系统来判定这次输入在当前状态下是否合法，
    /// 直接收动作 ID 等于让客户端自己宣告结果，权威就名存实亡。
    /// </summary>
    public struct AeNetInputEvent
    {
        public EActionInputKind Kind;
        public string KeyName;
        public int Payload;
    }

    /// <summary>
    /// owner 客户端一个 tick 的完整操作意图。
    ///
    /// 移动与视角是每 tick 的状态量，按键是稀疏边沿量，两者语义不同必须分开表达：
    /// 只传"当前按住哪些键"会丢掉同一 tick 内的按下-抬起序列，连招与长按判定随之失真。
    /// </summary>
    public struct AeNetIntentData
    {
        /// <summary>采集时的 <c>TimeManager.LocalTick</c>，服务端据此排序与去重。</summary>
        public uint ClientTick;

        /// <summary>本 tick 是否有方向输入。为 false 时服务端注入 <c>SetMoveInputStop</c>。</summary>
        public bool HasMoveInput;

        public Vector3 MoveDir;
        public Vector3 MoveDirCam;

        /// <summary>视角朝向（<c>SetMouseXY</c>）。</summary>
        public Quaternion MouseRot;

        /// <summary>
        /// 相机世界朝向（<c>GetCharacterFor</c>）。<c>ERotType.Camera</c> 的转身目标直接取它的 yaw，
        /// 缺了它服务端单位会恒定朝向 identity，广播出去的角度全错。
        /// </summary>
        public Quaternion CharacterFor;

        /// <summary>相机水平朝向（<c>SetCamRot</c>）。<c>ERotType.MoveDir</c> 用它把输入方向转到相机空间。</summary>
        public Quaternion CamRot;

        /// <summary>本 tick 内发生的输入边沿，按发生顺序排列。无输入时为 null。</summary>
        public AeNetInputEvent[] KeyEvents;
    }

    /// <summary>
    /// 一次 <c>ActionStateMachine.ChangeAction</c> 的网络载荷。
    ///
    /// <see cref="AnimaLayer"/> 用 int 而非 byte：引擎侧 <c>ActionState.AnimaLayer</c> 是 int，
    /// 收窄类型会让层号越界时静默截断到错误层，远端播错动作且难以排查。
    /// </summary>
    public struct AeNetActionData : IEquatable<AeNetActionData>
    {
        public int ActionId;
        public int MixTime;
        public int OffsetTime;
        public int AnimaLayer;

        public bool Equals(AeNetActionData other)
        {
            return ActionId == other.ActionId
                && MixTime == other.MixTime
                && OffsetTime == other.OffsetTime
                && AnimaLayer == other.AnimaLayer;
        }

        public override bool Equals(object obj)
        {
            return obj is AeNetActionData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ActionId;
                hash = (hash * 397) ^ MixTime;
                hash = (hash * 397) ^ OffsetTime;
                hash = (hash * 397) ^ AnimaLayer;
                return hash;
            }
        }
    }

    /// <summary>
    /// 单位建体所需的最小信息。服务端在 <c>ServerManager.Spawn</c> 之前写入 SyncVar，随 spawn 下发。
    /// </summary>
    public struct AeNetUnitSpawnData : IEquatable<AeNetUnitSpawnData>
    {
        public int UnitWarpId;
        public EAeNetUnitRole Role;

        /// <summary>
        /// 本单位的模拟权威是否在服务端。随 spawn 下发而不是各端读本地配置：
        /// 两端对权威归属的理解一旦不一致，位姿就会出现两个写入者互相覆盖。
        /// NPC 恒为 true；玩家取 <c>AeNetBootstrap</c> 的开关。
        /// </summary>
        public bool ServerAuthoritative;

        public bool Equals(AeNetUnitSpawnData other)
        {
            return UnitWarpId == other.UnitWarpId
                && Role == other.Role
                && ServerAuthoritative == other.ServerAuthoritative;
        }

        public override bool Equals(object obj)
        {
            return obj is AeNetUnitSpawnData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = UnitWarpId;
                hash = (hash * 397) ^ (int)Role;
                hash = (hash * 397) ^ (ServerAuthoritative ? 1 : 0);
                return hash;
            }
        }
    }

    /// <summary>
    /// 客户端连上后请求服务端为自己生成玩家单位。
    /// 预览环境没有中心服下发角色数据，<see cref="UnitWarpId"/> 由客户端 Inspector 配置。
    /// </summary>
    public struct AeNetJoinRequest : IBroadcast
    {
        public int UnitWarpId;
    }

    /// <summary>
    /// 一个标量 GV 槽位的新值。
    ///
    /// 四种标量类型共用一个结构而不是各发各的：GV 变更是零散且低频的，
    /// 按类型拆包会让每 tick 的批次退化成四个几乎为空的数组。
    /// <see cref="IntVal"/> 承载 GInt 原值、GBool 的 0/1、GEnum 的 byte，只有 GFloat 走 <see cref="FloatVal"/>。
    /// </summary>
    public struct AeNetGValueScalar
    {
        /// <summary><see cref="EGValueType"/> 的字节形式。</summary>
        public byte Type;

        public ushort Group;
        public ushort Id;

        public int IntVal;
        public float FloatVal;
    }

    /// <summary>
    /// 一个数组 GV 槽位的新值。三个数组字段中只有与 <see cref="Type"/> 对应的那个非空。
    /// </summary>
    public struct AeNetGValueArray
    {
        public byte Type;
        public ushort Group;
        public ushort Id;

        public int[] IntArray;
        public float[] FloatArray;
        public bool[] BoolArray;
    }

    /// <summary>
    /// 一次 GV 同步的完整载荷，可以是一个 tick 的增量，也可以是建体后的全量快照。
    /// 两者形状相同：接收端对快照和增量的应用逻辑没有区别，区别只在快照会打开接收闸门。
    /// </summary>
    public struct AeNetGValueBatch
    {
        public AeNetGValueScalar[] Scalars;
        public AeNetGValueArray[] Arrays;
    }
}
