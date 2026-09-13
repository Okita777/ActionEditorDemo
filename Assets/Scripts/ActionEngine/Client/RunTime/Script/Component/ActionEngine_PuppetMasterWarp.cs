using UnityEngine;
using AsiActionEngine.RunTime.Bullet;


#if PuppetMaster
using RootMotion.Dynamics;
#endif
namespace AsiTimeLine.RunTime
{
    public class ActionEngine_PuppetMasterWarp : MonoBehaviour
    {
#if PuppetMaster
        [Header("指定具体布娃娃组件")] public PuppetMaster PuppetMaster;
        public void SetOn()
        {
            PuppetMaster.state = PuppetMaster.State.Alive;
        }

        public void SetOff()
        {
            PuppetMaster.state = PuppetMaster.State.Frozen;
        }

        /// <summary>
        /// 同步 PuppetMaster 内部 muscle 状态到当前 Transform 位置。
        /// Mode.Disabled 时立即生效；Active 时延迟到下一次 Read()。
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            PuppetMaster.Teleport(position, rotation, true);
        }
#else
        public void SetOn(){}
        public void SetOff(){}
        public void Teleport(Vector3 position, Quaternion rotation){}
#endif
    }

}