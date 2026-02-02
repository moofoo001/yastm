using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace YASTM
{
    // Part 1: scenario part marker (only for scenario defs)
    public class ScenPart_KlingonRiteMarker : ScenPart
    {
        public override void Randomize() { }
        public override void DoEditInterface(Listing_ScenEdit listing) 
        {
            if (listing.ButtonText("Klingon Rite Active")) { }
        }
    }

    // Part 2: GameComponent to handle the rite logic
    public class GameComponent_KlingonRite : GameComponent
    {
        // Constants
        private const int RiteDurationDays = 100;
        private const int RaidIntervalDays = 12; 
        
        private int ticksPassed = 0;
        private bool riteActive = false;
        private bool riteCompleted = false;

        public GameComponent_KlingonRite(Game game) { }

        public override void FinalizeInit()
        {
            // Check if the scenario includes the Klingon Rite marker
            if (Find.Scenario != null && Find.Scenario.AllParts.Any(p => p is ScenPart_KlingonRiteMarker))
            {
                riteActive = true;
            }
        }

        public override void GameComponentTick()
        {
            if (!riteActive || riteCompleted) return;

            ticksPassed++;
            if (ticksPassed % 250 != 0) return; // Performance Check

            // raid every RaidIntervalDays
            if (ticksPassed % (RaidIntervalDays * 60000) == 0) 
            {
                TriggerRiteRaid();
            }

            // check for rite completion
            if (ticksPassed >= RiteDurationDays * 60000)
            {
                StartAscensionQuest();
            }
        }

        private void TriggerRiteRaid()
        {
            Map map = Find.AnyPlayerHomeMap; 
            if (map == null) return;

            float progress = (float)ticksPassed / (float)(RiteDurationDays * 60000);
            float pointsMultiplier = 1.0f + (progress * 2.5f); 

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.points *= pointsMultiplier;
            parms.customLetterLabel = "ST_RiteRaidLabel".Translate();
            parms.customLetterText = "ST_RiteRaidDesc".Translate();

            IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
        }

        private void StartAscensionQuest()
        {
            riteActive = false;
            riteCompleted = true;

            // reward generation
            Thing weapon = ThingMaker.MakeThing(ThingDef.Named("KL_Weapon_Batleth"), GenStuff.DefaultStuffFor(ThingDef.Named("KL_Weapon_Batleth")));
            weapon.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);

            // create quest
            Slate slate = new Slate();
            slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(Find.AnyPlayerHomeMap) * 2f); 
            slate.Set("itemStashSingleThing", weapon); 
            
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(
                DefDatabase<QuestScriptDef>.GetNamed("OpportunitySite_ItemStash"), 
                slate
            );

            // reward Klingon rank to all Klingon crew members
            foreach (Pawn p in ST_CrewUtility.GetAllActiveCrewMembers())
            {
                // only Klingons without existing rank
                Trait existing = p.story?.traits?.GetTrait(TraitDef.Named("ST_KlingonRank"));
                if (existing == null && (p.def.defName.Contains("Klingon") || p.kindDef.defName.Contains("Klingon"))) 
                {
                     p.story.traits.GainTrait(new Trait(TraitDef.Named("ST_KlingonRank"), 0)); // Bekk
                }
            }

            // send letter
            Find.LetterStack.ReceiveLetter(
                "ST_RiteQuestLabel".Translate(),    // Label
                "ST_RiteQuestDesc".Translate(),     // Text
                LetterDefOf.PositiveEvent,          // LetterDef
                null,                               // LookTargets
                null,                               // Faction
                quest                               // Quest
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksPassed, "ST_RiteTicks", 0);
            Scribe_Values.Look(ref riteActive, "ST_RiteActive", false);
            Scribe_Values.Look(ref riteCompleted, "ST_RiteCompleted", false);
        }
    }
}