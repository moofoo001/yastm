using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace YASTM
{
    public class CompUsageRestriction : ThingComp
    {
        // --- FloatMenu Options ---
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {

            var rules = parent.def.GetModExtension<UsageRestrictionExtension>();
            if (rules == null) yield break;

            if (!HasAccess(selPawn, rules))
            {
                // no access
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

            // 2. Check Rank (Traits)
            if (rules.allowedRanks != null && rules.allowedRanks.Count > 0)
            {
                bool rankMet = false;
                foreach (var req in rules.allowedRanks)
                {
                    Trait t = p.story.traits.GetTrait(req.rankDef);

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