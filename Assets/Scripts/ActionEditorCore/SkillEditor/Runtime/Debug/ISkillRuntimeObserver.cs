namespace AsiSkillEditor.RunTime
{
    public interface ISkillRuntimeObserver
    {
        void OnSnapshotUpdated(SkillRuntimeSnapshot snapshot);
        void OnTraceEmitted(SkillRuntimeTraceEvent traceEvent);
    }
}
