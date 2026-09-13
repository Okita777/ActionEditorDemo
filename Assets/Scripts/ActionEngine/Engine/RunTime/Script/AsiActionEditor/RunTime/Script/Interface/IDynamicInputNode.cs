namespace AsiActionEngine.RunTime.Graph
{
    /// <summary>
    /// 蓝图节点支持动态输入端口数量（用户可增删端口）
    /// </summary>
    public interface IDynamicInputNode
    {
        int DynamicInputCount { get; }
        BluePrint_Value GetDynamicInput(int index);
        void SetDynamicInput(int index, BluePrint_Value value);
        BluePrint_Value CreateDefaultDynamicInput();
        void AddDynamicInput();
        void RemoveDynamicInput(int index);
    }
}
