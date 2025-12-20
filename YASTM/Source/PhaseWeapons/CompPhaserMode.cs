using System.Collections.Generic;
using System.Reflection;
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

        public CompProperties_PhaserMode()
        {
            compClass = typeof(CompPhaserMode);
        }
    }

    public class CompPhaserMode : ThingComp
    {
        public PhaserFireMode mode = PhaserFireMode.Kill;
        public CompProperties_PhaserMode Props => (CompProperties_PhaserMode)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref mode, "mode", PhaserFireMode.Kill);
        }

        // --- REFLECTION HELPER (Linux Kompatibilität) ---
        private static FieldInfo disabledField;
        private void SetGizmoDisabled(Gizmo g, bool val)
        {
            if (disabledField == null)
            {
                // Sucht das Feld, egal ob public oder protected
                disabledField = typeof(Gizmo).GetField("disabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (disabledField != null)
            {
                disabledField.SetValue(g, val);
            }
        }
        // ------------------------------------------------

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            var pawn = (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled) yield break;

            // Kill Mode
            var cmdKill = new Command_Action
            {
                defaultLabel = "Kill",
                defaultDesc = "Lethal setting.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/PhaserLethal"),
                action = () => SetMode(PhaserFireMode.Kill),
                groupKey = 3133701
            };
            if (mode == PhaserFireMode.Kill) SetGizmoDisabled(cmdKill, true);
            yield return cmdKill;

            // Stun Mode
            var cmdStun = new Command_Action
            {
                defaultLabel = "Stun",
                defaultDesc = "Non-lethal setting.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/PhaserStun"),
                action = () => SetMode(PhaserFireMode.Stun),
                groupKey = 3133701
            };
            if (mode == PhaserFireMode.Stun) SetGizmoDisabled(cmdStun, true);
            yield return cmdStun;

            // Overcharge Mode
            if (Props.allowOvercharge)
            {
                var cmdOver = new Command_Action
                {
                    defaultLabel = "Overcharge",
                    defaultDesc = "Dangerous high energy output.",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/PhaserOverload"),
                    action = () => SetMode(PhaserFireMode.Overcharge),
                    groupKey = 3133701
                };
                if (mode == PhaserFireMode.Overcharge) SetGizmoDisabled(cmdOver, true);
                yield return cmdOver;
            }
        }

        private void SetMode(PhaserFireMode newMode)
        {
            mode = newMode;
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }
    }
}