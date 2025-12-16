using RimWorld;
using Verse;

namespace ST.PhaseWeapons
{
    public class Verb_PhaserShoot : Verb_Shoot
    {
        private static CompPhaserMode GetComp(ThingWithComps gear)
        {
            if (gear == null) return null;
            CompPhaserMode fallback = null;
            foreach (var c in gear.AllComps)
            {
                if (c is CompPhaserMode pm)
                {
                    var p = pm.Props;
                    if (p != null && (p.projectileKill != null || p.projectileStun != null || p.projectileOvercharge != null))
                        return pm;  
                    fallback ??= pm;
                }
            }
            return fallback;
        }

            public override ThingDef Projectile
            {
                get
                {
                    var comp = PhaserUtil.GetPhaserComp(EquipmentSource as ThingWithComps);

                    // Log.Message($"[PhaserMode] Verb getter on {EquipmentSource?.def?.defName} {EquipmentSource?.ThingID} mode={(comp!=null ? comp.mode.ToString() : "null-comp")}");
                    if (comp?.Props == null) return base.Projectile;

                    return comp.mode switch
                    {
                        PhaserFireMode.Stun       => comp.Props.projectileStun       ?? comp.Props.projectileKill ?? base.Projectile,
                        PhaserFireMode.Overcharge => comp.Props.projectileOvercharge ?? comp.Props.projectileKill ?? base.Projectile,
                        _                         => comp.Props.projectileKill       ?? base.Projectile
                    };
                }
            }

        protected override bool TryCastShot()
        {
            var comp = GetComp(EquipmentSource as ThingWithComps);
            var old  = verbProps.defaultProjectile;

            if (comp?.Props != null)
            {
                var forced = comp.mode switch
                {
                    PhaserFireMode.Stun       => comp.Props.projectileStun,
                    PhaserFireMode.Overcharge => comp.Props.projectileOvercharge ?? comp.Props.projectileKill,
                    _                         => comp.Props.projectileKill
                };
                if (forced != null) verbProps.defaultProjectile = forced;

                if (comp.mode == PhaserFireMode.Overcharge)
                {
                    float mis = comp.Props.overchargeMisfireChance;
                    if (mis > 0f && Rand.Value < mis)
                    {
                        var pawn = CasterPawn;
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Overcharge!", 1.25f);
                        if (comp.Props.overchargeExplosionRadius > 0.01f)
                            GenExplosion.DoExplosion(pawn.Position, pawn.Map,
                                comp.Props.overchargeExplosionRadius,
                                comp.Props.overchargeExplosionDamage ?? DamageDefOf.Flame, pawn);
                        verbProps.defaultProjectile = old;
                        return false;
                    }
                }
            }

            bool ok = base.TryCastShot();
            verbProps.defaultProjectile = old;
            return ok;
        }
    }
}

