using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    public class Recipe_TraitUpgrade : RecipeWorker
    {
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);

            var extension = recipe.GetModExtension<ST_RecipeExtension>();
            if (extension == null)
            {
                Log.Error($"[YASTM] Recipe {recipe.defName} is missing ST_RecipeExtension!");
                return;
            }

            // ---TRAIT UPDATE ---
            if (extension.requiredTrait != null)
            {
                Trait currentTrait = billDoer.story.traits.GetTrait(extension.requiredTrait);
                
                // logic to determine target degree
                int targetDegree = (extension.requiredDegree == -999) ? 0 : extension.requiredDegree + 1;

                // safe check: does the target degree exist?
                bool degreeExists = extension.requiredTrait.degreeDatas.Any(d => d.degree == targetDegree);
                
                if (!degreeExists)
                {
                    Log.Warning($"[YASTM] Training Complete but Target Degree {targetDegree} does not exist in TraitDef {extension.requiredTrait.defName}. Stopping.");
                    // Feedback message
                }
                else
                {
                    // Debug Log
                    Log.Message($"[YASTM] Upgrading {billDoer.LabelShort}: CurrentTrait={currentTrait?.Degree.ToString() ?? "None"} -> NewDegree={targetDegree}");

                    // Remove current trait if exists
                    if (currentTrait != null)
                    {
                        // Check if already at or above target degree
                        if (currentTrait.Degree >= targetDegree)
                        {
                            Messages.Message("ST_Message_AlreadyQualified".Translate(billDoer.LabelShort), billDoer, MessageTypeDefOf.NeutralEvent);
                            return; 
                        }
                        billDoer.story.traits.RemoveTrait(currentTrait);
                    }

                    // Add new trait degree
                    Trait newTrait = new Trait(extension.requiredTrait, targetDegree);
                    
                    // add the trait 
                    billDoer.story.traits.GainTrait(newTrait);
                    
                    // double check: did it work?
                    if (!billDoer.story.traits.HasTrait(extension.requiredTrait))
                    {
                        Log.Warning($"[YASTM] GainTrait failed (Max slots?). Forcing trait injection for {billDoer.LabelShort}.");
                        billDoer.story.traits.allTraits.Add(newTrait);
                    }
                    
                    // Feedback message
                    string rankLabel = extension.requiredTrait.DataAtDegree(targetDegree).label;
                    Messages.Message("ST_Message_TrainingComplete".Translate(billDoer.LabelShort, rankLabel), billDoer, MessageTypeDefOf.PositiveEvent);
                }
            }

            // ---REWARD APPAREL---
            if (extension.rewardApparel != null)
            {
                // remove old pip if tag specified
                if (!string.IsNullOrEmpty(extension.removeApparelWithTag))
                {
                    var oldPips = billDoer.apparel.WornApparel
                        .Where(a => a.def.apparel.tags != null && a.def.apparel.tags.Contains(extension.removeApparelWithTag))
                        .ToList();

                    foreach (var oldPip in oldPips)
                    {
                        billDoer.apparel.Remove(oldPip);
                        oldPip.Destroy();
                    }
                }

                // give new pip
                Thing newPip = ThingMaker.MakeThing(extension.rewardApparel);
                if (newPip is Apparel apparel)
                {
                    billDoer.apparel.Wear(apparel, true, true);
                }
            }
        }
    }
}