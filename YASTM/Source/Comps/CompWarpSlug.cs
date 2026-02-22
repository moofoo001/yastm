using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib; 

namespace YASTM
{
    public class CompProperties_WarpSlug : CompProperties
    {
        public float warpChance = 0.75f; // warp chance
        public int warpRadius = 25;      // warp radius

        public CompProperties_WarpSlug()
        {
            this.compClass = typeof(CompWarpSlug);
        }
    }

    public class CompWarpSlug : ThingComp
    {
        public CompProperties_WarpSlug Props => (CompProperties_WarpSlug)this.props;

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);

            if (this.parent is Pawn pawn && !pawn.Dead && pawn.Spawned)
            {
                if (Rand.Value < Props.warpChance)
                {
                    PerformPanicWarp(pawn);
                }
            }
        }

        private void PerformPanicWarp(Pawn pawn)
        {
            Map map = pawn.Map;
            IntVec3 currentPos = pawn.Position;

            if (CellFinder.TryFindRandomCellNear(currentPos, map, Props.warpRadius, c => c.Standable(map) && c.Walkable(map), out IntVec3 targetCell))
            {
                FleckMaker.ThrowLightningGlow(currentPos.ToVector3Shifted(), map, 1.5f);
                FleckMaker.ThrowDustPuffThick(currentPos.ToVector3Shifted(), map, 2.0f, new Color(0.2f, 0.6f, 1f));

                pawn.Position = targetCell;
                pawn.Notify_Teleported(true, true);

                FleckMaker.ThrowLightningGlow(targetCell.ToVector3Shifted(), map, 1.5f);
                FleckMaker.ThrowDustPuffThick(targetCell.ToVector3Shifted(), map, 2.0f, new Color(0.2f, 0.6f, 1f));

                FilthMaker.TryMakeFilth(targetCell, map, ThingDefOf.Filth_Slime);

                MoteMaker.ThrowText(targetCell.ToVector3Shifted(), map, "Warped!", Color.cyan);
            }
        }
    }

    // =========================================================
    // HARMONY PATCH: slug bite reaction
    // =========================================================
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class Patch_WarpSlugPoisonBite
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo)
        {
            if (dinfo.Instigator is Pawn attacker && attacker.def.defName == "ST_WarpSlug")
            {
                if (dinfo.Def == DamageDefOf.Bite)
                {
                    if (!__instance.Dead)
                    {
                        HediffDef poisonDef = DefDatabase<HediffDef>.GetNamedSilentFail("ST_WarpSlugBiteReaction");
                        if (poisonDef != null)
                        {
                            Hediff existingPoison = __instance.health.hediffSet.GetFirstHediffOfDef(poisonDef);
                            if (existingPoison != null)
                            {
                                existingPoison.Severity += 0.25f; 
                            }
                            else
                            {
                                Hediff newPoison = HediffMaker.MakeHediff(poisonDef, __instance, dinfo.HitPart);
                                newPoison.Severity = 0.25f;
                                __instance.health.AddHediff(newPoison);
                            }
                        }
                    }
                }
            }
        }
    }
}