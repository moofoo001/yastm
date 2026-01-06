using System;
using Verse;
using RimWorld;

namespace YASTM
{
    // Diese Def definiert Ränge wie "Ensign", "Lieutenant", "Captain"
    public class ST_RankDef : Def
    {
        public int level; // 1 = Ensign, 5 = Captain, etc.
        public float salaryAmount; // Credits oder Energie-Credits pro Quartal
        public bool isCommandStaff; // Darf Brücken-Konsolen voll nutzen
        
        // Optional: Skill-Voraussetzungen für die Beförderung
        public SkillDef requiredSkill;
        public int requiredSkillLevel;

        public ST_RankDef() 
        {
            // Verhindert Fehler bei Sonderzeichen in Labels
            ignoreIllegalLabelCharacterConfigError = true;
        }
    }
}