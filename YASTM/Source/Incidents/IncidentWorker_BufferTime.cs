using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace YASTM
{
    public class IncidentWorker_BufferTime : IncidentWorker
    {
        private const string TRAIT_LOWER_DECKER = "ST_LowerDecker"; 

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            Map map = (Map)parms.target;
            
            // 1. Gibt es genug Lower Deckers?
            List<Pawn> lowerDeckers = GetLowerDeckers(map).ToList();
            if (lowerDeckers.Count < 2) return false;

            // 2. Gibt es einen Ort zum Feiern?
            // KORREKTUR: Argument 2 ist GatheringDef, nicht Map!
            if (!RCellFinder.TryFindGatheringSpot(lowerDeckers[0], GatheringDefOf.Party, false, out IntVec3 spot)) 
                return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // 1. Crew sammeln
            List<Pawn> lowerDeckers = GetLowerDeckers(map).ToList();
            if (lowerDeckers.Count < 2) return false;

            // Einen "Anführer" für die Party bestimmen
            Pawn organizer = lowerDeckers.RandomElement();

            // 2. Ort finden
            // KORREKTUR: Argument 2 ist GatheringDef, nicht Map!
            if (!RCellFinder.TryFindGatheringSpot(organizer, GatheringDefOf.Party, false, out IntVec3 spot)) 
                return false;

            // 3. Party starten!
            LordJob_Joinable_Party lordJob = new LordJob_Joinable_Party(spot, organizer, GatheringDefOf.Party);
            
            LordMaker.MakeNewLord(map.ParentFaction, lordJob, map, lowerDeckers);

            // 4. Hediffs & Thoughts verteilen
            foreach (Pawn p in lowerDeckers)
            {
                // Hediff hinzufügen
                p.health.AddHediff(HediffDef.Named("ST_BufferTime_Bliss"));
                
                // Thought hinzufügen
                p.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("ST_BufferTime_Mood"));

                // Laufende Jobs abbrechen
                if (p.CurJob != null) p.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
            }

            // 5. Nachricht an Spieler
            SendStandardLetter(parms, lowerDeckers);
            
            return true;
        }

        private IEnumerable<Pawn> GetLowerDeckers(Map map)
        {
            TraitDef lowerDeckerDef = DefDatabase<TraitDef>.GetNamedSilentFail(TRAIT_LOWER_DECKER);
            
            if (lowerDeckerDef == null)
            {
                yield break;
            }

            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (!p.Dead && !p.Downed && !p.InMentalState && p.Awake() && 
                    p.story.traits.HasTrait(lowerDeckerDef))
                {
                    yield return p;
                }
            }
        }
    }
}