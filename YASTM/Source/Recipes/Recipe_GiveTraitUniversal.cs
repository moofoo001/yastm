using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    // mod extension to define which trait to give
    public class DefModExtension_GiveTrait : DefModExtension
    {
        public TraitDef traitDef;
        public int degree = 0;
    }

    // universal recipe worker to give a trait to the billDoer or patient
    public class Recipe_GiveTraitUniversal : RecipeWorker
    {
        // recipe application for bills
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            // training complete for billDoer
            TryGiveTrait(billDoer);
        }

        // recipe application for operations
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            // training complete for patient
            TryGiveTrait(pawn);
        }

        // logic to give the trait
        private void TryGiveTrait(Pawn p)
        {
            if (p == null || p.story == null || p.story.traits == null) return;

            // load recipe extension
            var extension = recipe.GetModExtension<DefModExtension_GiveTrait>();
            
            // validation
            if (extension == null)
            {
                Log.Error($"[YASTM] Recipe {recipe.defName} uses Recipe_GiveTraitUniversal but has no DefModExtension_GiveTrait!");
                return;
            }
            if (extension.traitDef == null) return;

            // check if pawn already has the trait
            if (p.story.traits.HasTrait(extension.traitDef))
            {
                Trait existing = p.story.traits.GetTrait(extension.traitDef);
                
                // already at same or higher degree
                if (existing.Degree >= extension.degree)
                {
                    Messages.Message($"{p.LabelShort} has already completed this training.", p, MessageTypeDefOf.NeutralEvent);
                    return;
                }
                
                // remove existing lower-degree trait
                p.story.traits.RemoveTrait(existing);
            }

            // give the trait
            p.story.traits.GainTrait(new Trait(extension.traitDef, extension.degree));
            
            // notify
            Messages.Message($"Training Complete: {p.LabelShort} has gained the trait {extension.traitDef.LabelCap}.", p, MessageTypeDefOf.PositiveEvent);
        }
    }
}