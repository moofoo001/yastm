using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound; 
using UnityEngine;

namespace YASTM
{
    public class IncidentWorker_QVisit : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return map.mapPawns.FreeColonists.Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            // Check if the mod is enabled
            if (!YASTM_Mod.Settings.enableQVisits) return false;
            Map map = (Map)parms.target;
            List<Pawn> colonists = map.mapPawns.FreeColonists.ToList();
            if (colonists.Count == 0) return false;

            // The Q-Effect
            Pawn witness = colonists.RandomElement();
            FleckMaker.ThrowLightningGlow(witness.TrueCenter(), map, 3.0f);
            
            // sound
            if (ST_SoundDefOf.ST_Transporter_Beam != null) 
            {
                ST_SoundDefOf.ST_Transporter_Beam.PlayOneShotOnCamera(map);
            }

            //  Random selection
            int roll = Rand.RangeInclusive(1, 5);
            string outcomeText = "";

            switch (roll)
            {
                case 1: // HEALING
                    outcomeText = "Q decided your frailty was annoying. He healed everyone.";
                    foreach (Pawn p in colonists)
                    {
                        List<Hediff> hediffs = p.health.hediffSet.hediffs;
                        for (int i = hediffs.Count - 1; i >= 0; i--)
                        {
                            if (hediffs[i] is Hediff_Injury || hediffs[i] is Hediff_MissingPart)
                            {
                                p.health.RemoveHediff(hediffs[i]);
                            }
                        }
                        FleckMaker.ThrowMetaIcon(p.Position, p.Map, FleckDefOf.HealingCross);
                    }
                    break;

                case 2: // TEST (RAID)  
                    outcomeText = "Q summons a threat to test your tactical abilities!";
                    IncidentDef raid = IncidentDefOf.RaidEnemy;
                    raid.Worker.TryExecute(parms); 
                    break;

                case 3: // TELEPORT
                    outcomeText = "Q snapped his fingers and rearranged your crew just for fun.";
                    foreach (Pawn p in colonists)
                    {
                        IntVec3 newPos = CellFinder.RandomCell(map);
                        if (newPos.InBounds(map) && newPos.Walkable(map) && !newPos.Fogged(map))
                        {
                            p.Position = newPos;
                            p.Notify_Teleported();
                            FleckMaker.ThrowLightningGlow(p.TrueCenter(), map, 1.0f);
                        }
                    }
                    break;

                case 4: // GIFT
                    outcomeText = "Q feels generous (or pity). Supplies have materialized.";
                    Thing gold = ThingMaker.MakeThing(ThingDefOf.Gold);
                    gold.stackCount = 75;
                    GenPlace.TryPlaceThing(gold, witness.Position, map, ThingPlaceMode.Near);
                    break;

                case 5: // MOOD (Inspiration oder Break)
                    Pawn victim = colonists.RandomElement();
                    if (Rand.Chance(0.5f))
                    {
                        outcomeText = $"Q inspired {victim.LabelShort} with a vision of greatness.";
                        
                        //  search inspiration 
                        InspirationDef frenzy = DefDatabase<InspirationDef>.GetNamed("Frenzy_Work", false);
                        // Fallback if Frenzy_Work is missing, take Inspired_Creativity
                        if (frenzy == null) frenzy = InspirationDefOf.Inspired_Creativity;
                        
                        if (frenzy != null) victim.mindState.inspirationHandler.TryStartInspiration(frenzy);
                    }
                    else
                    {
                        outcomeText = $"Q whispered terrifying truths to {victim.LabelShort}.";
                        victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Psychotic, "Q's Prank");
                    }
                    break;
            }

            // Letter to the player
            SendStandardLetter(parms, new LookTargets(witness), "The Q", 
                $"Mon Capitaine!\n\n{outcomeText}\n\nDon't provoke the continuum.");

            return true;
        }
    }
}