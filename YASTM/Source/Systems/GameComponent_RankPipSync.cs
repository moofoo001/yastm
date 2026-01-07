using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    public class GameComponent_RankPipSync : GameComponent
    {
        private int tickCounter = 0;
        private const int CheckInterval = 2500; // Alle ~40 Sekunden (Performance sparen)

        public GameComponent_RankPipSync(Game game) { }

        public override void GameComponentTick()
        {
            tickCounter++;
            if (tickCounter >= CheckInterval)
            {
                SyncAllRankPips();
                tickCounter = 0;
            }
        }

        public void SyncAllRankPips()
        {
            // Wir iterieren über die gesamte Crew (globale Liste aus ST_CrewUtility)
            foreach (Pawn pawn in ST_CrewUtility.GetAllActiveCrewMembers())
            {
                if (pawn.Destroyed || pawn.Dead || pawn.apparel == null) continue;

                SyncPawnPip(pawn);
            }
        }

        private void SyncPawnPip(Pawn pawn)
        {
            // 1. Suche nach Rang-Traits mit unserer VisualExtension
            Trait rankTrait = null;
            RankVisualExtension extension = null;
            RankData currentRankData = null;

            if (pawn.story?.traits?.allTraits == null) return;

            foreach (var trait in pawn.story.traits.allTraits)
            {
                var ext = trait.def.GetModExtension<RankVisualExtension>();
                if (ext != null)
                {
                    rankTrait = trait;
                    extension = ext;
                    // Finde die Daten für den aktuellen Degree (Stufe) des Traits
                    currentRankData = ext.ranks?.FirstOrDefault(r => r.degree == trait.Degree);
                    break; // Ein Pawn hat normalerweise nur einen Rang-Trait
                }
            }

            // Wenn kein Rang da ist, aber Pips getragen werden -> Ausziehen!
            if (currentRankData == null)
            {
                RemoveAllPips(pawn);
                return;
            }

            // 2. Bestimme, welches Item getragen werden soll
            // Wir bauen den DefName: "ST_Apparel_Pip_" + "Ensign"
            string targetDefName = !string.IsNullOrEmpty(currentRankData.specificDefName) 
                ? currentRankData.specificDefName 
                : "ST_Apparel_Pip_" + currentRankData.texName;

            ThingDef targetPipDef = DefDatabase<ThingDef>.GetNamedSilentFail(targetDefName);

            if (targetPipDef == null)
            {
                // Fallback: Logge Fehler nur einmalig, um Spam zu vermeiden (hier vereinfacht)
                // Log.Warning($"[YASTM] Could not find Pip ThingDef named: {targetDefName}");
                return;
            }

            // 3. Prüfen: Trägt er es schon?
            bool correctPipWorn = false;
            List<Apparel> pipsToRemove = new List<Apparel>();

            foreach (var worn in pawn.apparel.WornApparel)
            {
                // Prüfen ob es ein Pip ist (via Tag oder Naming)
                // Am besten haben alle Pips in XML den Tag <li>ST_RankPip</li>
                if (worn.def.apparel?.tags != null && worn.def.apparel.tags.Contains("ST_RankPip"))
                {
                    if (worn.def == targetPipDef)
                    {
                        correctPipWorn = true;
                    }
                    else
                    {
                        // Falscher Pip (z.B. noch Ensign Pip obwohl jetzt Lieutenant)
                        pipsToRemove.Add(worn);
                    }
                }
            }

            // 4. Aufräumen (Falsche Pips weg)
            foreach (var oldPip in pipsToRemove)
            {
                pawn.apparel.Remove(oldPip);
                oldPip.Destroy(); // Wir zerstören sie, damit das Lager nicht mit alten Pips vollmüllt
            }

            // 5. Anziehen (Wenn der richtige fehlt)
            if (!correctPipWorn)
            {
                Apparel newPip = (Apparel)ThingMaker.MakeThing(targetPipDef);
                if (newPip != null)
                {
                    pawn.apparel.Wear(newPip, true, true); // forceWear = true
                }
            }
        }

        private void RemoveAllPips(Pawn pawn)
        {
            var pips = pawn.apparel.WornApparel
                .Where(a => a.def.apparel?.tags != null && a.def.apparel.tags.Contains("ST_RankPip"))
                .ToList();

            foreach (var pip in pips)
            {
                pawn.apparel.Remove(pip);
                pip.Destroy();
            }
        }
    }
}