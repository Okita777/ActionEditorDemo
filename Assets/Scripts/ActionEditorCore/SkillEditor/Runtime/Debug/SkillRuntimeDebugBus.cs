namespace AsiSkillEditor.RunTime
{
    public static class SkillRuntimeDebugBus
    {
        public static ISkillRuntimeObserver ActiveObserver;

        public static void PublishSnapshot(SkillContext context, SkillRuntimeSnapshot snapshot)
        {
            ISkillRuntimeObserver observer = context != null && context.RuntimeObserver != null
                ? context.RuntimeObserver
                : ActiveObserver;

            observer?.OnSnapshotUpdated(snapshot);
        }

        public static void PublishTrace(SkillContext context, SkillRuntimeTraceEvent traceEvent)
        {
            ISkillRuntimeObserver observer = context != null && context.RuntimeObserver != null
                ? context.RuntimeObserver
                : ActiveObserver;

            observer?.OnTraceEmitted(traceEvent);
        }
    }
}
