// Source/Promotion/PromotionRequirementsExtension.cs
using Verse;

namespace YASTM
{

    public class PromotionRequirementsExtension : DefModExtension
    {
       
        public string fromRankTraitDefName;

        
        public int requiredMedOrScienceScans = 1;
        public int requiredSecuritySweeps = 1;
        public int requiredDiplomacyActions = 1;

     
        public int minDaysSinceLastPromotion = 0;
        public bool uniqueColonyWide = false;
        public int goodwillMin = 0;

     
        public string factionDefName = null;

  
        public int requiredCompletedQuests = 0;

     
        public string pipApparelDefName = null;
    }
}

