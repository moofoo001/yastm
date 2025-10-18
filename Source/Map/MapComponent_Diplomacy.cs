using Verse;

namespace YASTM
{
    public class MapComponent_Diplomacy : MapComponent
    {
        public int NextImproveTick;
        public int NextCeaseTick;

        public MapComponent_Diplomacy(Map map) : base(map) {}

        public override void ExposeData()
        {
            Scribe_Values.Look(ref NextImproveTick, "YASTM_Diplo_NextImproveTick", 0);
            Scribe_Values.Look(ref NextCeaseTick,   "YASTM_Diplo_NextCeaseTick",   0);
        }
    }
}
