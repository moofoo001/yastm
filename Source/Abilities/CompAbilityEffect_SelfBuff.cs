using RimWorld;
using Verse;
using Verse.Sound;  // oben


namespace ST.Abilities
{
    public class CompAbilityEffect_SelfBuff : CompAbilityEffect
    {
        public new CompProperties_SelfBuff Props => (CompProperties_SelfBuff)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return parent?.pawn != null && parent.pawn.Spawned;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = parent.pawn;
            if (pawn == null || Props == null || string.IsNullOrEmpty(Props.hediffDefName)) return;

            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.hediffDefName);
            if (hediffDef == null) return;

            var h = pawn.health?.AddHediff(hediffDef);
            var comp = h?.TryGetComp<HediffComp_Disappears>();
            if (comp != null) comp.ticksToDisappear = Props.durationTicks;

            var snd = DefDatabase<SoundDef>.GetNamedSilentFail("ST_SND_TacticalOverwatchCast");
            if (snd != null && pawn?.Map != null)
            {
                SoundStarter.PlayOneShot(snd, SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map)));
            }
        }
    }
}
