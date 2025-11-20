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

        // Optional: explizite Projektile je Modus (wenn nicht gesetzt, bleibt defaultProjectile aktiv)
        public ThingDef projectileKill;
        public ThingDef projectileStun;
        public ThingDef projectileOvercharge;   // von Verb_PhaserShoot/PhaserUtil erwartet

        // Optional: Overcharge-Nebenwirkungen / Backfire (werden von Verb_PhaserShoot genutzt)
        public float overchargeMisfireChance = 0f; // 0 = aus
        public float overchargeExplosionRadius = 0f;
        public DamageDef overchargeExplosionDamage;

        public CompProperties_PhaserMode()
        {
            compClass = typeof(CompPhaserMode);
        }
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
                if (parent?.ParentHolder is Pawn_InventoryTracker inv) return inv.pawn;
                return parent?.TryGetComp<CompEquippable>()?.PrimaryVerb?.CasterPawn;
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref mode, "phaserMode", PhaserFireMode.Kill);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Falls andere Mods/Comps eigene Gizmos liefern, zuerst durchreichen
            foreach (var g in base.CompGetGizmosExtra() ?? System.Array.Empty<Gizmo>())
                yield return g;

            var pawn = Wielder;
            if (pawn == null || pawn.Faction != Faction.OfPlayer)
                yield break;

            // Helper: exklusiver Toggle (erneutes Klicken -> Kill/Normal)
            Command_Toggle Make(string labelKey, string descKey, string iconPath,
                                PhaserFireMode targetMode, KeyBindingDef hotkey)
            {
                var cmd = new Command_Toggle
                {
                    defaultLabel = labelKey.Translate(),
                    defaultDesc  = descKey.Translate(),
                    icon         = ContentFinder<Texture2D>.Get(iconPath, true),
                    hotKey       = hotkey,
                    isActive     = () => mode == targetMode,
                    toggleAction = () =>
                    {
                        mode = (mode == targetMode) ? PhaserFireMode.Kill : targetMode;
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                };
                return cmd;
            }

            // STUN (immer verfügbar)
            yield return Make("ST.Phaser.Mode.Stun",
                              "ST.Phaser.Mode.Stun.Desc",
                              "Things/Projectile/PhaserPulse_Stun",
                              PhaserFireMode.Stun,
                              KeyBindingDefOf.Misc1);

            // OVERCHARGE (nur wenn erlaubt)
            if (Props?.allowOvercharge ?? false)
            {
                yield return Make("ST.Phaser.Mode.Overcharge",
                                  "ST.Phaser.Mode.Overcharge.Desc",
                                  "Things/Projectile/PhaserPulse_Overcharge",
                                  PhaserFireMode.Overcharge,
                                  KeyBindingDefOf.Misc2);
            }
        }

        // Abwärts-kompatibel (wird evtl. aus Patches/Jobs aufgerufen)
        public void ToggleStun()
        {
            mode = (mode == PhaserFireMode.Stun) ? PhaserFireMode.Kill : PhaserFireMode.Stun;
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }

        // Von Patches.cs erwartet (Fehler zuvor)
        public void ToggleOvercharge()
        {
            if (!(Props?.allowOvercharge ?? false))
                return;

            mode = (mode == PhaserFireMode.Overcharge) ? PhaserFireMode.Kill : PhaserFireMode.Overcharge;
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }

        // Optionaler Modus-Zyklus (für Alt-Code erhalten)
        public void CycleMode()
        {
            if (Props?.allowOvercharge ?? false)
            {
                mode = mode switch
                {
                    PhaserFireMode.Kill        => PhaserFireMode.Stun,
                    PhaserFireMode.Stun        => PhaserFireMode.Overcharge,
                    _                          => PhaserFireMode.Kill
                };
            }
            else
            {
                mode = (mode == PhaserFireMode.Stun) ? PhaserFireMode.Kill : PhaserFireMode.Stun;
            }
        }
    }
}

