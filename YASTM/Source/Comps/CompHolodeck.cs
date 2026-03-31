using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_Holodeck : CompProperties
    {
        public float trainingXpPerTick = 0.15f;
        public float injuryChance = 0.001f;
        public float powerTrainingMode = 2000f;
        public float powerRelaxMode = 500f;
    
        // 240 Ticks = 4 seconds
        public int holoRefreshInterval = 240; 

        public CompProperties_Holodeck()
        {
            this.compClass = typeof(CompHolodeck);
        }
    }

    public class CompHolodeck : ThingComp
    {
        public CompProperties_Holodeck Props => (CompProperties_Holodeck)this.props;

        private CompPowerTrader powerComp;
        private bool isTrainingMode = false;
        
        // Hologramm Timer
        private int nextHoloTick = 0;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = this.parent.GetComp<CompPowerTrader>();
            // Initialisiere Timer
            nextHoloTick = Find.TickManager.TicksGame + 10;
        }

        public override void CompTick()
        {
            base.CompTick();

            bool hasPower = (powerComp != null && powerComp.PowerOn);
            if (!hasPower) return;

            // check every 60 sec ( performance impact )
            if (parent.IsHashIntervalTick(60))
            {
                Pawn activeUser = GetActiveUser();
                isTrainingMode = (activeUser != null);
                
                // set power consumption
                powerComp.PowerOutput = isTrainingMode 
                    ? -Props.powerTrainingMode 
                    : -Props.powerRelaxMode;

                // apply training
                if (isTrainingMode)
                {
                    ApplyTraining(activeUser);
                }
            }

            // Hologramm Partikel Timer
            if (Find.TickManager.TicksGame >= nextHoloTick)
            {
                SpawnHoloFleck();
                nextHoloTick = Find.TickManager.TicksGame + Props.holoRefreshInterval;
            }
        }

/// <summary>
        /// Distribute XP based on the active Isolinear-Chip Program (RecipeDef)
        /// </summary>
        private void ApplyTraining(Pawn p)
        {
            RecipeDef recipe = p.CurJob?.RecipeDef;
            if (recipe == null) return;

            string recipeName = recipe.defName.ToLower();
            float xp = Props.trainingXpPerTick * 60f; // 60 Ticks = 1 second XP

            // 1. SECURITY / TACTICAL PROGRAMM
            if (recipeName.Contains("security") || recipeName.Contains("tactical") || recipeName.Contains("combat") || recipeName.Contains("shooting") || recipeName.Contains("melee"))
            {
                p.skills.Learn(SkillDefOf.Shooting, xp / 1.5f);
                p.skills.Learn(SkillDefOf.Melee, xp / 1.5f);

                // Injury risk (Bruises during Worf-Bat'leth training!)
                if (Rand.Chance(Props.injuryChance * 20f)) 
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, Rand.RangeInclusive(2, 4));
                    p.TakeDamage(dinfo);
                    // Floating text for visual confirmation
                    MoteMaker.ThrowText(p.DrawPos, p.Map, "Holodeck Injury!");
                }
            }
            // 2. SCIENCE PROGRAMM
            else if (recipeName.Contains("science") || recipeName.Contains("medical"))
            {
                p.skills.Learn(SkillDefOf.Intellectual, xp);
                p.skills.Learn(SkillDefOf.Medicine, xp / 2f);
            }
            // 3. TRANSPORTER / ENGINEERING PROGRAMM
            else if (recipeName.Contains("transporter") || recipeName.Contains("engineering"))
            {
                p.skills.Learn(SkillDefOf.Crafting, xp);
            }
            // 4. SOCIAL / DIPLOMACY PROGRAMM (NEU)
            else if (recipeName.Contains("social") || recipeName.Contains("contact"))
            {
                p.skills.Learn(SkillDefOf.Social, xp);
            }
            // FALLBACK: If the recipe has no known name
            else
            {
                p.skills.Learn(SkillDefOf.Intellectual, xp / 2f);
            }
        }

        /// <summary>
        /// Search for a pawn who is doing a bill at the console
        /// </summary>
        private Pawn GetActiveUser()
        {
            if (!parent.Spawned) return null;
            
            IntVec3 cell = parent.InteractionCell;
            List<Thing> thingList = cell.GetThingList(parent.Map);
            
            foreach (Thing t in thingList)
            {
                if (t is Pawn p && !p.Dead && !p.Downed)
                {
                    if (p.CurJobDef == JobDefOf.DoBill)
                    {
                        return p; // User found
                    }
                }
            }
            return null;
        }

        private void SpawnHoloFleck()
        {
            if (parent.Map == null) return;

            string defName = isTrainingMode ? "ST_Holo_Training" : "ST_Holo_Risa";
            FleckDef holoDef = DefDatabase<FleckDef>.GetNamedSilentFail(defName);

            if (holoDef != null)
            {
                FleckMaker.Static(parent.TrueCenter(), parent.Map, holoDef);
            }
            else
            {
                 if (Find.TickManager.TicksGame % 600 == 0)
                    Log.Warning($"[YASTM] CompHolodeck: Could not find FleckDef named '{defName}'. Check ST_Holo_Flecks.xml!");
            }
        }
    }
}