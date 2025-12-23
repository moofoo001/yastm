using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public enum HoloProgram
    {
        RisaVacation,
        BatlethTraining
    }

    public class CompProperties_Holodeck : CompProperties
    {
        public float trainingXpPerTick = 0.15f;
        public float injuryChance = 0.001f;
        public int powerCombatMode = 2000;
        public int powerRelaxMode = 500;
        
        // Neu: Wie oft das Hologramm erneuert wird (passend zur XML solidTime)
        public int holoRefreshInterval = 240; // Alle 4 Sekunden

        public CompProperties_Holodeck()
        {
            this.compClass = typeof(CompHolodeck);
        }
    }

    public class CompHolodeck : ThingComp
    {
        public CompProperties_Holodeck Props => (CompProperties_Holodeck)props;
        private HoloProgram currentProgram = HoloProgram.RisaVacation;
        
        // Timer für das Hologramm
        private int nextHoloTick = 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentProgram, "currentProgram", HoloProgram.RisaVacation);
        }

        public override void CompTick()
        {
            base.CompTick();
            
            // Strom prüfen
            var power = parent.TryGetComp<CompPowerTrader>();
            bool hasPower = (power != null && power.PowerOn);

            // 1. Energie-Verbrauch setzen (Nur selten checken spart CPU)
            if (parent.IsHashIntervalTick(60)) 
            {
                if (power != null)
                {
                    power.PowerOutput = (currentProgram == HoloProgram.BatlethTraining) 
                        ? -Props.powerCombatMode 
                        : -Props.powerRelaxMode;
                }
                
                // User-Check (Logik bleibt gleich)
                if (hasPower) CheckForUsers();
            }

            // 2. HOLOGRAMM PROJEKTION (Der Fleck)
            // Wir spawnen es rhythmisch, solange Strom da ist.
            if (hasPower && Find.TickManager.TicksGame >= nextHoloTick)
            {
                SpawnHoloFleck();
                nextHoloTick = Find.TickManager.TicksGame + Props.holoRefreshInterval;
            }
        }
        
        private void SpawnHoloFleck()
        {
            if (parent.Map == null) return;

            string defName = (currentProgram == HoloProgram.BatlethTraining) 
                ? "ST_Holo_Combat" 
                : "ST_Holo_Risa";

            FleckDef holoDef = DefDatabase<FleckDef>.GetNamedSilentFail(defName);
            
            // Fallback, falls XML nicht gefunden wird (damit man zumindest irgendwas sieht)
            if (holoDef == null)
            {
                 // Wir nutzen Standard-Flecks als Notlösung
                 holoDef = (currentProgram == HoloProgram.BatlethTraining) 
                    ? FleckDefOf.PsycastAreaEffect // Lila Kreis
                    : FleckDefOf.Heart; // Herzchen
            }

            // Wichtig: Spawn exakt in der Mitte
            FleckMaker.Static(parent.TrueCenter(), parent.Map, holoDef);
        }

        private void CheckForUsers()
        {
             IntVec3 interactionCell = parent.InteractionCell;
             List<Thing> thingList = interactionCell.GetThingList(parent.Map);
             foreach (Thing t in thingList)
             {
                 if (t is Pawn pawn && !pawn.Dead && !pawn.Downed)
                 {
                     if (pawn.CurJob != null && pawn.CurJob.targetA.Thing == parent)
                     {
                         ApplyHoloEffects(pawn);
                     }
                 }
             }
        }

        private void ApplyHoloEffects(Pawn pawn)
        {
            if (currentProgram == HoloProgram.BatlethTraining)
            {
                pawn.skills?.Learn(SkillDefOf.Melee, Props.trainingXpPerTick * 60f); 

                if (Rand.Value < (Props.injuryChance * 60f))
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "SAFETY FAILURE!", Color.red);
                    DamageDef dmg = Rand.Value > 0.5f ? DamageDefOf.Blunt : DamageDefOf.Cut;
                    pawn.TakeDamage(new DamageInfo(dmg, 5, 0, -1, parent));
                    pawn.skills?.Learn(SkillDefOf.Melee, 200f); 
                }
            }
        }

        // Keine PostDraw Methode mehr nötig! Das macht jetzt der FleckManager.

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "Program: Risa",
                defaultDesc = "Relaxation mode.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/HoloRisa", false),
                defaultIconColor = currentProgram == HoloProgram.RisaVacation ? Color.cyan : Color.white,
                action = () => 
                {
                    currentProgram = HoloProgram.RisaVacation;
                    nextHoloTick = 0; // Sofortiges Update erzwingen
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Program: Combat",
                defaultDesc = "Combat simulation.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/HoloCombat", false),
                defaultIconColor = currentProgram == HoloProgram.BatlethTraining ? Color.red : Color.white,
                action = () => 
                {
                    currentProgram = HoloProgram.BatlethTraining;
                    nextHoloTick = 0; // Sofortiges Update erzwingen
                }
            };
        }
    }
}