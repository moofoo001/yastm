using RimWorld;
using Verse;
using Verse.Sound;

namespace ST.Abilities
{
    public class CompAbilityEffect_FieldTriage : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages)) return false;
            var pawn = target.Pawn;
            if (pawn == null || pawn.Dead) return false;

            // Caster sollte medizinisch geschult sein (StarfleetTraining)
            var caster = parent.pawn;
            var t = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Trait_StarfleetTraining");
            if (t != null && caster?.story?.traits?.HasTrait(t) != true)
            {
                if (throwMessages) Messages.Message("Requires Starfleet training.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null || pawn.Dead) return;

            var buff = DefDatabase<HediffDef>.GetNamedSilentFail("ST_FieldTriageBuff");
            if (buff != null)
            {
                var h = pawn.health?.AddHediff(buff);
                // Optional: skalieren mit Medicine-Skill des Casters
                var casterMed = parent.pawn?.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;
                // leichte Skalierung der Dauer via Skill
                var comp = h?.TryGetComp<HediffComp_Disappears>();
                if (comp != null) comp.ticksToDisappear = 2400 + (int)(casterMed * 60f); // 40s + 1s/Skill

                var snd = DefDatabase<SoundDef>.GetNamedSilentFail("ST_SND_FieldTriageCast");
                if (snd != null && pawn?.Map != null)
                {
                    Verse.Sound.SoundStarter.PlayOneShot(snd, Verse.Sound.SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map)));
                }

                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, 0.9f);
                FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.Map);
            }
        }
    }
}
