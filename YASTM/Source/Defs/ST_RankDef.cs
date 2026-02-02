using System;
using Verse;
using RimWorld;

namespace YASTM
{
    // rank definition for crew members
    public class ST_RankDef : Def
    {
        public int level; // 1 = Ensign, 5 = Captain, etc.
        public float salaryAmount; // monthly salary
        public bool isCommandStaff; // is part of command staff
        
        // required skill and level to attain this rank
        public SkillDef requiredSkill;
        public int requiredSkillLevel;

        public ST_RankDef() 
        {
            
            ignoreIllegalLabelCharacterConfigError = true;
        }
    }
}