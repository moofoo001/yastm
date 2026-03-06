using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace YASTM.Incidents
{
    public class IncidentWorker_UnwantedGuest : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Checks if the quest exists and can be fired
            return base.CanFireNowSub(parms) && DefDatabase<QuestScriptDef>.GetNamedSilentFail("ST_Quest_BabysitWesley") != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            string title = "Starfleet Emergency: Unwanted Guest";
            string text = "Captain, a Federation vessel in the sector has suffered a critical warp core breach! They need to evacuate all 'non-essential' cargo immediately to save the ship. They are requesting we take in their cargo for exactly 10 days until a rescue ship arrives.\n\nThey offer two options:\n\nOption A: Acting Ensign 'Wesley'. An absolute genius at the research bench, but incredibly annoying. He will severely test the nerves of your colonists.\n\nOption B: A crate of Tribbles. Absolutely harmless and incredibly cute, but they multiply rapidly and will eat through your food supplies.\n\nWhat are your orders?";

            DiaNode node = new DiaNode(text);

            // Option A: Wesley
            DiaOption optionA = new DiaOption("Take the boy (Accept Wesley)");
            optionA.action = delegate
            {
                StartBabysitQuest("ST_Quest_BabysitWesley", map);
            };
            optionA.resolveTree = true;
            node.options.Add(optionA);

            // Option B: Tribbles
            DiaOption optionB = new DiaOption("Take the furballs (Accept Tribbles)");
            optionB.action = delegate
            {
                StartBabysitQuest("ST_Quest_BabysitTribbles", map);
            };
            optionB.resolveTree = true;
            node.options.Add(optionB);

            // Option C: Reject
            DiaOption optionC = new DiaOption("Close the channel (Reject)");
            optionC.action = delegate { };
            optionC.resolveTree = true;
            node.options.Add(optionC);

            Find.WindowStack.Add(new Dialog_NodeTree(node, true, false, title));

            return true;
        }

        private void StartBabysitQuest(string questDefName, Map map)
        {
            // Generates and auto-accepts the quest
            Slate slate = new Slate();
            slate.Set<Map>("map", map);
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed(questDefName), slate);
            
            quest.Accept(null); 
            
            Find.LetterStack.ReceiveLetter(
                "Guest Arrived", 
                "The transport has been completed. Take good care of the 'package' for the next 10 days.", 
                LetterDefOf.PositiveEvent
            );
        }
    }
}