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
                // effect smoke from ears
                FleckMaker.ThrowSmoke(pawn.Position.ToVector3Shifted(), pawn.Map, 1.0f);
                
                // find brain for death report
                BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
                
                // Fallback: if no damage def is defined, use "ExecutionCut"
                DamageDef dmg = Props.damageDef ?? DamageDefOf.ExecutionCut;
                
                //  preventing the "InvalidCastException" Crash in the DamageWorker
                DamageInfo dinfo = new DamageInfo(dmg, 9999, 999f, -1f, pawn, brain);
                
                pawn.Kill(dinfo);
            }
        }
    }
}