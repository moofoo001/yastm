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

        
        public override void CompTickRare()
        {
            base.CompTickRare();

            Building_Bed bed = parent as Building_Bed;
            if (bed == null) return;


            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power == null || !power.PowerOn) return;


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

            Hediff hediff = patient.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (hediff == null)
            {
                patient.health.AddHediff(Props.hediffDef);
            }
            else
            {

                var comp = hediff.TryGetComp<HediffComp_Disappears>();
                if (comp != null)
                {
                    comp.ticksToDisappear = 300; 
                }
            }
        }
    }
}