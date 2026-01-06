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
            if (extension == null) return;

            // --- TEIL 1: TRAIT UPDATE (Wie zuvor) ---
            if (extension.requiredTrait != null)
            {
                Trait currentTrait = billDoer.story.traits.GetTrait(extension.requiredTrait);
                int newDegree = (currentTrait != null) ? currentTrait.Degree + 1 : 0;

                if (currentTrait != null) billDoer.story.traits.RemoveTrait(currentTrait);

                if (extension.requiredTrait.degreeDatas != null && 
                    extension.requiredTrait.degreeDatas.Any(d => d.degree == newDegree))
                {
                    billDoer.story.traits.GainTrait(new Trait(extension.requiredTrait, newDegree));
                }
            }

            // --- TEIL 2: PIP UPDATE (Das Visuelle) ---
            if (extension.rewardApparel != null)
            {
                // 1. Alte Pips entfernen
                if (!string.IsNullOrEmpty(extension.removeApparelWithTag))
                {
                    // Wir suchen alles, was der Pawn trägt und den Tag hat
                    var oldPips = billDoer.apparel.WornApparel
                        .Where(a => a.def.apparel.tags != null && a.def.apparel.tags.Contains(extension.removeApparelWithTag))
                        .ToList(); // ToList ist wichtig, da wir die Collection modifizieren

                    foreach (var oldPip in oldPips)
                    {
                        // Ausziehen und zerstören (oder ins Inventar legen, hier: zerstören für Sauberkeit)
                        billDoer.apparel.Remove(oldPip);
                        oldPip.Destroy(); 
                    }
                }

                // 2. Neuen Pip generieren
                Thing newPip = ThingMaker.MakeThing(extension.rewardApparel, GenStuff.DefaultStuffFor(extension.rewardApparel));
                if (newPip is not Apparel apparel) 
                {
                    Log.Error($"[YASTM] {extension.rewardApparel} is defined as reward but is not Apparel!");
                    return;
                }

                // 3. Pip anziehen (ForceWear sorgt dafür, dass er nicht automatisch ausgezogen wird)
                billDoer.apparel.Wear(apparel, true, true);
                
                // Feedback
                Messages.Message("ST_Message_RankPipAwarded".Translate(billDoer.LabelShort, apparel.Label), billDoer, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}