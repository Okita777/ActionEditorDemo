namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 输入注入到状态机的方式，用于让观察者无损重放一次输入。
    ///
    /// 三者语义不同且不可互相替代：<see cref="KeyDown"/> 走预输入与打断检测，
    /// <see cref="SendKey"/> 直接写当帧输入槽位（含点击 / 长按等已判定形态）。
    /// </summary>
    public enum EActionInputKind
    {
        /// <summary>对应 <c>SetKeyDown</c>，负载为 ActionGroupID。</summary>
        KeyDown = 0,

        /// <summary>对应 <c>SetKeyUp</c>，无负载。</summary>
        KeyUp = 1,

        /// <summary>对应 <c>SendKeyDown</c>，负载为 InputType（0 按下 / 1 抬起 / 2 点击 / 3 长按）。</summary>
        SendKey = 2,
    }
}
