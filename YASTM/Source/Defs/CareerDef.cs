using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CareerDef : Def
    {
        // BEDINGUNGEN: Wer bekommt diese Karriere?
        public List<string> requiredXenotypes; // z.B. "Ectos_Klingon"
        public List<string> requiredFactions;  // z.B. "ST_Federation"

        // LOGIK: Welche Punktekategorie zählt hier?
        // "Combat" (Kills), "Trade" (Silber), "Science" (Scans/Forschung), "Time" (Dienstzeit)
        public string pointCategory = "Combat"; 

        // RÄNGE: Die Leiter nach oben
        public List<CareerRank> ranks;
    }

    public class CareerRank
    {
        public float threshold;        // Benötigte Punkte (z.B. 10)
        public TraitDef rewardTrait;   // Welches Trait gibt es?
        public int rewardDegree = 0;   // Welches Degree (für Klingonen wichtig)?
        public string label;           // Name für die Benachrichtigung
    }
}