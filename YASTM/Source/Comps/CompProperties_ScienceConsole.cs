using Verse;

namespace YASTM
{
    // Plain CompProperties wrapper so the Science console can attach the comp via XML.
    public class CompProperties_ScienceConsole : CompProperties
    {
        public CompProperties_ScienceConsole()
        {
            compClass = typeof(CompScienceConsole);
        }
    }
}
