using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class MainTabWindow_MemoryAlpha : MainTabWindow
    {
        // empty window
        public override void DoWindowContents(Rect inRect)
        {
            // nothing here
        }

        public override void PostOpen()
        {
            base.PostOpen();

            // 1. close the empty tab
            Find.MainTabsRoot.EscapeCurrentTab(false);

            // 2. open the big database window
            if (!Find.WindowStack.IsOpen<Dialog_MemoryAlpha>())
            {
                Find.WindowStack.Add(new Dialog_MemoryAlpha());
            }
        }
    }
}