namespace AsiActionEngine.RunTime
{
    public interface IInterruptCondition
    {
        int InterruptType { get; }
        string GetInterruptDescription() { return ""; }
        bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart);
        IInterruptCondition Clone();

        /// <summary>
        /// 本条件是否需要每条跳转轨独立的运行时实例。
        /// 默认 false：条件实例与配置数据共享，被同一Action的所有单位复用，禁止写入自身字段。
        /// 在 Enter/Exit/CheckInterrupt 中记录运行态的条件必须返回 true，装载跳转轨时会为其单独 Clone。
        /// </summary>
        bool NeedRuntimeState => false;

        /// <summary>
        /// 跳转轨进入判定窗口时调用一次。_isSingle 为 true 表示单帧轨(本帧判定后即离开窗口)。
        /// 仅常规跳转轨(含跳转组、立即跳转、裁切保护)驱动；受击/命中跳转轨不驱动(其条件实例为共享配置数据)。
        /// </summary>
        void Enter(ActionStatePart actionStatePart, ActionInterrupt interrupt, bool _isSingle) { }

        /// <summary>
        /// 跳转轨处于判定窗口内时每帧调用一次(Enter 当帧不调用)，且先于本帧任何条件判定。
        /// 需要按帧累计时间的条件在此推进状态，把结果缓存给 CheckInterrupt，以保证 CheckInterrupt 无副作用。
        /// </summary>
        void Update(ActionStatePart actionStatePart, ActionInterrupt interrupt) { }

        /// <summary>
        /// 跳转轨离开判定窗口时调用一次，与 Enter 成对。
        /// _isInterrupt 为 true 表示被Action切换或状态机停机打断，false 表示窗口自然结束(含Action循环回绕)。
        /// </summary>
        void Exit(ActionStatePart actionStatePart, ActionInterrupt interrupt, bool _isInterrupt) { }

        void EditorDraw(ActionEngine_Unit unit, CharacterConfig characterConfig, ActionMachineTime _actionTime) { }
    }
}
