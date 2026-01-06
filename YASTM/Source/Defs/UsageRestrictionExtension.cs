using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM
{
    public class UsageRestrictionExtension : DefModExtension
    {
        // 1. FACHAUSBILDUNG (Optional)
        // Trait, den man via Holodisc gelernt haben muss (z.B. "ST_Training_Comms")
        public TraitDef requiredTrainingTrait;

        // 2. RANG-LISTE (Optional)
        // Einer dieser Ränge muss erfüllt sein.
        public List<RankRequirement> allowedRanks;
        
        // Fehlernachricht für den Spieler
        public string failMessage = "Access denied. Clearance or specialized training required.";
    }

    public class RankRequirement
    {
        public TraitDef rankDef; // z.B. ST_FederationRank
        public int minDegree;    // z.B. 1 (Ensign)
    }
}