using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class CompProperties_CloakingField : CompProperties
    {
        public float radius = 15f; // Standard-Radius
        public CompProperties_CloakingField()
        {
            this.compClass = typeof(CompCloakingField);
        }
    }

    public class CompCloakingField : ThingComp
    {
        public CompProperties_CloakingField Props => (CompProperties_CloakingField)props;

        public bool IsActive
        {
            get
            {
                var power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        // Optimierter Loop: Nur alle 250 Ticks prüfen
        public override void CompTickRare()
        {
            base.CompTickRare();

            if (IsActive)
            {
                ApplyCloakToArea();
            }
        }

        private void ApplyCloakToArea()
        {
            float radius = Props.radius;
            // Nutze GenRadial für effiziente Zell-Suche
            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(parent.Position, radius, true);

            foreach (IntVec3 c in cells)
            {
                if (!c.InBounds(parent.Map)) continue;
                
                List<Thing> thingList = c.GetThingList(parent.Map);
                foreach (Thing t in thingList)
                {
                    // Nur eigene Kolonisten tarnen
                    if (t is Pawn p && !p.Dead && p.Faction == Faction.OfPlayer) 
                    {
                        RefreshCloak(p);
                    }
                }
            }
        }

        private void RefreshCloak(Pawn p)
        {
            if (ST_HediffDefOf.ST_CloakingField == null) return;

            var hediff = p.health.hediffSet.GetFirstHediffOfDef(ST_HediffDefOf.ST_CloakingField);
            if (hediff == null)
            {
                hediff = p.health.AddHediff(ST_HediffDefOf.ST_CloakingField);
            }

            // HEARTBEAT: Timer verlängern
            var disappearComp = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = 300; // 5 Sekunden
            }
        }

        public override string CompInspectStringExtra()
        {
            return IsActive ? "Cloaking Field: Active" : "Cloaking Field: Offline";
        }
        
        // Optional: Radius zeichnen wenn ausgewählt
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(parent.Position, Props.radius);
        }
    }
}