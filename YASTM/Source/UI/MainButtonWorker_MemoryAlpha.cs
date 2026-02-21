using RimWorld;
using Verse;

namespace YASTM
{
    // Worker for Memory Alpha button
    public class MainButtonWorker_MemoryAlpha : MainButtonWorker_ToggleTab
    {
        public override bool Visible
        {
            get
            {
                // Checks in the options if the checkbox is set
                return YASTM_Mod.Settings.showMemoryAlphaTab && base.Visible;
            }
        }
    }
}