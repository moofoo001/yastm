using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM.Incidents
{
    /// <summary>
    /// adds headiff to pawns
    /// removes headiff when event ends
    /// </summary>
    public class GameCondition_SubspaceAnomaly : GameCondition
    {
        public override void GameConditionTick()
        {
            base.GameConditionTick();
            
            // check every 250 ticks
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                HediffDef anomalyHediff = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Hediff_SubspaceAnomaly");
                if (anomalyHediff == null) return;

                foreach (Map map in this.AffectedMaps)
                {
                    foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                    {
                        // only humanoid pawns
                        if (pawn.RaceProps.Humanlike && pawn.health != null)
                        {
                            if (!pawn.health.hediffSet.HasHediff(anomalyHediff))
                            {
                                pawn.health.AddHediff(anomalyHediff);
                            }
                        }
                    }
                }
            }
        }
        
        // remove headiff when event ends
        public override void End()
        {
            HediffDef anomalyHediff = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Hediff_SubspaceAnomaly");
            
            if (anomalyHediff != null)
            {
                foreach (Map map in this.AffectedMaps)
                {
                    foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                    {
                        if (pawn.health != null)
                        {
                            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(anomalyHediff);
                            if (hediff != null)
                            {
                                pawn.health.RemoveHediff(hediff);
                            }
                        }
                    }
                }
            }
            base.End();
        }
    }
}