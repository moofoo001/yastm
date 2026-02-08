using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM
{
    public class ST_CareerDef : Def
    {
        public TraitDef requiredTrait; // War "trait"
        
        // Für Kompatibilität mit alten XMLs, die noch <trait> nutzen
        public TraitDef trait; 

        public List<CareerStage> stages;

        // AUTOMATISCHE REPARATUR BEIM LADEN
        public override void ResolveReferences()
        {
            base.ResolveReferences();
            
            // 1. Trait Fix (falls XML "trait" nutzte, schieben wir es nach "requiredTrait")
            if (requiredTrait == null && trait != null) requiredTrait = trait;

            // 2. Rank Fix (targetDegree -> ST_RankDef)
            if (stages != null)
            {
                foreach (var stage in stages)
                {
                    // Wenn kein Rang definiert ist, aber eine Nummer (targetDegree) da ist...
                    if (stage.rank == null && stage.targetDegree > 0)
                    {
                        // ...suchen wir den passenden Rang in der Datenbank!
                        stage.rank = DefDatabase<ST_RankDef>.AllDefsListForReading
                            .Find(r => r.level == stage.targetDegree);
                    }
                }
            }
        }
    }

    public class CareerStage
    {
        public ST_RankDef rank; 
        
        // WICHTIG: Das hier wieder eingefügt, damit alte XMLs nicht crashen!
        public int targetDegree; 
        
        public string label;
        public CareerRequirements requirements;
    }

    public class CareerRequirements
    {
        public float timeInRankYears = 0f;
        public int minSocialSkill = 0;
        public int minIntellectualSkill = 0;
        public int minShootingSkill = 0;
        public int minMeleeSkill = 0;
        public List<CareerPointRequirement> careerPoints;
    }

    public class CareerPointRequirement
    {
        public string category; 
        public int count;       
    }
}