
namespace AsiActionEngine.RunTime
{
    public interface IActionEventData
    {
        void Enter(ActionStatePart _actionState, bool _isSingle)
        {
        }
        void Update(ActionStatePart _actionState, ActionMachineTime _actionTime)//deltaTime(ms)
        {
        }
        void LateUpdate(ActionStatePart _actionState, ActionMachineTime _actionTime)//deltaTime(ms)
        {
        }
        void Exit(ActionStatePart _actionState, bool _interruot)//interruot 是否因打断轨退出  false代表事件自然结束
        {
        }
        /// <summary>
        /// 仅在Editor模式下调用，用于在Scence视图中绘制图形，Deltatime始终为零
        /// </summary>
        /// <param name="characterConfig">场景中角色配置的组件</param>
        /// <param name="_actionTime">Deltatime始终为零</param>
        void EditorDraw(CharacterConfig characterConfig, ActionStatePart _actionState, ActionMachineTime _actionTime) { }
        string GetDescription() { return string.Empty; }//跳转成功或者失败的描述
        int GetEvenType();
        IActionEventData Creact();
        IActionEventData Clone(IActionEventData _eventData);
    }

    public struct ActionMachineTime
    {
        /// <summary>
        /// 每帧间隔时间
        /// </summary>
        public float Deltatime;
        /// <summary>
        /// 当前状态机在当前状态下已经度过的时间
        /// </summary>
        public float CurrentTime;
        /// <summary>
        /// 当前事件的触发时间
        /// </summary>
        public int TriggerTime;
        /// <summary>
        /// 当前事件的持续时间
        /// </summary>
        public int Duration;

        public ActionMachineTime(float _deltatime, float _currentTime, int _triggerTime, int _duration)
        {
            Deltatime = _deltatime;
            CurrentTime = _currentTime;
            TriggerTime = _triggerTime;
            Duration = _duration;
        }

        //当前时间是否处于当前事件的时间范围内
        public bool IsInRange
        {
            get
            {
                if (Duration < 0) return CurrentTime >= TriggerTime;
                return CurrentTime >= TriggerTime && CurrentTime <= TriggerTime + Duration;
            }
        }

        //获得当前轨道执行的百分比
        public float GetPercentage()
        {
            float percentage = CurrentTime - TriggerTime;

            if (percentage > 0)
            {
                if (CurrentTime > TriggerTime + Duration)
                {
                    percentage = 1;
                }
                else
                {
                    percentage /= Duration;
                }
            }
            else
            {
                percentage = 0;
            }

            return percentage;
        }
    }
}
