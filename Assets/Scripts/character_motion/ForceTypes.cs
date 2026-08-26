using System;
using UnityEngine;

namespace ActionEditor.CharacterMotion
{
    /// <summary>
    /// 力效果类型定义。
    /// </summary>
    public enum ForceType
    {
        Constant = 0,
        Impulse = 1,
        Directional = 2,
        Curve = 3,
    }

    /// <summary>
    /// 力配置数据。用于创建一条可运行的力效果实例。
    /// </summary>
    [Serializable]
    public sealed class ForceConfig
    {
        public ForceType Type = ForceType.Directional;
        public Vector3 Direction = Vector3.forward;
        public float Magnitude = 1f;
        public float Duration = 0f;
        public string Tag = "Skill";
        public AnimationCurve Curve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    }

    /// <summary>
    /// 力实例句柄。用于外部引用并移除指定力效果。
    /// </summary>
    public readonly struct ForceHandle
    {
        public readonly int Id;

        public ForceHandle(int id)
        {
            Id = id;
        }

        public bool IsValid => Id > 0;
    }
}
