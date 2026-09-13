namespace AsiActionEngine.RunTime.Bullet
{
    //子弹基础属性
    public interface IBullet_Basic
    {
        //生成ID
        int BulletID { get; set; }

        //寿命
        int bullteLife { get; set; }
    }
}