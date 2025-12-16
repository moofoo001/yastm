using System.Collections.Generic;
using RimWorld;            
using RimWorld.QuestGen;
using Verse;
using YASTM.Source.Systems;

namespace StarTrekFactions.QuestNodes
{

    public class QuestNode_ChoiceCompat : QuestNode
    {
        public List<Option> choices = new List<Option>();

        public class Option
        {
            public SlateRef<string> label; 
            public QuestNode node;
        }

        protected override void RunInt()
        {
            var slate = QuestGen.slate;

            if (choices == null || choices.Count == 0)
            {
                Log.Warning("[YASTM][CHOICE] No choices defined on ChoiceCompat node.");
                return;
            }


            string gate = slate.Get<string>("inSignal");
            if (gate.NullOrEmpty())
                gate = QuestGen.GenerateNewSignal("ChoiceCompat");

            var part = new QuestPart_Choice
            {
                inSignalChoiceUsed = gate,
                choices = new List<QuestPart_Choice.Choice>()
            };

            foreach (var opt in choices)
            {
                var choice = new QuestPart_Choice.Choice();

                int before = QuestGen.quest.PartsListForReading.Count;

                if (opt?.node != null)
                {

                    string innerDone = QuestGen.GenerateNewSignal("ChoiceCompatInner");
                    QuestGenUtility.RunInnerNode(opt.node, innerDone);
                }


                var parts = QuestGen.quest.PartsListForReading;
                if (parts.Count > before)
                {
                    if (choice.questParts == null)
                        choice.questParts = new List<QuestPart>();
                    for (int i = before; i < parts.Count; i++)
                        choice.questParts.Add(parts[i]);
                }

                part.choices.Add(choice);
            }

            QuestGen.quest.AddPart(part);
            Log.Message($"[YASTM][CHOICE] armed on inSignalChoiceUsed='{gate}' with {part.choices.Count} options (quest {QuestGen.quest.id}).");
        }

        protected override bool TestRunInt(Slate s) => true;
    }
}

