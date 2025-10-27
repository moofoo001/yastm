// Source/Promotion/PromotionRequirementsExtension.cs
using Verse;

namespace YASTM
{
    /// <summary>
    /// ModExtension, die an den ZIEL-Rang (TraitDef) gehängt wird.
    /// Steuert: von welchem Rang befördert wird + welche Objectives/Zusatzgates gelten.
    /// </summary>
    public class PromotionRequirementsExtension : DefModExtension
    {
        // Von welchem Rang (TraitDef.defName) kommt die Beförderung?
        public string fromRankTraitDefName;

        // Objectives (Zähler): Medical ODER Science zählt hier hinein.
        public int requiredMedOrScienceScans = 1;
        public int requiredSecuritySweeps = 1;
        public int requiredDiplomacyActions = 1;

        // Zusätzliche Gates (optional)
        public int minDaysSinceLastPromotion = 0;
        public bool uniqueColonyWide = false;
        public int goodwillMin = 0;

        // Ziel-Fraktion, gegen die goodwillMin geprüft wird (z. B. "ST_Starfleet").
        // Wenn leer/null, kann PromotionGating.cs einen Default verwenden.
        public string factionDefName = null;

        // Mindestanzahl abgeschlossener (relevanter) Quests.
        public int requiredCompletedQuests = 0;

        // Optional: direktes Mapping auf das Pip-Item (ThingDef.defName)
        public string pipApparelDefName = null;
    }
}
