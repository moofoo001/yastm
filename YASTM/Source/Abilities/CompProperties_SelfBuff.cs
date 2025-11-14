using Verse;
using RimWorld;

namespace ST.Abilities
{
    public class CompProperties_SelfBuff : CompProperties_AbilityEffect
    {
        public string hediffDefName;
        public int durationTicks = 600; // default

        public CompProperties_SelfBuff()
        {
            compClass = typeof(CompAbilityEffect_SelfBuff);
        }
    }
}
