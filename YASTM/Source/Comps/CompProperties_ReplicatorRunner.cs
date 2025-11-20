using Verse;

namespace YASTM
{
    // Properties wrapper so XML can reference this comp
    public class CompProperties_ReplicatorRunner : CompProperties
    {
        public CompProperties_ReplicatorRunner()
        {
            compClass = typeof(CompReplicatorRunner);
        }
    }
}

