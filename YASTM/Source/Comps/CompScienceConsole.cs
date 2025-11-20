using Verse;

namespace YASTM
{
    // Simple properties holder (XML: <compClass>YASTM.CompScienceConsole</compClass>)
    public class CompProperties_ScienceConsole : CompProperties
    {
        // purely informational; the actual boost is handled by your CompProperties_Facility in XML
        public float researchSpeedFactor = 0.04f;   // +4%
        public int maxLinkedPerBuilding = 2;

        public CompProperties_ScienceConsole()
        {
            compClass = typeof(CompScienceConsole);
        }
    }

    // Lightweight comp that only provides inspect info; no AllComps/ThingWithComps needed
    public class CompScienceConsole : ThingComp
    {
        public CompProperties_ScienceConsole Props => (CompProperties_ScienceConsole)props;

        public override string CompInspectStringExtra()
        {
            // We just show what the XML facility would provide; showing "(inactive)" keeps parity with your screenshot.
            // If you want live status later, we'll add a tiny radius/link probe that doesn't rely on AllComps.
            string pct = (Props.researchSpeedFactor * 100f).ToString("0.#");
            return $"Research speed factor: +{pct}% (inactive)\nMax connected per building: {Props.maxLinkedPerBuilding}";
        }
    }
}

