using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    public class GameComponent_RankPipSync : GameComponent
    {
        private int tickCounter = 0;
        private const int CheckInterval = 2500; 

        public GameComponent_RankPipSync(Game game) { }

        public override void GameComponentTick()
        {
            tickCounter++;
            if (tickCounter >= CheckInterval)
            {
                SyncAllRankPips();
                tickCounter = 0;
            }
        }

        public void SyncAllRankPips()
        {
            // check all crew members
            foreach (Pawn pawn in ST_CrewUtility.GetAllActiveCrewMembers())
            {
                if (pawn.Destroyed || pawn.Dead || pawn.apparel == null) continue;

                SyncPawnPip(pawn);
            }
        }

        private void SyncPawnPip(Pawn pawn)
        {
            // get rank trait and corresponding data
            Trait rankTrait = null;
            RankVisualExtension extension = null;
            RankData currentRankData = null;

            if (pawn.story?.traits?.allTraits == null) return;

            foreach (var trait in pawn.story.traits.allTraits)
            {
                var ext = trait.def.GetModExtension<RankVisualExtension>();
                if (ext != null)
                {
                    rankTrait = trait;
                    extension = ext;
                    // set current rank data
                    currentRankData = ext.ranks?.FirstOrDefault(r => r.degree == trait.Degree);
                    break; // found rank trait
                }
            }

            // no rank found, remove pips
            if (currentRankData == null)
            {
                RemoveAllPips(pawn);
                return;
            }

            // which pip to wear?
            string targetDefName = !string.IsNullOrEmpty(currentRankData.specificDefName) 
                ? currentRankData.specificDefName 
                : "ST_Apparel_Pip_" + currentRankData.texName;

            ThingDef targetPipDef = DefDatabase<ThingDef>.GetNamedSilentFail(targetDefName);

            if (targetPipDef == null)
            {
                // Fallback: log warning and skip
                // Log.Warning($"[YASTM] Could not find Pip ThingDef named: {targetDefName}");
                return;
            }

            // check worn pips
            bool correctPipWorn = false;
            List<Apparel> pipsToRemove = new List<Apparel>();

            foreach (var worn in pawn.apparel.WornApparel)
            {
                // is it a rank pip?
                if (worn.def.apparel?.tags != null && worn.def.apparel.tags.Contains("ST_RankPip"))
                {
                    if (worn.def == targetPipDef)
                    {
                        correctPipWorn = true;
                    }
                    else
                    {
                        // wrong pip, mark for removal
                        pipsToRemove.Add(worn);
                    }
                }
            }

            // remove wrong pips
            foreach (var oldPip in pipsToRemove)
            {
                pawn.apparel.Remove(oldPip);
                oldPip.Destroy(); // destroy to avoid clutter
            }

            // wear correct pip if not already worn
            if (!correctPipWorn)
            {
                Apparel newPip = (Apparel)ThingMaker.MakeThing(targetPipDef);
                if (newPip != null)
                {
                    pawn.apparel.Wear(newPip, true, true); // forceWear = true
                }
            }
        }

        private void RemoveAllPips(Pawn pawn)
        {
            var pips = pawn.apparel.WornApparel
                .Where(a => a.def.apparel?.tags != null && a.def.apparel.tags.Contains("ST_RankPip"))
                .ToList();

            foreach (var pip in pips)
            {
                pawn.apparel.Remove(pip);
                pip.Destroy();
            }
        }
    }
}