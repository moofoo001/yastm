using RimWorld;
using Verse;

namespace YASTM
{
    public class ST_StatPart_Rank : StatPart
    {
        // factor Rank level (eg 1.1 = +10% pro Level)
        public float rankFactor = 1.1f; // +10% per rank level
        
        // get rank of pawn
        protected ST_RankDef GetRank(Pawn pawn)
        {
            // assuming we have a CompStarfleetCareer
            var careerComp = pawn.TryGetComp<CompStarfleetCareer>();
            return careerComp?.CurrentRank;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is not Pawn pawn) return;

            var rank = GetRank(pawn);
            if (rank != null)
            {
                // apply multiplier
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