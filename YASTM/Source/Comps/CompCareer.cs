using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_Career : CompProperties
    {
        public CompProperties_Career()
        {
            this.compClass = typeof(CompCareer);
        }
    }

    public class CompCareer : ThingComp
    {
        public int ticksInCurrentRank = 0;
        
        // Das universelle Punktekonto
        private Dictionary<string, int> pointTracker = new Dictionary<string, int>();

        public CompProperties_Career Props => (CompProperties_Career)props;

        // --- API ---
        
        public void AddCareerPoint(string category, int amount = 1)
        {
            if (!pointTracker.ContainsKey(category))
            {
                pointTracker[category] = 0;
            }
            pointTracker[category] += amount;
            
            // Sofort prüfen
            TryPromote();
        }

        public int GetPoints(string category)
        {
            if (pointTracker.TryGetValue(category, out int val)) return val;
            return 0;
        }

        // --- LOGIK ---

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (parent is Pawn p && !p.Dead)
            {
                ticksInCurrentRank += 250;
                if (ticksInCurrentRank % 60000 == 0) TryPromote(); 
            }
        }

        public void TryPromote()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null || pawn.story == null) return;

            // Finde die CareerDef
            CareerDef career = DefDatabase<CareerDef>.AllDefsListForReading
                .FirstOrDefault(c => pawn.story.traits.HasTrait(c.trait));

            if (career == null) return;

            Trait currentTrait = pawn.story.traits.GetTrait(career.trait);
            int currentDegree = currentTrait?.Degree ?? -1;

            foreach (var stage in career.stages)
            {
                // Nur der nächste Rang (oder Einstieg)
                if (stage.targetDegree == currentDegree + 1)
                {
                    if (CheckRequirements(pawn, stage.requirements))
                    {
                        PromoteTo(pawn, career.trait, stage.targetDegree);
                        break; 
                    }
                }
            }
        }

        private bool CheckRequirements(Pawn p, CareerRequirements req)
        {
            if (req == null) return true;

            // 1. Zeit
            float yearsServed = ticksInCurrentRank / (60000f * 60f); // 60 Tage/Jahr
            if (yearsServed < req.timeInRankYears) return false;

            // 2. Skills
            if (GetSkill(p, SkillDefOf.Social) < req.minSocialSkill) return false;
            if (GetSkill(p, SkillDefOf.Intellectual) < req.minIntellectualSkill) return false;
            if (GetSkill(p, SkillDefOf.Shooting) < req.minShootingSkill) return false;
            if (GetSkill(p, SkillDefOf.Melee) < req.minMeleeSkill) return false;

            // 3. Punkte
            if (req.careerPoints != null)
            {
                foreach (var pointReq in req.careerPoints)
                {
                    if (GetPoints(pointReq.category) < pointReq.count) return false;
                }
            }

            return true;
        }

        private int GetSkill(Pawn p, SkillDef skill) => p.skills?.GetSkill(skill)?.Level ?? 0;

        private void PromoteTo(Pawn p, TraitDef traitDef, int newDegree)
        {
            ticksInCurrentRank = 0; // Reset Zeit
            // pointTracker.Clear(); // Optional: Punkte behalten oder resetten? Hier: behalten.
            
            Trait existing = p.story.traits.GetTrait(traitDef);
            if (existing != null) p.story.traits.RemoveTrait(existing);
            p.story.traits.GainTrait(new Trait(traitDef, newDegree));

            Messages.Message("ST_Message_Promoted".Translate(p.LabelShort, traitDef.DataAtDegree(newDegree).label), p, MessageTypeDefOf.PositiveEvent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksInCurrentRank, "ticksInRank", 0);
            Scribe_Collections.Look(ref pointTracker, "careerPoints", LookMode.Value, LookMode.Value);
            if (pointTracker == null) pointTracker = new Dictionary<string, int>();
        }
    }
}