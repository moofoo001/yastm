using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM
{
    public class UsageRestrictionExtension : DefModExtension
    {
        //trait required for usage (e.g., specialized training)
        public TraitDef requiredTrainingTrait;

        //rank list - allowed ranks to use the item
        public List<RankRequirement> allowedRanks;
        
        // message to show if usage is denied
        public string failMessage = "Access denied. Clearance or specialized training required.";
    }

    public class RankRequirement
    {
        public TraitDef rankDef; // eg ST_FederationRank
        public int minDegree;    // eg 1 (Ensign)
    }
}