
namespace AsiActionEngine.RunTime
{

    /// <summary>
    /// 非MonoBehaviour的单例基类
    /// </summary>
    /// <typeparam name="T">子类类型</typeparam>
    public abstract class SingletonBase<T> where T : SingletonBase<T>, new()
    {
        // 单例实例
        private static T _instance = null;

        // // 线程安全锁
        // private static readonly object _lock = new object();


        /// <summary>
        /// 全局访问点
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new T();
                    // lock (_lock)
                    // {
                    //     if (_instance is null)
                    //     {
                    //         _instance = new T();
                    //     }
                    // }
                }

                return _instance;
            }
        }
    }


}