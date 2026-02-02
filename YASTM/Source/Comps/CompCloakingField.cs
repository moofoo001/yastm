using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class CompCloakingField : ThingComp
    {
        private bool isCloakActive = true;
        private const float FieldRadius = 50f; 

        public CompProperties_CloakingField Props => (CompProperties_CloakingField)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isCloakActive, "isCloakActive", true);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "ST_CloakToggle".Translate(),
                    defaultDesc = "ST_CloakToggleDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RomulanCloak", true),
                    isActive = () => isCloakActive,
                    toggleAction = () => { isCloakActive = !isCloakActive; }
                };
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!isCloakActive) return "ST_CloakStatus_Disabled".Translate();
            
            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn) return "ST_CloakStatus_NoPower".Translate();

            return "ST_CloakStatus_Active".Translate();
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (isCloakActive)
            {
                GenDraw.DrawRadiusRing(parent.Position, FieldRadius, Color.green);
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            if (!isCloakActive) return;

            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power == null || !power.PowerOn || parent.IsBrokenDown()) return;

            // visual effect
            FleckMaker.ThrowLightningGlow(parent.DrawPos, parent.Map, 3.0f);
            
            // effects
            if (Rand.Chance(0.5f))
            {
                FleckMaker.ThrowHeatGlow(parent.Position, parent.Map, 1.5f);
            }

            // logic effect
            IReadOnlyList<Pawn> pawns = parent.Map.mapPawns.AllPawnsSpawned;
            HediffDef jammerDef = HediffDef.Named("ST_CloakInterference");

            foreach (Pawn p in pawns)
            {
                if (p.HostileTo(parent.Faction) && !p.Downed && (p.RaceProps.Humanlike || p.RaceProps.IsMechanoid))
                {
                    Hediff existing = p.health.hediffSet.GetFirstHediffOfDef(jammerDef);
                    if (existing == null)
                    {
                        p.health.AddHediff(jammerDef);
                        
                        // feedback
                        if (p.IsHashIntervalTick(250)) 
                        {
                            MoteMaker.ThrowText(p.DrawPos, p.Map, "Jammed", Color.green); 
                        }
                    }
                    else
                    {
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