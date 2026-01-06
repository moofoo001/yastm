using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace YASTM
{
    public class CompUsageRestriction : ThingComp
    {
        // Klinkt sich in das Rechtsklick-Menü ein
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            // Regeln aus dem XML holen
            var rules = parent.def.GetModExtension<UsageRestrictionExtension>();
            if (rules == null) yield break; // Keine Regeln -> Zugriff erlaubt

            if (!HasAccess(selPawn, rules))
            {
                // Zeige ausgegraute Option mit Grund
                string reason = GetReason(selPawn, rules);
                yield return new FloatMenuOption($"{rules.failMessage} ({reason})", null);
            }
        }

        private bool HasAccess(Pawn p, UsageRestrictionExtension rules)
        {
            if (p.story == null || p.story.traits == null) return false;

            // 1. Check Training (Holodisc)
            if (rules.requiredTrainingTrait != null)
            {
                if (!p.story.traits.HasTrait(rules.requiredTrainingTrait)) return false;
            }

            // 2. Check Rank (Liste durchgehen)
            if (rules.allowedRanks != null && rules.allowedRanks.Count > 0)
            {
                bool rankMet = false;
                foreach (var req in rules.allowedRanks)
                {
                    Trait t = p.story.traits.GetTrait(req.rankDef);
                    // Hat den Rang-Trait UND der Degree ist hoch genug?
                    if (t != null && t.Degree >= req.minDegree)
                    {
                        rankMet = true;
                        break;
                    }
                }
                if (!rankMet) return false;
            }

            return true;
        }

        private string GetReason(Pawn p, UsageRestrictionExtension rules)
        {
            if (rules.requiredTrainingTrait != null && !p.story.traits.HasTrait(rules.requiredTrainingTrait))
                return "Training Missing";
            
            if (rules.allowedRanks != null && rules.allowedRanks.Count > 0)
                return "Rank Insufficient";

            return "Restricted";
        }
    }
}