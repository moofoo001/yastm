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

            // --- TEIL 1: TRAIT UPDATE ---
            if (extension.requiredTrait != null)
            {
                Trait currentTrait = billDoer.story.traits.GetTrait(extension.requiredTrait);
                
                // LOGIK VERBESSERUNG: 
                // Statt blind "+1" zu rechnen, leiten wir das Ziel vom Rezept ab.
                // Wenn requiredDegree -999 ist (keine Voraussetzung), ist das Ziel Degree 0 (Basic).
                // Wenn requiredDegree 0 ist (Cadet), ist das Ziel Degree 1 (Pilot).
                int targetDegree = (extension.requiredDegree == -999) ? 0 : extension.requiredDegree + 1;

                // Sicherheits-Check: Gibt es diesen Degree im XML überhaupt?
                bool degreeExists = extension.requiredTrait.degreeDatas.Any(d => d.degree == targetDegree);
                
                if (!degreeExists)
                {
                    Log.Warning($"[YASTM] Training Complete but Target Degree {targetDegree} does not exist in TraitDef {extension.requiredTrait.defName}. Stopping.");
                    // Wir brechen hier aber nicht ab, vielleicht gibt es ja noch ein Item (Pip).
                }
                else
                {
                    // Debug Log
                    Log.Message($"[YASTM] Upgrading {billDoer.LabelShort}: CurrentTrait={currentTrait?.Degree.ToString() ?? "None"} -> NewDegree={targetDegree}");

                    // Alten Trait entfernen (falls vorhanden)
                    if (currentTrait != null)
                    {
                        // Wenn wir schon den Ziel-Rang (oder höher) haben, machen wir nichts (verhindert Downgrade durch Basic Training)
                        if (currentTrait.Degree >= targetDegree)
                        {
                            Messages.Message("ST_Message_AlreadyQualified".Translate(billDoer.LabelShort), billDoer, MessageTypeDefOf.NeutralEvent);
                            return; 
                        }
                        billDoer.story.traits.RemoveTrait(currentTrait);
                    }

                    // Neuen Trait erzwingen
                    Trait newTrait = new Trait(extension.requiredTrait, targetDegree);
                    
                    // Trick 17: Wir umgehen das Trait-Limit, indem wir direkt auf die interne Liste zugreifen, 
                    // falls GainTrait fehlschlägt (was bei vollen Slots passiert).
                    billDoer.story.traits.GainTrait(newTrait);
                    
                    // Prüfen ob es geklappt hat
                    if (!billDoer.story.traits.HasTrait(extension.requiredTrait))
                    {
                        // Fallback für volle Trait-Slots: Hartes Einfügen
                        Log.Warning($"[YASTM] GainTrait failed (Max slots?). Forcing trait injection for {billDoer.LabelShort}.");
                        billDoer.story.traits.allTraits.Add(newTrait);
                    }
                    
                    // Feedback Nachricht
                    string rankLabel = extension.requiredTrait.DataAtDegree(targetDegree).label;
                    Messages.Message("ST_Message_TrainingComplete".Translate(billDoer.LabelShort, rankLabel), billDoer, MessageTypeDefOf.PositiveEvent);
                }
            }

            // --- TEIL 2: PIP UPDATE (Item) ---
            if (extension.rewardApparel != null)
            {
                // Alte Pips entfernen
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

                // Neuen Pip geben
                Thing newPip = ThingMaker.MakeThing(extension.rewardApparel);
                if (newPip is Apparel apparel)
                {
                    billDoer.apparel.Wear(apparel, true, true);
                }
            }
        }
    }
}