using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class CompCareer : ThingComp
    {
        // Speichert Punkte: "Combat" -> 15.5, "Trade" -> 5000
        private Dictionary<string, float> pointTracker = new Dictionary<string, float>();
        
        // Cache für die aktive Karriere (damit wir nicht jeden Tick suchen)
        private CareerDef activeCareer;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref pointTracker, "pointTracker", LookMode.Value, LookMode.Value);
            // Wir speichern activeCareer nicht direkt, sondern suchen es beim Laden neu (sicherer bei Updates)
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (activeCareer == null) AssignCareer();
        }

        public void AssignCareer()
        {
            Pawn p = parent as Pawn;
            if (p == null) return;

            // Finde die erste Karriere, die auf diesen Pawn passt
            activeCareer = DefDatabase<CareerDef>.AllDefs.FirstOrDefault(def => IsApplicable(def, p));
        }

        private bool IsApplicable(CareerDef def, Pawn p)
        {
            // 1. Xenotype Check (Klingonen/Romulaner)
            if (def.requiredXenotypes != null && !def.requiredXenotypes.NullOrEmpty())
            {
                if (p.genes?.Xenotype == null || !def.requiredXenotypes.Contains(p.genes.Xenotype.defName))
                    return false;
            }

            // 2. Faction Check (Föderation)
            if (def.requiredFactions != null && !def.requiredFactions.NullOrEmpty())
            {
                if (p.Faction == null || !def.requiredFactions.Contains(p.Faction.def.defName))
                    return false;
            }

            return true;
        }

        // --- PUBLIC API: PUNKTE HINZUFÜGEN ---
        public void AddPoints(string category, float amount)
        {
            if (activeCareer == null) return; // Wer keine Karriere hat, sammelt keine Punkte
            if (activeCareer.pointCategory != category) return; // Falsche Kategorie (z.B. Klingone handelt)

            if (!pointTracker.ContainsKey(category)) pointTracker[category] = 0;
            pointTracker[category] += amount;

            // Kleines visuelles Feedback
            if (amount > 0 && parent is Pawn p && p.Map != null && !p.Drafted)
            {
                // Zeigt "+1 Combat" über dem Kopf
                MoteMaker.ThrowText(p.DrawPos, p.Map, $"+{amount:F0} {category}", Color.cyan);
            }

            CheckPromotion();
        }

private void CheckPromotion()
        {
            if (activeCareer == null) return;
            Pawn p = parent as Pawn;
            if (p.story == null) return;

            float currentPoints = pointTracker.ContainsKey(activeCareer.pointCategory) ? pointTracker[activeCareer.pointCategory] : 0;

            CareerRank bestRank = activeCareer.ranks
                                    .OrderByDescending(r => r.threshold)
                                    .FirstOrDefault(r => currentPoints >= r.threshold);

            if (bestRank == null) return;

            // Haben wir diesen Rang schon?
            Trait currentTrait = p.story.traits.GetTrait(bestRank.rewardTrait);
            if (currentTrait != null && currentTrait.Degree == bestRank.rewardDegree)
            {
                return; 
            }

            // --- BEFÖRDERUNG ---
            
            // 1. Alte Ränge entfernen
            foreach (var rank in activeCareer.ranks)
            {
                if (p.story.traits.HasTrait(rank.rewardTrait))
                {
                    Trait t = p.story.traits.GetTrait(rank.rewardTrait);
                    p.story.traits.RemoveTrait(t);
                }
            }

            // 2. Neuen Rang vergeben
            Trait newTrait = new Trait(bestRank.rewardTrait, bestRank.rewardDegree);
            p.story.traits.GainTrait(newTrait);

            // 3. HIER WAR DER FEHLERHAFTE "APPAREL SWAP" BLOCK -> GELÖSCHT!
            // Da wir nur noch Visuals nutzen, müssen wir keine Items mehr spawnen.

            // 4. Feier!
            Find.LetterStack.ReceiveLetter($"Promotion: {bestRank.label}", 
                $"{p.LabelShort} has reached {currentPoints} {activeCareer.pointCategory} points and has been promoted to {bestRank.label}.", 
                LetterDefOf.PositiveEvent, p);

            // Sound (Optional)
            // SoundDefOf.Quest_Succeeded.PlayOneShotOnCamera();
        }


        public override string CompInspectStringExtra()
        {
            if (activeCareer == null) return null;
            float pts = pointTracker.ContainsKey(activeCareer.pointCategory) ? pointTracker[activeCareer.pointCategory] : 0;
            
            // Zeigt: "Career: Klingon Warrior (Combat: 5)"
            return $"Career Progress ({activeCareer.pointCategory}): {pts:F0}";
        }
    }
}