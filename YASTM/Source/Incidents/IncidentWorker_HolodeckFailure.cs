using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace YASTM
{
    public class IncidentWorker_HolodeckFailure : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return GetHolodeck(map) != null && base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            // Check if the mod is enabled
            if (!YASTM_Mod.Settings.enableHolodeckFailures) return false;
            
            Map map = (Map)parms.target;
            Building holodeck = GetHolodeck(map);

            if (holodeck == null) return false;

            IntVec3 spawnSpot = holodeck.InteractionCell;
            if (!spawnSpot.Walkable(map)) spawnSpot = holodeck.Position;

            // Badgey spawnt 2-5 Hologramme
            int count = Mathf.Clamp((int)(parms.points / 100), 2, 5); 
            
            PawnKindDef enemyKind = PawnKindDefOf.AncientSoldier; 
            Faction enemyFaction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.AncientsHostile) 
                                   ?? Find.FactionManager.FirstFactionOfDef(FactionDefOf.Mechanoid);

            List<Pawn> spawnedHolograms = new List<Pawn>();

            for (int i = 0; i < count; i++)
            {
                if (!CellFinder.TryFindRandomCellNear(spawnSpot, map, 3, (c) => c.Standable(map) && c.GetRoom(map) == holodeck.GetRoom(), out IntVec3 cell))
                {
                    cell = spawnSpot;
                }

                // --- DER FIX: DIE SICHERE METHODE ---
                // Wir nutzen den simplen Konstruktor und setzen wichtige Werte manuell, falls nötig.
                // Das verhindert Abstürze durch Versions-Unterschiede bei den Parametern.
                
                PawnGenerationRequest request = new PawnGenerationRequest(
                    enemyKind,
                    enemyFaction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    true // forceGenerateNewPawn
                );
                
                // Weitere Einstellungen (optional, aber sicherer so):
                request.AllowDead = false;
                request.AllowDowned = false;
                request.MustBeCapableOfViolence = true;
                
                // ------------------------------------

                Pawn hologram = PawnGenerator.GeneratePawn(request);
                
                // Hediff geben (Macht sie schmerzfrei etc.)
                hologram.health.AddHediff(HediffDef.Named("ST_Hediff_HolographicProjection"));
                
                // Name ändern
                hologram.Name = new NameTriple("Hologram", "Villain", "Simulation");

                GenSpawn.Spawn(hologram, cell, map, WipeMode.Vanish);
                spawnedHolograms.Add(hologram);
                
                // Effekt: Blauer Blitz
                FleckMaker.ThrowLightningGlow(cell.ToVector3(), map, 1.5f);
            }

            if (spawnedHolograms.Count > 0)
            {
                // LordJob: Angriff auf die Kolonie!
                LordMaker.MakeNewLord(enemyFaction, new LordJob_AssaultColony(enemyFaction, true, true, false, false, true), map, spawnedHolograms);
            }

            SendStandardLetter(parms, new TargetInfo(spawnSpot, map));
            
            return true;
        }

        private Building GetHolodeck(Map map)
        {
            // Suche nach dem Gebäude.
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("ST_HoloEmitter");
            
            if (def == null) return null;

            return map.listerBuildings.AllBuildingsColonistOfDef(def).FirstOrDefault();
        }
    }
}