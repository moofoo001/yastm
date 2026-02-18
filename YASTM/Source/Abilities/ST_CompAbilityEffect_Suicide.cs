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
                // Lore-Effekt:
                FleckMaker.ThrowSmoke(pawn.Position.ToVector3Shifted(), pawn.Map, 1.5f);
                
                // buff: "Loyalty Sacrifice"
                if (pawn.Map != null)
                {
                    foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
                    {
                        // Only own faction and humanlikes get the buff
                        if (p.Faction == pawn.Faction && p.RaceProps.Humanlike && p != pawn)
                        {
                            p.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("ST_Thought_WitnessedTermination"));
                        }
                    }
                }

                // The death
                BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
                DamageDef dmg = Props.damageDef ?? DamageDefOf.ExecutionCut;
                DamageInfo dinfo = new DamageInfo(dmg, 9999, 999f, -1f, pawn, brain);
                pawn.Kill(dinfo);
            }
        }
    }
}