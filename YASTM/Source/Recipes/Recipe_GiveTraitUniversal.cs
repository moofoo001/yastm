using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    // 1. Die Daten-Struktur für das XML
    // Damit können wir im XML sagen: <traitDef>ST_Pilot</traitDef>
    public class DefModExtension_GiveTrait : DefModExtension
    {
        public TraitDef traitDef;
        public int degree = 0; // Optional: Für Traits mit Stufen (z.B. Neurotisch 1 vs 2), Standard 0
        public bool replaceConflicting = false; // Soll ein widersprüchlicher Trait (z.B. Faul) entfernt werden?
    }

    // 2. Der Worker, der das Rezept ausführt
    public class Recipe_GiveTraitUniversal : RecipeWorker
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            // Der "billDoer" ist derjenige, der im Holodeck trainiert
            if (billDoer == null || billDoer.story == null) return;

            // Wir holen uns die Infos aus dem XML des Rezepts
            var extension = recipe.GetModExtension<DefModExtension_GiveTrait>();

            if (extension == null || extension.traitDef == null)
            {
                Log.Error($"[YASTM] Recipe {recipe.defName} uses Recipe_GiveTraitUniversal but has no DefModExtension_GiveTrait or missing traitDef.");
                return;
            }

            // Check: Hat er den Trait schon?
            if (billDoer.story.traits.HasTrait(extension.traitDef))
            {
                // Optional: Nachricht "Hat schon gelernt"
                Messages.Message($"{billDoer.LabelShort} already knows this training.", billDoer, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Konflikt-Check (Vanilla Logik würde hier warnen, wir erzwingen es wenn gewünscht)
            // Wir erstellen den neuen Trait
            Trait newTrait = new Trait(extension.traitDef, extension.degree);

            // Wir fügen ihn hinzu
            billDoer.story.traits.GainTrait(newTrait);
            
            // Erfolgsmeldung
            Messages.Message($"Simulation complete! {billDoer.LabelShort} has gained the trait: {extension.traitDef.degreeDatas[0].label}.", billDoer, MessageTypeDefOf.PositiveEvent);
        }
    }
}