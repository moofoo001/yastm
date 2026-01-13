using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class CompCloakingField : ThingComp
    {
        // cloak active state
        private bool isCloakActive = true;

        // radius of the cloaking field
        private const float FieldRadius = 50f; 

        public CompProperties_CloakingField Props => (CompProperties_CloakingField)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            // store the active state
            Scribe_Values.Look(ref isCloakActive, "isCloakActive", true);
        }

        // 1. Gizmo
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            // if player faction, show toggle
            if (parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "ST_CloakToggle".Translate(), // "Cloaking Field"
                    defaultDesc = "ST_CloakToggleDesc".Translate(), // "activate or deactivate..."
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RomulanCloak", true), 
                    isActive = () => isCloakActive,
                    toggleAction = () => { isCloakActive = !isCloakActive; }
                };
            }
        }

        // status string
        public override string CompInspectStringExtra()
        {
            if (!isCloakActive)
            {
                return "ST_CloakStatus_Disabled".Translate(); // "Cloaking: Disabled"
            }
            
            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                return "ST_CloakStatus_NoPower".Translate(); // "Cloaking: Offline (No Power)"
            }

            return "ST_CloakStatus_Active".Translate(); // "Cloaking: ACTIVE"
        }

        // visual radius overlay
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (isCloakActive)
            {
                // draw radius
                GenDraw.DrawRadiusRing(parent.Position, FieldRadius, Color.green);
            }
        }

        // timer tick
        public override void CompTickRare()
        {
            base.CompTickRare();

            if (!isCloakActive) return;

            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power == null || !power.PowerOn || parent.IsBrokenDown()) return;

            // effect visuals
            FleckMaker.ThrowLightningGlow(parent.DrawPos, parent.Map, 3.0f);
            
            if (Rand.Chance(0.1f)) 
            {
                MoteMaker.ThrowMetaIcon(parent.Position, parent.Map, ThingDefOf.Mote_PowerBeam);
            }

            // for each hostile pawn in radius, apply jammer hediff
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
                        
                        // optional text mote
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

    // properties class
    public class CompProperties_CloakingField : CompProperties
    {
        public CompProperties_CloakingField()
        {
            this.compClass = typeof(CompCloakingField);
        }
    }
}