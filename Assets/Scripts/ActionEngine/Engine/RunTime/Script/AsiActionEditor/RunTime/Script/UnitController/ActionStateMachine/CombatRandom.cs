namespace AsiActionEngine.RunTime
{
    /// <summary>
    /// 战斗随机接口：按 serverTick+unitId 播种，保证 DS/客户端可复现（P5）。
    /// </summary>
    public interface ICombatRandom
    {
        void Reseed(uint serverTick, long unitId);
        float Value { get; }
        int Range(int minInclusive, int maxExclusive);
        float Range(float minInclusive, float maxInclusive);
    }

    /// <summary>确定性 LCG 随机（不依赖 UnityEngine.Random）。</summary>
    public sealed class CombatRandom : ICombatRandom
    {
        private uint _state;

        public void Reseed(uint serverTick, long unitId)
        {
            unchecked
            {
                _state = serverTick * 747796405u + (uint)unitId * 2891336453u + 1u;
                if (_state == 0) _state = 1u;
            }
        }

        public float Value
        {
            get
            {
                Next();
                return (_state & 0xFFFFFF) / (float)0x1000000;
            }
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            Next();
            return minInclusive + (int)(_state % (uint)(maxExclusive - minInclusive));
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            return minInclusive + (maxInclusive - minInclusive) * Value;
        }

        private void Next()
        {
            unchecked
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
            }
        }
    }
}
