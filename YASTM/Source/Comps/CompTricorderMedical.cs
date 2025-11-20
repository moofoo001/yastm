// Source/Comps/CompTricorderMedical.cs
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound; // <- wichtig für SoundInfo, MaintenanceType, SoundStarter

namespace YASTM
{
    // ------------------------------------------------------------
    // Properties, die exakt zu deiner XML passen
    // <li Class="YASTM.CompProperties_TricorderMedical"> ... </li>
    // ------------------------------------------------------------
    public class CompProperties_TricorderMedical : CompProperties
    {
        // Aus XML befüllte Felder (Namen 1:1 wie in deinen Defs)
        public int cooldownTicks = 6000;         // 100s
        public int scanTicks = 1200;             // 20s Arbeit im Job
        public int range = 12;                   // Zielreichweite
        public string hediffDef = "ST_MedScan_Boost";
        public int hediffMinTicks = 30000;       // 8,3 Min
        public int hediffMaxTicks = 45000;       // 12,5 Min
        public int minMedicine = 0;              // optionales Skill-Gate

        public CompProperties_TricorderMedical()
        {
            compClass = typeof(CompTricorderMedical);
        }
    }

    // ------------------------------------------------------------
    // Backwards-Compat Comp:
    // - Stellt die erwarteten Members für WorkGiver/JobDriver bereit
    // - Nutzt die Werte aus den oben definierten Props
    // ------------------------------------------------------------
    public class CompTricorderMedical : ThingComp
    {
        public CompProperties_TricorderMedical Props => (CompProperties_TricorderMedical)props;

        private int nextUseTick;

        // Häufig genutzte Wrapper-Eigenschaften (robust gegen Nulls)
        public int Range => Props?.range ?? 0;
        public int ScanTicks => Props?.scanTicks ?? 0;
        public int CooldownTicks => Props?.cooldownTicks ?? 0;
        public bool OnCooldown => Find.TickManager.TicksGame < nextUseTick;
        public int CooldownRemainingTicks => Mathf.Max(0, nextUseTick - Find.TickManager.TicksGame);

        // Von deinen bestehenden Klassen erwartete Properties (Fehlermeldung CS1061)
        public string HediffDefName => Props?.hediffDef ?? string.Empty; // für WorkGiver/JobDriver
        public int HediffDuration
            => Mathf.Max(Props?.hediffMinTicks ?? 0, Props?.hediffMaxTicks ?? 0); // simple Anzeige-Dauer

        // Zusatzbequemer Getter (falls woanders benötigt)
        public HediffDef HediffDef => string.IsNullOrEmpty(Props?.hediffDef)
            ? null
            : DefDatabase<HediffDef>.GetNamedSilentFail(Props.hediffDef);

        public int MinMedicine => Mathf.Max(0, Props?.minMedicine ?? 0);

        /// <summary>
        /// Darf der Benutzer jetzt scannen? (Skill-Gate & Cooldown)
        /// </summary>
        public bool CanUseNow(Pawn user)
        {
            if (user == null || user.Dead || !user.Spawned) return false;
            if (OnCooldown) return false;

            if (MinMedicine > 0)
            {
                int lvl = user.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;
                if (lvl < MinMedicine) return false;
            }
            return true;
        }

        /// <summary>
        /// Wird üblicherweise nach einem Toil, der ScanTicks abwartet, aufgerufen.
        /// Wendet den Hediff an und setzt den Cooldown.
        /// </summary>
        public void UseOn(Pawn user, Pawn target)
        {
            if (target == null || target.Destroyed) return;

            // SFX
            var sdef = SoundDef.Named("ST_MedicalTricorder_Scan");
            var info = SoundInfo.InMap(new TargetInfo(target.Position, target.Map), MaintenanceType.None);
            SoundStarter.PlayOneShot(sdef, info);

            // Hediff anwenden
            var hdef = HediffDef;
            if (hdef != null && target.health != null)
            {
                var h = target.health.AddHediff(hdef);
                if (h.TryGetComp<HediffComp_Disappears>() is HediffComp_Disappears disp)
                {
                    int minT = Mathf.Max(0, Props.hediffMinTicks);
                    int maxT = Mathf.Max(minT, Props.hediffMaxTicks);
                    disp.ticksToDisappear = Rand.RangeInclusive(minT, maxT);
                }
            }

            // Cooldown scharf stellen
            int cd = Mathf.Max(0, Props?.cooldownTicks ?? 0);
            nextUseTick = Find.TickManager.TicksGame + cd;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref nextUseTick, "ST_TricorderMed_nextUseTick", 0);
        }
    }
}

