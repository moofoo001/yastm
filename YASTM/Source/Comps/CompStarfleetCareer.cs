using Verse;

namespace YASTM
{
    public class CompProperties_StarfleetCareer : CompProperties
    {
        public CompProperties_StarfleetCareer()
        {
            compClass = typeof(CompStarfleetCareer);
        }
    }

    public class CompStarfleetCareer : ThingComp
    {
        public int lastPromotionTick;        
        public int completedPromotionQuests;  

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastPromotionTick, "ST_lastPromotionTick", 0);
            Scribe_Values.Look(ref completedPromotionQuests, "ST_completedPromotionQuests", 0);
        }

        public float DaysSinceLastPromotion
        {
            get
            {
                int now = Find.TickManager.TicksGame;
                const float TicksPerDay = 60000f; 
                if (lastPromotionTick <= 0) return 9999f; 
                return (now - lastPromotionTick) / TicksPerDay;
            }
        }

        public void MarkPromotedNow()
        {
            lastPromotionTick = Find.TickManager.TicksGame;
        }

        public void IncrementPromotionQuestCount(int amount = 1)
        {
            completedPromotionQuests += amount;
            if (completedPromotionQuests < 0) completedPromotionQuests = 0;
        }
    }
}

