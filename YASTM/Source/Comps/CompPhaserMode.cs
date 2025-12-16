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
    // Yield base gizmos first
    foreach (var g in base.CompGetGizmosExtra() ?? System.Array.Empty<Gizmo>())
        yield return g;

    var pawn = Wielder;
    if (pawn == null || pawn.Faction != Faction.OfPlayer)
        yield break;

    // Local helper to build our toggle commands
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
                var old = mode;
                mode = (mode == targetMode) ? PhaserFireMode.Kill : targetMode;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();

                // Debug logging – safe, no interpolated string
                string wielderName = Wielder != null ? Wielder.LabelShort : "(no wielder)";
                Log.Message("[YASTM][PHASER] " + wielderName
                            + " switched " + parent.Label
                            + " from " + old + " to " + mode);
            }
        };
        return cmd;
    }

    // Stun mode
    yield return Make(
        "ST.Phaser.Mode.Stun",
        "ST.Phaser.Mode.Stun.Desc",
        "Things/Projectile/PhaserPulse_Stun",
        PhaserFireMode.Stun,
        KeyBindingDefOf.Misc1
    );

    // Overcharge mode (optional)
    if (Props != null && Props.allowOvercharge)
    {
        yield return Make(
            "ST.Phaser.Mode.Overcharge",
            "ST.Phaser.Mode.Overcharge.Desc",
            "Things/Projectile/PhaserPulse_Overcharge",
            PhaserFireMode.Overcharge,
            KeyBindingDefOf.Misc2
        );
    }
}


        public void ToggleStun()
        {
            mode = (mode == PhaserFireMode.Stun) ? PhaserFireMode.Kill : PhaserFireMode.Stun;
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }


        public void ToggleOvercharge()
        {
            if (!(Props?.allowOvercharge ?? false))
                return;

            mode = (mode == PhaserFireMode.Overcharge) ? PhaserFireMode.Kill : PhaserFireMode.Overcharge;
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }

       
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

