namespace AsiActionEngine.RunTime
{
    public partial class MotionEngineConst
    {
        public const int MaxEventNumber = 100;//最大执行事件数量
        public const int MaxInteruptNumber = 200;//最大打断轨数量
        public const int ActionPoolMaxSize = 20;//可继承的Action最大数量
        public const int EventPooklMaxSize = 5;//同一类型中可继承的最大事件数量

        public const int BluePrint_MaxListNumber = 512;//蓝图内同类型列表最大数量
        public const int BluePrint_ListMaxCount = 256;//蓝图内列表最大长度

        public const int TimeDoubling = 1000; //时间倍化器

        public const float TimeDoubling_F = 0.001f;

        public static readonly string NondKeyName = "Nond";//输入未空时默认字符

        public static readonly string UnitSaveName = "Unit";//Unit列表保存到Json的名称

        public static readonly string[] ComparisonNames = new[] {
            ">","<","=",">=","<="
        };
        public static string[] AxisNames = new[] {
            "X","Y","Z","-X","-Y","-Z"
        };
    }
}