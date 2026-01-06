using RimWorld;
using Verse;

namespace YASTM.Abilities
{
    public class CompAbilityEffect_VulcanNervePinch : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages)) return false;

            var pawn = target.Pawn;
            if (pawn == null || pawn.Dead || pawn.RaceProps?.IsMechanoid == true)
            {
                if (throwMessages) Messages.Message("Invalid target.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            
            var caster = parent.pawn;
            var traitIDIC = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Trait_IDICMindset");
            if (caster?.story?.traits?.HasTrait(traitIDIC) != true)
            {
                if (throwMessages) Messages.Message("Requires IDIC mindset.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            
            if (caster.Position.DistanceToSquared(pawn.Position) > 2.8f) 
                return false;

            return true;
            }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null || pawn.Dead) return;

            
            var stunner = pawn.stances?.stunner;
            if (stunner != null)
            {
                
                stunner.StunFor(300, parent.pawn, addBattleLog: true, showMote: true);
            }
        }
    }
}

