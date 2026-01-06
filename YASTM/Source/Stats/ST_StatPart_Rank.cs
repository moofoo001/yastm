using RimWorld;
using Verse;

namespace YASTM
{
    public class ST_StatPart_Rank : StatPart
    {
        // Diese Werte werden im XML definiert
        public float rankFactor = 1.1f; // +10% pro Rang-Level
        
        // Prüft, ob der Pawn einen Rang hat (wir nehmen an, du hast eine Component dafür)
        protected ST_RankDef GetRank(Pawn pawn)
        {
            // Hier greifen wir auf deine existierende Crew-Logik zu (fiktiv: CompStarfleetCareer)
            var careerComp = pawn.TryGetComp<CompStarfleetCareer>();
            return careerComp?.CurrentRank;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is not Pawn pawn) return;

            var rank = GetRank(pawn);
            if (rank != null)
            {
                // Beispiel: Basis-Wert * (1 + (RangLevel * 0.1))
                // Level 1 (Ensign) = 1.1x
                // Level 5 (Captain) = 1.5x
                float multiplier = 1f + (rank.level * (rankFactor - 1f));
                val *= multiplier;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.Thing is not Pawn pawn) return null;
            
            var rank = GetRank(pawn);
            if (rank != null)
            {
                float multiplier = 1f + (rank.level * (rankFactor - 1f));
                return "ST_RankModifier".Translate(rank.LabelCap) + ": x" + multiplier.ToStringPercent();
            }
            return null;
        }
    }
}