using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    public class Recipe_AddTransporterTrait : RecipeWorker
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {

            if (billDoer != null)
            {
                if (billDoer.story != null && !billDoer.story.traits.HasTrait(ST_TraitDefOf.ST_TransporterEngineer))
                {
                    billDoer.story.traits.GainTrait(new Trait(ST_TraitDefOf.ST_TransporterEngineer));
                    Messages.Message($"{billDoer.LabelShort} is now qualified as a Transporter Chief!", billDoer, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
    }
}