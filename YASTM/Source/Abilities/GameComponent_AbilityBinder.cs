using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using RimWorld.Planet; // Wichtig für Caravans
using YASTM.Abilities; 

namespace YASTM
{
    public class GameComponent_AbilityBinder : GameComponent
    {
        private int tickCounter = 0;
        private const int CheckInterval = 2000; 

        public GameComponent_AbilityBinder(Game game)
        {
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            
            tickCounter++;
            if (tickCounter >= CheckInterval)
            {
                CheckAndBindAbilities();
                tickCounter = 0;
            }
        }

        private void CheckAndBindAbilities()
        {
            // STRATEGIE FÜR RIMWORLD 1.6:
            // Die monolithischen Listen wurden entfernt. Wir iterieren über die spezifischen Kategorien.
            
            // 1. Kolonisten auf Karten (Das ist die wichtigste Gruppe)
            // "AllMaps_FreeColonists" existiert stabil in 1.4, 1.5 und 1.6
            if (PawnsFinder.AllMaps_FreeColonists != null)
            {
                foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
                {
                    TryUpdatePawn(pawn);
                }
            }

            // 2. Gefangene der Kolonie (Auf Karten)
            if (PawnsFinder.AllMaps_PrisonersOfColony != null)
            {
                foreach (Pawn pawn in PawnsFinder.AllMaps_PrisonersOfColony)
                {
                    TryUpdatePawn(pawn);
                }
            }

            // 3. Kolonisten in Karawanen und Kapseln (Weltkarte)
            // Wir nutzen hier die spezifische Liste für mobile Einheiten, statt der globalen Map-Liste.
            // Falls diese in 1.6 auch umbenannt wurde, nutzen wir sicherheitshalber eine direkte Filterung der Liste "AllCaravans...Alive".
            if (PawnsFinder.AllCaravansAndTravelingTransportPods_Alive != null)
            {
                foreach (Pawn pawn in PawnsFinder.AllCaravansAndTravelingTransportPods_Alive)
                {
                    // Hier müssen wir manuell filtern, da diese Liste auch Tiere enthalten kann
                    if (pawn.RaceProps.Humanlike && pawn.IsColonist)
                    {
                        TryUpdatePawn(pawn);
                    }
                }
            }
        }

        // Hilfsmethode, um Code-Duplizierung in den Schleifen zu vermeiden
        private void TryUpdatePawn(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;

            if (pawn.story != null && pawn.story.traits != null)
            {
                UpdateAbilitiesForPawn(pawn);
            }
        }

        public void UpdateAbilitiesForPawn(Pawn pawn)
        {
            if (pawn.abilities == null) return;

            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                var extension = trait.def.GetModExtension<ST_GrantAbilitiesExtension>();
                
                if (extension != null && extension.abilities != null)
                {
                    foreach (AbilityDef abilityDef in extension.abilities)
                    {
                        if (pawn.abilities.GetAbility(abilityDef) == null)
                        {
                            pawn.abilities.GainAbility(abilityDef);
                        }
                    }
                }
            }
        }
    }
}