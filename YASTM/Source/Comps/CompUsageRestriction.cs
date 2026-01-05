using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace YASTM
{
    public class CompUsageRestriction : ThingComp
    {
        // Wir nutzen FloatMenuOptions, um den Zugriff direkt beim Rechtsklick zu sperren
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            // Hole die Regeln aus dem XML des Gebäudes
            var rules = parent.def.GetModExtension<UsageRestrictionExtension>();
            if (rules == null) yield break; // Keine Regeln = Zugriff erlaubt

            if (!HasAccess(selPawn, rules))
            {
                yield return new FloatMenuOption(rules.failMessage + $" ({GetReason(selPawn, rules)})", null);
            }
        }

        private bool HasAccess(Pawn p, UsageRestrictionExtension rules)
        {
            // 1. Check Training (Holodisc)
            if (rules.requiredTrainingTrait != null)
            {
                if (p.story?.traits?.HasTrait(rules.requiredTrainingTrait) == false) return false;
            }

            // 2. Check Rank
            if (rules.allowedRanks != null && rules.allowedRanks.Count > 0)
            {
                bool rankMet = false;
                foreach (var req in rules.allowedRanks)
                {
                    Trait t = p.story?.traits?.GetTrait(req.rankDef);
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
                return "Training missing";
            return "Rank insufficient";
        }
    }
}