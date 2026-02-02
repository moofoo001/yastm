using RimWorld;
using Verse;

namespace YASTM
{
    public class CompProperties_Visor : CompProperties
    {
        public CompProperties_Visor()
        {
            this.compClass = typeof(CompVisor);
        }
    }

    public class CompVisor : ThingComp
    {
        private int checkInterval = 120; // Alle 2 Sekunden prüfen

        public override void CompTick()
        {
            base.CompTick();
            
            // FIX: Wir fragen das Item "Bist du Kleidung? Wer trägt dich?"
            Apparel apparel = parent as Apparel;
            if (apparel == null) return;

            Pawn pawn = apparel.Wearer; // Das ist der korrekte Weg!
            
            if (pawn == null || pawn.Dead) return;

            // Performance: Nicht jeden Tick prüfen
            if (pawn.IsHashIntervalTick(checkInterval))
            {
                UpdateVisorStatus(pawn);
            }
        }

        private void UpdateVisorStatus(Pawn pawn)
        {
            bool hasImplant = HasNeuralInterface(pawn);
            Hediff uplink = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("ST_Hediff_VisorUplink"));

            if (hasImplant)
            {
                if (uplink == null)
                {
                    pawn.health.AddHediff(HediffDef.Named("ST_Hediff_VisorUplink"));
                }
            }
            else
            {
                if (uplink != null)
                {
                    pawn.health.RemoveHediff(uplink);
                }
            }
        }

        private bool HasNeuralInterface(Pawn p)
        {
            if (p.health == null || p.health.hediffSet == null) return false;
            return p.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamedSilentFail("ST_Hediff_NeuralInterface"));
        }
        
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            Hediff uplink = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("ST_Hediff_VisorUplink"));
            if (uplink != null)
            {
                pawn.health.RemoveHediff(uplink);
            }
        }
    }
}