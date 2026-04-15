using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine; 

namespace YASTM
{
    public class Building_RessikanFlute : Building
    {
        private int lastSoundTick = -9999; 

        public override void TickRare()
        {
            base.TickRare();

            if (this.Spawned)
            {
                Pawn user = this.InteractionCell.GetFirstPawn(this.Map);

                if (user != null && user.CurJob != null && user.CurJob.targetA.Thing == this)
                {
                    // visual effect
                    ThrowMusicNotes(this.DrawPos, this.Map);

                    // sound
                    if (Find.TickManager.TicksGame > lastSoundTick + 2000)
                    {

                        SoundDef melody = DefDatabase<SoundDef>.GetNamedSilentFail("ST_Sound_RessikanFlute_Melody");
                        if (melody != null)
                        {
                            melody.PlayOneShot(new TargetInfo(this.Position, this.Map));
                            lastSoundTick = Find.TickManager.TicksGame;
                        }
                    }

                    // easter egg check
                    CheckEasterEgg(user);
                }
            }
        }

        private void ThrowMusicNotes(Vector3 loc, Map map)
        {
            int noteCount = Rand.RangeInclusive(2, 3);
            for (int i = 0; i < noteCount; i++)
            {
                Vector3 spawnLoc = loc + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(0f, 0.4f));
                
                // particle effect
                FleckDef noteFleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_Fleck_MusicNote");
                if (noteFleck != null)
                {
                    FleckCreationData data = FleckMaker.GetDataStatic(spawnLoc, map, noteFleck, Rand.Range(0.5f, 0.8f));
                    data.velocityAngle = Rand.Range(20f, 70f); 
                    data.velocitySpeed = Rand.Range(0.4f, 0.8f);
                    map.flecks.CreateFleck(data);
                }
            }
        }

        private void CheckEasterEgg(Pawn p)
        {
            string name = p.Name.ToStringShort;
            
            if (name.Contains("Jean-Luc") || name.Contains("Picard"))
            {

                ThoughtDef innerLightThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ST_InnerLightMemory");
                
                if (innerLightThought != null && p.needs.mood != null && p.needs.mood.thoughts.memories.GetFirstMemoryOfDef(innerLightThought) == null)
                {
                    TriggerInnerLight(p, innerLightThought);
                }
            }
        }

        private void TriggerInnerLight(Pawn p, ThoughtDef thoughtDef)
        {

            HediffDef comaDef = DefDatabase<HediffDef>.GetNamedSilentFail("ST_InnerLightComa");
            if (comaDef != null)
            {
                p.health.AddHediff(comaDef);
            }
            
            // Skill-Boosts
            p.skills.GetSkill(SkillDefOf.Artistic).Level += 10;
            p.skills.GetSkill(SkillDefOf.Intellectual).Level += 10;
            
            // Mood Buff 
            p.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);

            // notify
            Find.LetterStack.ReceiveLetter(
                "The Inner Light",
                $"{p.LabelShort} has collapsed into a deep trance while playing the flute. They are experiencing a lifetime of memories from a vanished world called Kataan.",
                LetterDefOf.PositiveEvent,
                p
            );
        }
    }
}