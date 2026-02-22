using RimWorld;
using Verse;
using YASTM; 

namespace ST.PhaseWeapons
{
    public class ModExtension_PhaserSettings : DefModExtension
    {
        public int   stunTicks     = 240;
        public float stunChance    = 1f;
        public bool  stunOnlyFlesh = true;
    }

    public class Projectile_Phaser : Bullet
    {
        static HediffDef StunMarker => DefDatabase<HediffDef>.GetNamedSilentFail("ST_Hediff_PhaserStunMode");

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            var shooter  = launcher as Pawn;
            
            // Lese das NEUE Waffen-System aus!
            var modeComp = shooter?.equipment?.Primary?.TryGetComp<CompMultiModeWeapon>();
            var ext      = def.GetModExtension<ModExtension_PhaserSettings>();

            bool isStunMode = modeComp != null && modeComp.CurrentMode != null && modeComp.CurrentMode.label.ToLower().Contains("stun");
            bool isOverloadMode = modeComp != null && modeComp.CurrentMode != null && modeComp.CurrentMode.isOverload;

            bool wantStun = isStunMode || (StunMarker != null && shooter != null && shooter.health.hediffSet.HasHediff(StunMarker));

            if (wantStun && !blockedByShield && hitThing is Pawn target)
            {
                if (!(ext?.stunOnlyFlesh ?? false) || (target.RaceProps?.IsFlesh ?? false))
                {
                    if (Rand.Value <= (ext?.stunChance ?? 1f))
                    {
                        target.stances?.stunner?.StunFor(ext?.stunTicks ?? 240, shooter);
                        Destroy(DestroyMode.Vanish);
                        return;
                    }
                }
            }

            if (isOverloadMode && launcher?.Map != null)
            {
                GenExplosion.DoExplosion(Position, launcher.Map, 1.3f, DamageDefOf.Flame, shooter);
            }

            base.Impact(hitThing, blockedByShield);
        }
    }
}