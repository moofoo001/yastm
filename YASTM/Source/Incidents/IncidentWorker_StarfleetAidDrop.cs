using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class IncidentWorker_StarfleetAidDrop : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            var map = parms.target as Map;
            if (map == null) return false;
            
            IntVec3 spot = DropCellFinder.TradeDropSpot(map);
            return spot.IsValid;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = parms.target as Map;
            if (map == null) return false;

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            if (!dropSpot.IsValid)
                dropSpot = map.Center;

            var tsm = DefDatabase<ThingSetMakerDef>.GetNamedSilentFail("ST_StarfleetAid_ThingSet");
            if (tsm == null || tsm.root == null)
            {
                Messages.Message("ST_Aid: ThingSetMaker missing.", MessageTypeDefOf.RejectInput);
                return false;
            }

            List<Thing> things = tsm.root.Generate();

            
            float totalValue = 0f;
            foreach (var t in things) totalValue += t.MarketValue * t.stackCount;
            if (totalValue > 1200f)
                foreach (var t in things) t.stackCount = Mathf.Max(1, Mathf.RoundToInt(t.stackCount * 0.75f));

            
            DropPodUtility.DropThingsNear(dropSpot, map, things);

            string label = "ST_Aid_LetterLabel".Translate();
            string text  = "ST_Aid_LetterText".Translate();
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new TargetInfo(dropSpot, map));

            return true;
        }
    }
}

