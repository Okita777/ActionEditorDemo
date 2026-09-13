namespace AsiTimeLine.RunTime
{
    public enum EMoveDirType
    {
        Transform,
        Camera,
        World,
        inputDir_Cam,
    }

    public enum EContrast
    {
        Greater,
        Less,
        Equals,
        GreaterOrEquals,
        LessOrEquals
    }

    public enum EAnimFloatFor
    {
        //相机相对单位的角度差
        CamOffsetToUnit,
        //输入方向和实际朝向的角度差
        InputToTransDir,
        //地面朝向
        GroundDir
    }

    public enum ERotType
    {
        Camera,
        MoveDir,
        LockToTargetDir,
        LookToAttacker,
        LookToPlayer
    }

    public enum EOnMoveDirType
    {
        MoveDir,
        Transform,
        Camera,
        World,
    }

    public enum EPriority
    {
        Height,
        Normal,
        Lower
    }

    public enum EInteraetPointType
    {
        LockTarget,
        HookLock,
        InteractPoint,
    }
}