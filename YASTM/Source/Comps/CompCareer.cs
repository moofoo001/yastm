using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_Career : CompProperties
    {
        public ST_CareerDef career;

        public CompProperties_Career()
        {
            this.compClass = typeof(CompCareer);
        }
    }

    public class CompCareer : ThingComp
    {
        public int ticksInCurrentRank = 0;
        private Dictionary<string, int> pointTracker = new Dictionary<string, int>();
        
        public ST_CareerDef career; 

        public CompProperties_Career Props => (CompProperties_Career)props;

        public void AddCareerPoint(string category, int amount = 1)
        {
            if (!pointTracker.ContainsKey(category))
            {
                pointTracker[category] = 0;
            }
            pointTracker[category] += amount;
            
            TryPromote();
        }

        public int GetPoints(string category)
        {
            if (pointTracker.TryGetValue(category, out int val)) return val;
            return 0;
        }

        // --- FIX: Jetzt PUBLIC, damit Patches darauf zugreifen können ---
        public void TryPromote()
        {
            // Placeholder Logik
            // Hier würde die echte Prüfung stattfinden.
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (parent is Pawn p && !p.Dead)
            {
                ticksInCurrentRank += 250;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksInCurrentRank, "ticksInCurrentRank", 0);
            Scribe_Collections.Look(ref pointTracker, "pointTracker", LookMode.Value, LookMode.Value);
            Scribe_Defs.Look(ref career, "career"); 
        }
    }
}