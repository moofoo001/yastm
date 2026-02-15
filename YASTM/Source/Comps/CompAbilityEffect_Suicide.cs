using RimWorld;
using Verse;

namespace YASTM
{
    public class CompProperties_AbilitySuicide : CompProperties_AbilityEffect
    {
        public DamageDef damageDef;
        
        public CompProperties_AbilitySuicide()
        {
            this.compClass = typeof(CompAbilityEffect_Suicide);
        }
    }

    public class CompAbilityEffect_Suicide : CompAbilityEffect
    {
        public new CompProperties_AbilitySuicide Props => (CompProperties_AbilitySuicide)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            
            if (pawn != null && !pawn.Dead)
            {
                // Kopf suchen
                BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
                
                DamageInfo dinfo = new DamageInfo(Props.damageDef ?? DamageDefOf.ExecutionCut, 99999, 999, -1, pawn, brain);
                pawn.TakeDamage(dinfo);
                
                if (!pawn.Dead) pawn.Kill(dinfo); // Sicher ist sicher
            }
        }
    }
}