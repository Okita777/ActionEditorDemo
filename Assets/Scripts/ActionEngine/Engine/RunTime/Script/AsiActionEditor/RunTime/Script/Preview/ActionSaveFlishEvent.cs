using UnityEngine.Events;

namespace AsiActionEngine.RunTime
{
    public class ActionSaveFlishEvent
    {
        // private static ActionSaveFlishEvent _instance;
        // public static ActionSaveFlishEvent Instance;
        public static UnityEvent ActionEvent = new UnityEvent();

        /// <summary>
        /// 永久回调，在一次性监听之前执行，不会被 RemoveAllListeners 清除。
        /// 用于保存前的全量校验（如 LocalParam 索引重建）。
        /// </summary>
        public static System.Action OnPreSave;

        public static void Run()
        {
            OnPreSave?.Invoke();
            ActionEvent?.Invoke();
            ActionEvent?.RemoveAllListeners();
        }
    }
}