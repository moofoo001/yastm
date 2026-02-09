using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    // Die Eigenschaften aus der XML
    public class CompProperties_CloakingDevice : CompProperties
    {
        public AbilityDef abilityDef; // Damit Ihre XML nicht abstürzt
        
        public CompProperties_CloakingDevice()
        {
            this.compClass = typeof(CompCloakingDevice);
        }
    }

    public class CompCloakingDevice : ThingComp
    {
        public CompProperties_CloakingDevice Props => (CompProperties_CloakingDevice)props;

        private bool isActive;
        public bool IsActive => isActive;

        public Pawn Wearer => (parent as Apparel)?.Wearer;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isActive, "isActive", false);
        }

        // --- STEUERUNG ---
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (var g in base.CompGetWornGizmosExtra()) yield return g;

            // Der An/Aus Schalter
            yield return new Command_Toggle
            {
                defaultLabel = "Cloak",
                defaultDesc = "Toggle the personal cloaking field.",
                icon = parent.def.uiIcon,
                isActive = () => isActive,
                toggleAction = () => 
                {
                    isActive = !isActive;
                    if (!isActive) RemoveCloak();
                }
            };
        }

        // --- DER HEARTBEAT (Alle 4 Sekunden) ---
        public override void CompTickRare()
        {
            base.CompTickRare();
            
            // Wenn an und getragen -> Tarnung erneuern
            if (isActive && Wearer != null)
            {
                RefreshCloak(Wearer);
            }
            // Wenn an aber nicht getragen -> Aus
            else if (isActive && Wearer == null)
            {
                isActive = false;
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

            // Timer zurücksetzen (Heartbeat)
            var disappearComp = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = 300; // 5 Sekunden Puffer
            }
        }

        private void RemoveCloak()
        {
            if (Wearer != null && ST_HediffDefOf.ST_CloakingField != null)
            {
                var hediff = Wearer.health.hediffSet.GetFirstHediffOfDef(ST_HediffDefOf.ST_CloakingField);
                if (hediff != null) Wearer.health.RemoveHediff(hediff);
            }
        }
        
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            isActive = false;
            RemoveCloak(); // Sofort enttarnen beim Ausziehen
        }
    }
}