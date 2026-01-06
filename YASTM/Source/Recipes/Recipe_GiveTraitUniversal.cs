using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    // Die Extension bleibt gleich
    public class DefModExtension_GiveTrait : DefModExtension
    {
        public TraitDef traitDef;
        public int degree = 0;
    }

    // Wir erben von RecipeWorker (Basisklasse für beides)
    public class Recipe_GiveTraitUniversal : RecipeWorker
    {
        // 1. DIESE METHODE FEHLTE: Wird aufgerufen, wenn ein "Bill" an einer Werkbank fertig ist (Holodeck)
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            // Derjenige, der das Rezept ausgeführt hat, bekommt den Trait
            TryGiveTrait(billDoer);
        }

        // 2. Diese Methode ist für "Operationen" (Falls du es mal als Medical Bill nutzt)
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            // Bei Operationen bekommt der PATIENT (pawn) den Trait, nicht der Arzt (billDoer)
            TryGiveTrait(pawn);
        }

        // Die gemeinsame Logik
        private void TryGiveTrait(Pawn p)
        {
            if (p == null || p.story == null || p.story.traits == null) return;

            // Extension laden
            var extension = recipe.GetModExtension<DefModExtension_GiveTrait>();
            
            // Sicherheitscheck: Hat das Rezept die Extension?
            if (extension == null)
            {
                Log.Error($"[YASTM] Recipe {recipe.defName} uses Recipe_GiveTraitUniversal but has no DefModExtension_GiveTrait!");
                return;
            }
            if (extension.traitDef == null) return;

            // Hat er den Trait schon?
            if (p.story.traits.HasTrait(extension.traitDef))
            {
                Trait existing = p.story.traits.GetTrait(extension.traitDef);
                
                // Wenn gleicher oder höherer Grad -> Nachricht und Abbruch
                if (existing.Degree >= extension.degree)
                {
                    Messages.Message($"{p.LabelShort} has already completed this training.", p, MessageTypeDefOf.NeutralEvent);
                    return;
                }
                
                // Wenn niedrigerer Grad (Upgrade) -> Alten entfernen
                p.story.traits.RemoveTrait(existing);
            }

            // Neuen Trait vergeben
            p.story.traits.GainTrait(new Trait(extension.traitDef, extension.degree));
            
            // Feedback
            Messages.Message($"Training Complete: {p.LabelShort} has gained the trait {extension.traitDef.LabelCap}.", p, MessageTypeDefOf.PositiveEvent);
        }
    }
}