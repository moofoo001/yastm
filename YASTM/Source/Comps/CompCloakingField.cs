using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class CompCloakingField : ThingComp
    {
        public override void CompTickRare()
        {
            base.CompTickRare();

            // power
            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power == null || !power.PowerOn || parent.IsBrokenDown()) return;

            // effects
            if (Rand.Chance(0.2f)) 
            {
                FleckMaker.ThrowDustPuffThick(parent.DrawPos, parent.Map, 1.0f, Color.gray);
            }

            //  jamming debuff
            IReadOnlyList<Pawn> pawns = parent.Map.mapPawns.AllPawnsSpawned;
            
            HediffDef jammerDef = HediffDef.Named("ST_CloakInterference");

            foreach (Pawn p in pawns)
            {
                // only hostile, downed excluded, humanlike or mechanoid
                if (p.HostileTo(parent.Faction) && !p.Downed && (p.RaceProps.Humanlike || p.RaceProps.IsMechanoid))
                {
                    // apply or refresh hediff
                    Hediff existing = p.health.hediffSet.GetFirstHediffOfDef(jammerDef);
                    if (existing == null)
                    {
                        p.health.AddHediff(jammerDef);
                    }
                    else
                    {
                        // refresh severity
                        existing.Severity = 1.0f; 
                    }
                }
            }
        }
    }
    public class CompProperties_CloakingField : CompProperties
    {
        public CompProperties_CloakingField()
        {
            this.compClass = typeof(CompCloakingField);
        }
    }
}