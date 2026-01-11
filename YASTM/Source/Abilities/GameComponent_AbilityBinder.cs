using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
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

        public void CheckAndBindAbilities()
        {

            foreach (Pawn pawn in ST_CrewUtility.GetAllActiveCrewMembers())
            {
                TryUpdatePawn(pawn);
            }


            if (PawnsFinder.AllMaps_PrisonersOfColony != null)
            {
                foreach (Pawn pawn in PawnsFinder.AllMaps_PrisonersOfColony)
                {
                    TryUpdatePawn(pawn);
                }
            }
        }

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