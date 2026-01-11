using System;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompStarfleetCareer : ThingComp
    {
       
        public ST_RankDef CurrentRank;

        // --- ExposeData ---
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Defs.Look(ref CurrentRank, "ST_CurrentRank");
        }

        public override string CompInspectStringExtra()
        {
            if (CurrentRank != null)
            {
                return "ST_RankLabel".Translate() + ": " + CurrentRank.LabelCap;
            }
            return null;
        }

        // promote pawn to new rank
        public void Promote(ST_RankDef newRank)
        {
            this.CurrentRank = newRank;

        }
    }
}