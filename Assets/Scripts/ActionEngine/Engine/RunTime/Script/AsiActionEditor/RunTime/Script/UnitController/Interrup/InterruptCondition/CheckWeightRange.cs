using UnityEngine;

namespace AsiActionEngine.RunTime
{
    [System.Serializable]
    public class CheckWeightRange : IInterruptCondition
    {
        public float checkWeight = 0.5f;
        public int InterruptType => -(int)EInterruptTypeInternal.EIT_CheckWeightRange;
        public bool CheckInterrupt(ActionEngine_Unit unit, ActionStatePart actionStatePart)
        {
            float value = Random.Range(0.0f, 1.0f);
            // Debug.Log("随机数: " + value);
            return value <= checkWeight;
        }

        public IInterruptCondition Clone()
        {
            CheckWeightRange clone = new CheckWeightRange();
            clone.checkWeight = this.checkWeight;
            return clone;
        }
    }
}