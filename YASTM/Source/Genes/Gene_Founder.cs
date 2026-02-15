using RimWorld;
using RimWorld.Planet; // WICHTIG: Hier lebt GetCaravan()
using Verse;

namespace YASTM
{
    public class Gene_Founder : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            EnsureHediff();
        }

        public override void Tick()
        {
            base.Tick();
            // Prüft alle paar Sekunden (ca. 1 Stunde im Spiel), ob der Buff noch da ist
            if (pawn.IsHashIntervalTick(2500)) 
            {
                EnsureHediff();
            }
        }

        private void EnsureHediff()
        {
            // Nur anwenden, wenn der Pawn existiert (Map oder Karawane)
            if (pawn.Map == null && pawn.GetCaravan() == null) return;
            
            if (!pawn.health.hediffSet.HasHediff(ST_HediffDefOf.ST_Hediff_MorphogenicMatrix))
            {
                pawn.health.AddHediff(ST_HediffDefOf.ST_Hediff_MorphogenicMatrix);
            }
        }
    }
}