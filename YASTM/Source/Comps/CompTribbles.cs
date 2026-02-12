using System.Collections.Generic;
using System.Linq; 
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace YASTM
{
    public class CompProperties_Tribbles : CompProperties
    {
        public float reproduceChance = 0.05f;
        public float reproduceThreshold = 0.9f;
        public int maxTribblesOnMap = 200;
        public int checkInterval = 250;
        public string hatedTrait = "ST_Klingon";
        
        public CompProperties_Tribbles()
        {
            this.compClass = typeof(CompTribbles);
        }
    }

    public class CompTribbles : ThingComp
    {
        public CompProperties_Tribbles Props => (CompProperties_Tribbles)props;
        private Pawn Pawn => (Pawn)parent;

        public override void CompTickRare()
        {
            base.CompTickRare();

            if (Pawn.Dead || Pawn.Downed || Pawn.Map == null) return;

            // 1. KLINGONEN DETEKTOR
            bool klingonNearby = false;
            foreach (Pawn p in Pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (p.RaceProps.Humanlike && p.Position.InHorDistOf(Pawn.Position, 8f))
                {
                    if (p.story?.traits?.allTraits.Any(t => t.def.defName == Props.hatedTrait) ?? false)
                    {
                        klingonNearby = true;
                        break;
                    }
                }
            }

            // Play sound
            if (klingonNearby)
            {
                ST_SoundDefOf.ST_Sound_Tribble_Angry?.PlayOneShot(Pawn);
            }
            else if (Rand.Chance(0.05f)) 
            {
                ST_SoundDefOf.ST_Sound_Tribble_Coo?.PlayOneShot(Pawn);
            }

            // 2. REPRODUCTION
            if (!klingonNearby && Pawn.needs.food != null && Pawn.needs.food.CurLevelPercentage > Props.reproduceThreshold)
            {
                TryReproduce();
            }
        }

        private void TryReproduce()
        {
            if (!Rand.Chance(Props.reproduceChance)) return;

            Map map = Pawn.Map;
            
            // Limit check
            if (map.mapPawns.AllPawnsSpawned.Count(p => p.def == Pawn.def) >= Props.maxTribblesOnMap)
            {
                return; 
            }

            PawnKindDef kind = Pawn.kindDef;
            if (kind == null) return;

            Pawn newTribble = PawnGenerator.GeneratePawn(kind, Faction.OfPlayer);
            PawnUtility.TrySpawnHatchedOrBornPawn(newTribble, Pawn);

            Pawn.needs.food.CurLevel -= 0.3f; 
            
            FleckMaker.ThrowDustPuff(Pawn.Position, map, 1.0f);
            
            if (Rand.Chance(0.1f)) 
                Messages.Message("Tribbles are multiplying...", newTribble, MessageTypeDefOf.NeutralEvent, true);
        }
    }
}