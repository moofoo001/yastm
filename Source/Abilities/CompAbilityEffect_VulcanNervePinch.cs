using RimWorld;
using Verse;

namespace ST.Abilities
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

            // Caster braucht das passende Trait (IDIC)
            var caster = parent.pawn;
            var traitIDIC = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Trait_IDICMindset");
            if (caster?.story?.traits?.HasTrait(traitIDIC) != true)
            {
                if (throwMessages) Messages.Message("Requires IDIC mindset.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Nahkampfreichweite
            if (caster.Position.DistanceToSquared(pawn.Position) > 2.8f) // ~1.67 cells
                return false;

            return true;
            }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null || pawn.Dead) return;

            // Statt Hediff: StunHandler verwenden (keine DefOfs nötig)
            var stunner = pawn.stances?.stunner;
            if (stunner != null)
            {
                // 300 Ticks = 5 Sekunden
                stunner.StunFor(300, parent.pawn, addBattleLog: true, showMote: true);
            }
        }
    }
}
