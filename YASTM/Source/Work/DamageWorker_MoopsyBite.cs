using RimWorld;
using Verse;

namespace YASTM
{
    public class DamageWorker_MoopsyBite : DamageWorker_AddInjury
    {
        protected override void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageResult result)
        {
            // its only a small bite for a moopsy
            base.ApplySpecialEffectsToPart(pawn, totalDamage, dinfo, result);

            // is mech ?
            if (pawn.RaceProps.IsFlesh && pawn.health != null && !pawn.Dead)
            {
                HediffDef bonelessDef = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Hediff_BoneLiquefaction");
                if (bonelessDef != null)
                {
                    // greater debuff
                    Hediff hediff = HediffMaker.MakeHediff(bonelessDef, pawn, dinfo.HitPart);
                    pawn.health.AddHediff(hediff);
                    
                    // vlood for the blood-moopsy
                    FleckMaker.ThrowMicroSparks(pawn.Position.ToVector3Shifted(), pawn.Map);
                }
            }
        }
    }
}