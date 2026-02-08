using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class CompStarfleetCareer : ThingComp
    {
        public ST_CareerDef careerDef;
        public ST_RankDef currentRank;

        // FIX: Property für externe Zugriffe (z.B. StatParts)
        public ST_RankDef CurrentRank => currentRank;

        public bool IsStarfleetMember => careerDef != null;
        public string CurrentRankLabel => currentRank?.label ?? "Civilian";

        public Texture2D CurrentRankIcon
        {
            get
            {
                // FIX: Greift jetzt sicher auf iconPath zu (da wir es in ST_RankDef ergänzt haben)
                if (currentRank != null && !currentRank.iconPath.NullOrEmpty())
                {
                    return ContentFinder<Texture2D>.Get(currentRank.iconPath, true);
                }
                return ContentFinder<Texture2D>.Get("UI/Icons/Starfleet/Combadge", true); 
            }
        }

public override void PostExposeData()
        {
            base.PostExposeData(); // Korrekt: Ruft PostExposeData der Basisklasse auf
            Scribe_Defs.Look(ref careerDef, "ST_careerDef");
            Scribe_Defs.Look(ref currentRank, "ST_currentRank");
        }

        public void AssignCareer(ST_CareerDef newCareer)
        {
            this.careerDef = newCareer;
            // Wir nehmen den Rang der ERSTEN Stufe
            if (newCareer.stages != null && newCareer.stages.Count > 0)
            {
                AssignRank(newCareer.stages[0].rank);
            }
        }

        public void AssignRank(ST_RankDef rank)
        {
            this.currentRank = rank;
        }

        // --- FIX: Beförderung mit komplexer "stages"-Liste ---
        public void Promote()
        {
            if (careerDef == null || careerDef.stages.NullOrEmpty()) return;

            // Finde heraus, auf welcher Stufe wir gerade sind
            int currentIdx = careerDef.stages.FindIndex(s => s.rank == currentRank);

            // Gibt es eine nächste Stufe?
            if (currentIdx >= 0 && currentIdx < careerDef.stages.Count - 1)
            {
                var nextStage = careerDef.stages[currentIdx + 1];
                if (nextStage.rank != null)
                {
                    AssignRank(nextStage.rank);
                    Messages.Message($"{parent.LabelShort} promoted to {currentRank.label}.", parent, MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                Messages.Message("Maximum rank achieved.", MessageTypeDefOf.RejectInput, false);
            }
        }

        public void Demote()
        {
            if (careerDef == null || careerDef.stages.NullOrEmpty()) return;

            int currentIdx = careerDef.stages.FindIndex(s => s.rank == currentRank);

            if (currentIdx > 0)
            {
                var prevStage = careerDef.stages[currentIdx - 1];
                if (prevStage.rank != null)
                {
                    AssignRank(prevStage.rank);
                    Messages.Message($"{parent.LabelShort} demoted to {currentRank.label}.", parent, MessageTypeDefOf.NegativeEvent);
                }
            }
        }
    }
}