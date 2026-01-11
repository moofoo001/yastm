using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YASTM.Source.Comps
{
    public class CompProperties_Hypospray : CompProperties
    {
        public int maxCharges = 10;
        public float tendQualityOffset = 0.30f; 
        public ThingDef ammoDef; 
        public HediffDef analgesicHediff; 
        public HediffDef stimulantHediff;

        public CompProperties_Hypospray()
        {
            this.compClass = typeof(CompHypospray);
        }
    }

    public class CompHypospray : ThingComp
    {
        public CompProperties_Hypospray Props => (CompProperties_Hypospray)props;
        public int charges;

        public override void PostPostMake()
        {
            base.PostPostMake();
            charges = Props.maxCharges;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref charges, "charges", 0);
        }

        public override string CompInspectStringExtra()
        {
            return $"Charges: {charges} / {Props.maxCharges}";
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            if (parent.ParentHolder is Pawn_EquipmentTracker eq && eq.pawn.IsColonistPlayerControlled)
            {
                // reload hypospray
                if (charges < Props.maxCharges && Props.ammoDef != null)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Reload Hypospray",
                        defaultDesc = $"Refill using {Props.ammoDef.label}.",
                        icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/Replicate"), 
                        action = TryReload
                    };
                }

                // Injektion: Analgesic
                if (charges > 0 && Props.analgesicHediff != null)
                {
                    yield return new Command_Target
                    {
                        defaultLabel = "Inject: Analgesic",
                        defaultDesc = "Administer painkiller (Consumes 1 charge).",
                        icon = ContentFinder<Texture2D>.Get("Things/Item/Equipment/Utilities/Hypospray_pain"),
                        // FIX: Manuelle Parameter statt .ForPawn()
                        targetingParams = new TargetingParameters { 
                            canTargetPawns = true, 
                            canTargetBuildings = false,
                            validator = (TargetInfo x) => x.Thing is Pawn
                        },
                        action = (target) => TryInject(target, Props.analgesicHediff)
                    };
                }

                // Injektion: Stimulant
                if (charges > 0 && Props.stimulantHediff != null)
                {
                    yield return new Command_Target
                    {
                        defaultLabel = "Inject: Stimulant",
                        defaultDesc = "Administer stimulant (Consumes 1 charge).",
                        icon = ContentFinder<Texture2D>.Get("Things/Item/Equipment/Utilities/Hypospray_stim"),
                        // FIX: Manuelle Parameter statt .ForPawn()
                        targetingParams = new TargetingParameters { 
                            canTargetPawns = true, 
                            canTargetBuildings = false,
                            validator = (TargetInfo x) => x.Thing is Pawn
                        },
                        action = (target) => TryInject(target, Props.stimulantHediff)
                    };
                }
            }
        }

        private void TryReload()
        {
            Pawn owner = (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;
            if (owner == null) return;

            Thing ammo = owner.inventory.innerContainer.FirstOrFallback(t => t.def == Props.ammoDef);
            if (ammo != null)
            {
                int amountToAdd = 5; 
                if (ammo.stackCount >= 1)
                {
                    ammo.SplitOff(1).Destroy();
                    charges = Mathf.Min(charges + amountToAdd, Props.maxCharges);
                    Messages.Message("Hypospray recharged.", parent, MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                Messages.Message($"Need {Props.ammoDef.label} in inventory.", parent, MessageTypeDefOf.RejectInput);
            }
        }

        private void TryInject(LocalTargetInfo target, HediffDef hediffDef)
        {
            if (charges <= 0) return;
            Pawn patient = target.Pawn;
            if (patient == null) return;

            Pawn doctor = (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;
            if (doctor != null)
            {
                if (doctor.Position.DistanceTo(patient.Position) <= 2.5f)
                {
                    ApplyInjection(patient, hediffDef);
                }
                else
                {
                    Messages.Message("Patient out of range.", patient, MessageTypeDefOf.RejectInput);
                }
            }
        }

        private void ApplyInjection(Pawn patient, HediffDef hediffDef)
        {
            patient.health.AddHediff(hediffDef);
            if (charges > 0) charges--;
            MoteMaker.ThrowText(patient.DrawPos, patient.Map, hediffDef.LabelCap, Color.white);
            Messages.Message($"Administered {hediffDef.label} to {patient.LabelShort}.", patient, MessageTypeDefOf.NeutralEvent);
        }

        public bool CanTend() => charges > 0;
        public void UseCharge() { if (charges > 0) charges--; }
    }
}