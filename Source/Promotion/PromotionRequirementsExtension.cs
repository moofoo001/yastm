using Verse;

namespace YASTM
{
    // An diese Extension hängen wir die Anforderungen direkt an eure Rang-Traits.
    public class PromotionRequirementsExtension : DefModExtension
    {
        public string factionDefName = "ST_Starfleet"; // Fraktion, gegen deren Goodwill geprüft wird
        public int goodwillMin = 0;                    // Mindest-Goodwill
        public int requiredCompletedQuests = 0;        // Mindestanzahl erfolgreich beendeter Quests (gesamt)
        public string requiredQuestTag = null;         // Optional: nur Quests mit diesem Tag zählen
        public int minDaysSinceLastPromotion = 0;      // Mindesttage seit letzter Beförderung (Pawn-individuell)
        public bool uniqueColonyWide = false;          // true => nur 1 Kolonist darf diesen Rang tragen (z.B. Captain)
    }
}
