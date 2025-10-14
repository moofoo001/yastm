using System.Linq;
using RimWorld;
using Verse;

namespace ST.Abilities
{
    // Vergibt/entfernt Abilities automatisch, wenn Traits vorhanden/fehlen.
    public class GameComponent_AbilityBinder : GameComponent
    {
        private AbilityDef nervePinch;
        private AbilityDef fieldTriage;
        private TraitDef traitIDIC;
        private TraitDef traitStarfleet;
        private AbilityDef commandPresence;
        private AbilityDef tacticalOverwatch;
        private TraitDef traitCommand;
        private TraitDef traitSecurity;

        public GameComponent_AbilityBinder(Game game) { }

        public override void FinalizeInit()
        {
            nervePinch   = DefDatabase<AbilityDef>.GetNamedSilentFail("ST_Ability_VulcanNervePinch");
            fieldTriage  = DefDatabase<AbilityDef>.GetNamedSilentFail("ST_Ability_FieldTriage");
            traitIDIC    = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Trait_IDICMindset");
            traitStarfleet = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Trait_StarfleetTraining");
            commandPresence  = DefDatabase<AbilityDef>.GetNamedSilentFail("ST_Ability_CommandPresence");
            tacticalOverwatch = DefDatabase<AbilityDef>.GetNamedSilentFail("ST_Ability_TacticalOverwatch");
            traitCommand     = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Trait_CommandTraining");
            traitSecurity    = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Trait_SecurityOfficer");

            LongEventHandler.ExecuteWhenFinished(BindAll);
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 1200 == 0) BindAll(); // alle 20s
        }

        private void BindAll()
        {
            foreach (var map in Find.Maps)
            {
                var pawns = map.mapPawns?.FreeColonistsSpawned;
                if (pawns == null) continue;
                foreach (var p in pawns) BindForPawn(p);
            }
        }

        private void BindForPawn(Pawn p)
        {
            if (p?.abilities == null || p.Dead || !(p.RaceProps?.Humanlike ?? false)) return;

            // Nerve Pinch an IDIC
            if (traitIDIC != null && nervePinch != null)
            {
                bool hasTrait = p.story?.traits?.HasTrait(traitIDIC) == true;
                bool hasAbility = p.abilities.GetAbility(nervePinch) != null;
                if (hasTrait && !hasAbility) p.abilities.GainAbility(nervePinch);
                // if (!hasTrait && hasAbility) p.abilities.RemoveAbility(nervePinch); // <-- AUS
            }

            // Field Triage an Starfleet Training
            if (traitStarfleet != null && fieldTriage != null)
            {
                bool hasTrait = p.story?.traits?.HasTrait(traitStarfleet) == true;
                bool hasAbility = p.abilities.GetAbility(fieldTriage) != null;
                if (hasTrait && !hasAbility) p.abilities.GainAbility(fieldTriage);
                // if (!hasTrait && hasAbility) p.abilities.RemoveAbility(fieldTriage); // <-- AUS
            }

            // Command Presence an CommandTraining
            if (traitCommand != null && commandPresence != null)
            {
                bool hasTrait = p.story?.traits?.HasTrait(traitCommand) == true;
                bool hasAbility = p.abilities.GetAbility(commandPresence) != null;
                if (hasTrait && !hasAbility) p.abilities.GainAbility(commandPresence);
                // if (!hasTrait && hasAbility) p.abilities.RemoveAbility(commandPresence); // <-- AUS
            }

            // Tactical Overwatch an SecurityOfficer
            if (traitSecurity != null && tacticalOverwatch != null)
            {
                bool hasTrait = p.story?.traits?.HasTrait(traitSecurity) == true;
                bool hasAbility = p.abilities.GetAbility(tacticalOverwatch) != null;
                if (hasTrait && !hasAbility) p.abilities.GainAbility(tacticalOverwatch);
                // if (!hasTrait && hasAbility) p.abilities.RemoveAbility(tacticalOverwatch); // <-- AUS
            }
        }
    }
}
