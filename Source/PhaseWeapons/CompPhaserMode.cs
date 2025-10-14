using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ST.PhaseWeapons
{
    public enum PhaserFireMode { Kill, Stun, Overcharge }

    public class CompProperties_PhaserMode : CompProperties
    {
        public bool allowOvercharge = false;

        public ThingDef projectileKill;
        public ThingDef projectileStun;
        public ThingDef projectileOvercharge;

        public float overchargeMisfireChance = 0f;
        public float overchargeExplosionRadius = 0f;
        public DamageDef overchargeExplosionDamage;

        public CompProperties_PhaserMode() => compClass = typeof(CompPhaserMode);
    }

    public class CompPhaserMode : ThingComp
    {
        public PhaserFireMode mode = PhaserFireMode.Kill;
        public CompProperties_PhaserMode Props => (CompProperties_PhaserMode)props;

        private Pawn Wielder
        {
            get
            {
                if (parent?.ParentHolder is Pawn_EquipmentTracker eq) return eq.pawn;
                if (parent?.ParentHolder is Pawn_InventoryTracker inv)  return inv.pawn;
                return parent?.TryGetComp<CompEquippable>()?.PrimaryVerb?.CasterPawn;
            }
        }

        public override void PostExposeData() =>
            Scribe_Values.Look(ref mode, "phaserMode", PhaserFireMode.Kill);

            public override IEnumerable<Gizmo> CompGetGizmosExtra()
            {
                // Wichtig: Keine Gizmos hier, damit es nicht doppelt wird.
                yield break;
            }
            public void ToggleStun()
                {
                    // Stun ↔ Kill (deaktiviert Overcharge implizit)
                    mode = (mode == PhaserFireMode.Stun) ? PhaserFireMode.Kill : PhaserFireMode.Stun;
                    Log.Message($"[Phaser2Btn] {parent?.def?.defName} {parent?.ThingID} -> {mode} (StunToggle)");
                }

            public void ToggleOvercharge()
            {
                if (!(Props?.allowOvercharge ?? false)) return;
                // Overcharge ↔ Kill (deaktiviert Stun implizit)
                mode = (mode == PhaserFireMode.Overcharge) ? PhaserFireMode.Kill : PhaserFireMode.Overcharge;
                Log.Message($"[Phaser2Btn] {parent?.def?.defName} {parent?.ThingID} -> {mode} (OverchargeToggle)");
            }
        private Command_Action BuildCycleGizmo()
        {
            var cmd = new Command_Action { hotKey = KeyBindingDefOf.Misc1 };
            void ApplyVisuals()
            {
                cmd.defaultLabel = mode switch
                {
                    PhaserFireMode.Stun => "Mode: Stun",
                    PhaserFireMode.Overcharge => "Mode: Overcharge",
                    _ => "Mode: Lethal"
                };
                string next = mode switch
                {
                    PhaserFireMode.Kill => "Stun",
                    PhaserFireMode.Stun => (Props?.allowOvercharge ?? false) ? "Overcharge" : "Lethal",
                    _ => "Lethal"
                };
                cmd.defaultDesc = $"Click to cycle to {next} mode.";
                cmd.icon = mode switch
                {
                    PhaserFireMode.Stun => ContentFinder<Texture2D>.Get("Things/Projectile/PhaserPulse_Stun", false),
                    PhaserFireMode.Overcharge => ContentFinder<Texture2D>.Get("Things/Projectile/PhaserPulse_Overcharge", false),
                    _ => ContentFinder<Texture2D>.Get("Things/Projectile/PhaserPulse", false)
                };
            }
            ApplyVisuals();

            cmd.action = () =>
            {
                Log.Message($"[PhaserMode] CLICK {parent.def.defName} {parent.ThingID} pre={mode}");
                CycleMode();
                Log.Message($"[PhaserMode] CLICK post={mode}");
                ApplyVisuals();
                SoundDefOf.Click.PlayOneShot(SoundInfo.OnCamera());
            };

            return cmd;
        }

        public void CycleMode()
        {
            Log.Message($"[PhaserMode][cycle] {parent?.def?.defName} {parent?.ThingID} pre={mode}");
            if (Props?.allowOvercharge ?? false)
                mode = mode switch { PhaserFireMode.Kill => PhaserFireMode.Stun, PhaserFireMode.Stun => PhaserFireMode.Overcharge, _ => PhaserFireMode.Kill };
            else
                mode = (mode == PhaserFireMode.Stun) ? PhaserFireMode.Kill : PhaserFireMode.Stun;

            // optional: Hediff/Marker hier updaten, falls du sowas nutzt
            var pawn = Wielder;
            Log.Message($"[PhaserMode] {parent?.def?.defName} {parent?.ThingID} -> {mode} (pawn={pawn?.LabelShort ?? "null"})");
            Log.Message($"[PhaserMode][cycle] post={mode}");
        }
    }
}
