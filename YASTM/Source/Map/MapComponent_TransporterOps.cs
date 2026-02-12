using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace YASTM
{
    public class MapComponent_TransporterOps : MapComponent
    {
        // 5% chance of transporter accident
        private const float ACCIDENT_CHANCE = 0.05f;

        public MapComponent_TransporterOps(Map map) : base(map) { }

        // The main function, which should be called by your buildings
        public void TeleportThing(Thing thing, IntVec3 targetCell, Map targetMap)
        {
            if (thing == null || !targetCell.IsValid || targetMap == null) return;

            // 1. Effect at the start location (before he's gone)
            FleckMaker.ThrowLightningGlow(thing.TrueCenter(), thing.Map, 1.0f);

            // 2. Perform teleportation
            if (thing.Map == targetMap)
            {
                // Same map: Just move
                thing.Position = targetCell;
                if (thing is Pawn p) p.Notify_Teleported();
            }
            else
            {
                // Different map: De-Spawn and Re-Spawn
                thing.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(thing, targetCell, targetMap);
            }

            // 3. Effect at the destination
            FleckMaker.ThrowLightningGlow(targetCell.ToVector3Shifted(), targetMap, 1.0f);
            
            // Sound (if available)
            if (ST_SoundDefOf.ST_Transporter_Beam != null)
                ST_SoundDefOf.ST_Transporter_Beam.PlayOneShot(new TargetInfo(targetCell, targetMap));

            // 4. Accident check (only for pawns and alive)
            if (thing is Pawn victim && !victim.Dead)
            {
                CheckForPatternDecay(victim);
            }
        }

        private void CheckForPatternDecay(Pawn p)
        {
            // 95% chance of no accident
            if (!Rand.Chance(ACCIDENT_CHANCE)) return;

            // Oh no, an accident!
            int roll = Rand.RangeInclusive(1, 3);
            string accidentDesc = "";

            switch (roll)
            {
                case 1: // Nausea
                    accidentDesc = $"{p.LabelShort} is suffering from severe pattern nausea.";
                    Hediff sickness = HediffMaker.MakeHediff(HediffDefOf.CryptosleepSickness, p);
                    p.health.AddHediff(sickness);
                    break;

                case 2: // Hot Pattern
                    accidentDesc = $"{p.LabelShort}'s pattern buffer ran too hot! Mild burns detected.";
                    // Random body part burn (damage 10)
                    p.TakeDamage(new DamageInfo(DamageDefOf.Burn, 10, 0, -1, null, null, null));
                    break;

                case 3: // Wardrobe Malfunction
                    accidentDesc = $"{p.LabelShort}'s clothing failed to rematerialize correctly!";
                    // Check if clothing is available
                    if (p.apparel != null && p.apparel.WornApparel.Any())
                    {
                        // Drop all clothing!
                        p.apparel.DropAll(p.Position, false, true);
                        Messages.Message("Clothing pattern lost in buffer!", p, MessageTypeDefOf.NegativeEvent);
                    }
                    else
                    {
                        // Fallback falls schon nackt -> Betäubung
                        accidentDesc = $"{p.LabelShort} was stunned by a rematerialization spike.";
                        p.health.AddHediff(HediffDefOf.Anesthetic);
                    }
                    break;
            }

            // Visual feedback for the fail
            FleckMaker.ThrowSmoke(p.Position.ToVector3Shifted(), p.Map, 2f);
            FleckMaker.ThrowText(p.DrawPos, p.Map, "ERROR!", Color.red);
            
            // Letter to the player
            Find.LetterStack.ReceiveLetter("Transporter Malfunction", 
                $"Transporter logs indicate a pattern buffer error during transport of {p.LabelShort}.\n\nOutcome: {accidentDesc}\n\nMaintain your equipment, Captain!", 
                LetterDefOf.NegativeEvent, p);
        }
    }
}