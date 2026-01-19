using Verse;

namespace YASTM
{
    public enum AlertLevel : byte { Green=0, Yellow=1, Red=2 }

    public class MapComponent_ColonyAlert : MapComponent
    {
        public AlertLevel Level = AlertLevel.Green;
        public MapComponent_ColonyAlert(Map map) : base(map) { }
    }
}

