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
        // chance of accident (5%)
        private const float ACCIDENT_CHANCE = 0.05f;

        public MapComponent_TransporterOps(Map map) : base(map) { }

        public void TeleportThing(Thing thing, IntVec3 targetCell, Map targetMap)
        {
            if (thing == null || !targetCell.IsValid || targetMap == null) return;

            // 1. effect at the start location
            FleckMaker.ThrowLightningGlow(thing.TrueCenter(), thing.Map, 1.0f);

            // 2. teleportation
            if (thing.Map == targetMap)
            {
                thing.Position = targetCell;
                if (thing is Pawn p) p.Notify_Teleported();
            }
            else
            {
                thing.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(thing, targetCell, targetMap);
            }

            // 3. effect at the destination
            FleckMaker.ThrowLightningGlow(targetCell.ToVector3Shifted(), targetMap, 1.0f);
            
            if (ST_SoundDefOf.ST_Transporter_Beam != null)
                ST_SoundDefOf.ST_Transporter_Beam.PlayOneShot(new TargetInfo(targetCell, targetMap));

            // 4. accident check
            if (thing is Pawn victim && !victim.Dead)
            {
                CheckForPatternDecay(victim);
            }
        }

        private void CheckForPatternDecay(Pawn p)
        {
            if (!Rand.Chance(ACCIDENT_CHANCE)) return;

            int roll = Rand.RangeInclusive(1, 3);
            string accidentDesc = "";

            switch (roll)
            {
                case 1: // nausea
                    accidentDesc = $"{p.LabelShort} is suffering from severe pattern nausea.";
                    Hediff sickness = HediffMaker.MakeHediff(HediffDefOf.CryptosleepSickness, p);
                    p.health.AddHediff(sickness);
                    break;

                case 2: // burn
                    accidentDesc = $"{p.LabelShort}'s pattern buffer ran too hot! Mild burns detected.";
                    p.TakeDamage(new DamageInfo(DamageDefOf.Burn, 10, 0, -1, null, null, null));
                    break;

                case 3: // naked
                    accidentDesc = $"{p.LabelShort}'s clothing failed to rematerialize correctly!";
                    if (p.apparel != null && p.apparel.WornApparel.Any())
                    {
                        p.apparel.DropAll(p.Position, false, true);
                        Messages.Message("Clothing pattern lost in buffer!", p, MessageTypeDefOf.NegativeEvent);
                    }
                    else
                    {
                        accidentDesc = $"{p.LabelShort} was stunned by a rematerialization spike.";
                        p.health.AddHediff(HediffDefOf.Anesthetic);
                    }
                    break;
            }

            // visual feedback
            FleckMaker.ThrowSmoke(p.Position.ToVector3Shifted(), p.Map, 2f);
            
            // MoteMaker for text
            MoteMaker.ThrowText(p.DrawPos, p.Map, "ERROR!", Color.red);
            
            Find.LetterStack.ReceiveLetter("Transporter Malfunction", 
                $"Transporter logs indicate a pattern buffer error during transport of {p.LabelShort}.\n\nOutcome: {accidentDesc}\n\nMaintain your equipment, Captain!", 
                LetterDefOf.NegativeEvent, p);
        }
    }
}