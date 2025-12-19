using RimWorld;
using Verse;

namespace YASTM.Source.Comps
{
    public class CompProperties_Biobed : CompProperties
    {
        public HediffDef hediffDef;

        public CompProperties_Biobed()
        {
            this.compClass = typeof(CompBiobed);
        }
    }

    public class CompBiobed : ThingComp
    {
        public CompProperties_Biobed Props => (CompProperties_Biobed)props;

        // Wir prüfen nur alle 250 Ticks (ca. 4 Sekunden), um Performance zu sparen
        public override void CompTickRare()
        {
            base.CompTickRare();

            Building_Bed bed = parent as Building_Bed;
            if (bed == null) return;

            // 1. Hat das Bett Strom?
            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power == null || !power.PowerOn) return;

            // 2. Liegen Patienten darin?
            // (CurOccupants ist besser als GetCurOccupant, da es auch für Doppelbetten funktioniert)
            foreach (Pawn occupant in bed.CurOccupants)
            {
                if (occupant != null && !occupant.Dead)
                {
                    ApplyBioMonitor(occupant);
                }
            }
        }

        private void ApplyBioMonitor(Pawn patient)
        {
            // Wir fügen den Hediff hinzu oder erneuern ihn
            Hediff hediff = patient.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (hediff == null)
            {
                patient.health.AddHediff(Props.hediffDef);
            }
            else
            {
                // Reset timer (Disappears component)
                // Wir setzen die Ticks zurück, damit der Effekt bleibt, solange man liegt
                var comp = hediff.TryGetComp<HediffComp_Disappears>();
                if (comp != null)
                {
                    comp.ticksToDisappear = 300; 
                }
            }
        }
    }
}