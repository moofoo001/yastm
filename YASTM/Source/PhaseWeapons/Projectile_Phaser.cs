using RimWorld;
using Verse;

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
        static HediffDef StunMarker =>
            DefDatabase<HediffDef>.GetNamedSilentFail("ST_Hediff_PhaserStunMode");

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            var shooter  = launcher as Pawn;
            var modeComp = shooter?.equipment?.Primary?.TryGetComp<CompPhaserMode>();
            var ext      = def.GetModExtension<ModExtension_PhaserSettings>();

            bool wantStun =
                (modeComp != null && modeComp.mode == PhaserFireMode.Stun) ||
                (StunMarker != null && shooter != null && shooter.health.hediffSet.HasHediff(StunMarker));

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

            if (modeComp != null && modeComp.mode == PhaserFireMode.Overcharge && launcher?.Map != null)
            {
                GenExplosion.DoExplosion(Position, launcher.Map, 1.3f, DamageDefOf.Flame, shooter);
            }

            base.Impact(hitThing, blockedByShield);
        }
    }
}
