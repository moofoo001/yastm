using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CareerDef : Def
    {
        public TraitDef trait;
        public List<CareerStage> stages;
    }

    public class CareerStage
    {
        public int targetDegree;
        public CareerRequirements requirements;
    }

    public class CareerRequirements
    {
        public float timeInRankYears = 0f;
        
        // Skill requirements
        public int minSocialSkill = 0;
        public int minIntellectualSkill = 0;
        public int minShootingSkill = 0;
        public int minMeleeSkill = 0;


        // XML: <careerPoints><li><category>ScienceScan</category><count>5</count></li></careerPoints>
        public List<CareerPointRequirement> careerPoints;
    }

    public class CareerPointRequirement
    {
        public string category; // eg "ScienceScan", "CombatKill"
        public int count;       // level required
    }
}