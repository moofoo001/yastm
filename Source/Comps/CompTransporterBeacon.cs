// Source/Comps/CompTransporterBeacon.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    // ------------------------------------------------------------
    // PROPS
    // ------------------------------------------------------------
    public class CompProperties_TransporterBeacon : CompProperties
    {
        // Cooldowns (Ticks) – Info/Kompat; Handling ggf. separat
        public int cooldownBeamInTicks  = 300000;   // 5 Tage
        public int cooldownBeamOutTicks = 120000;   // 2 Tage

        // Reichweite: -1 = unlimitiert (auf derselben Map)
        public int maxBeamOutDistance = 200;

        // Zieltyp einschränken (leer/null = jeder Beacon mit dieser Comp)
        public string targetBeaconDefName = "ST_TransporterBeacon";

        // Bedienung
        public bool requireOperatorAdjacent = true;
        public int  operatorMaxDistance     = 1;

        // Rang-Gates
        public string       minOperatorRankTraitDefName = "ST_Rank_LieutenantJG";
        public List<string> requiredRankTraitsBeamIn    = null; // Listen haben Vorrang, falls gesetzt
        public List<string> requiredRankTraitsBeamOut   = null;

        // Sonstiges
        public int     maxPawnsPerPulse = 1; // bei Select ist das 1; bleibt als Kompat-Feld
        public bool    colonistsOnly    = true;
        public string  soundDefName     = "ST_Transporter_Beam_YASTM";

        public CompProperties_TransporterBeacon()
        {
            compClass = typeof(CompTransporterBeacon);
        }
    }

    // ------------------------------------------------------------
    // COMP
    // ------------------------------------------------------------
    public class CompTransporterBeacon : ThingComp
    {
        public CompProperties_TransporterBeacon Props => (CompProperties_TransporterBeacon)props;

        // --------------------------------------------------------
        // SETTINGS-INTEGRATION (reflektiert optional auf ModSettings)
        // Min(Props.maxBeamOutDistance, SettingsCap) – wenn Settings fehlen, nur Props
        // --------------------------------------------------------
        int EffectiveMaxDistance()
        {
            int propsVal    = Props.maxBeamOutDistance;         // -1 => unlimitiert
            int settingsCap = TryGetSettingsCapOrIntMax();       // int.MaxValue => kein Cap

            if (propsVal <= 0) return settingsCap;               // unlimitiert durch Props => nur Settings-Cap
            return propsVal < settingsCap ? propsVal : settingsCap;
        }

        static int TryGetSettingsCapOrIntMax()
        {
            // Sucht nach YASTM.YASTM_ModSettings.[TransporterMaxDistance|TransporterMaxBeamDistance]
            try
            {
                var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } });

                var t = allTypes.FirstOrDefault(tp => tp.FullName == "YASTM.YASTM_ModSettings");
                if (t == null) return int.MaxValue;

                // 1) statische Property?
                var p = t.GetProperty("TransporterMaxDistance", BindingFlags.Public | BindingFlags.Static)
                     ?? t.GetProperty("TransporterMaxBeamDistance", BindingFlags.Public | BindingFlags.Static);
                if (p != null)
                {
                    var val = p.GetValue(null);
                    if (val is int iv1 && iv1 > 0) return iv1;
                }

                // 2) Instanz über Instance/Current
                var instProp = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                            ?? t.GetProperty("Current",  BindingFlags.Public | BindingFlags.Static);
                var inst = instProp?.GetValue(null);
                if (inst != null)
                {
                    var p2 = t.GetProperty("TransporterMaxDistance", BindingFlags.Public | BindingFlags.Instance)
                          ?? t.GetProperty("TransporterMaxBeamDistance", BindingFlags.Public | BindingFlags.Instance);
                    if (p2 != null)
                    {
                        var val2 = p2.GetValue(inst);
                        if (val2 is int iv2 && iv2 > 0) return iv2;
                    }
                }
            }
            catch { /* still safe */ }

            return int.MaxValue; // kein Cap gefunden
        }

        // --------------------------------------------------------
        // HILFSMETHODEN
        // --------------------------------------------------------
        bool PowerIsOn()
        {
            var p = parent.GetComp<CompPowerTrader>();
            var f = parent.GetComp<CompFlickable>();
            if (f != null && !f.SwitchIsOn) return false;
            return p == null || p.PowerOn;
        }

        bool OperatorSatisfiesRank(Pawn pawn, bool forBeamOut)
        {
            if (pawn == null || pawn.story?.traits == null) return false;

            // Listen haben Vorrang, wenn vorhanden
            var list = forBeamOut ? Props.requiredRankTraitsBeamOut : Props.requiredRankTraitsBeamIn;
            if (list != null && list.Count > 0)
                return pawn.story.traits.allTraits.Any(t => t?.def != null && list.Contains(t.def.defName));

            // Fallback: Mindest-Rang
            if (!string.IsNullOrEmpty(Props.minOperatorRankTraitDefName))
            {
                var td = DefDatabase<TraitDef>.GetNamedSilentFail(Props.minOperatorRankTraitDefName);
                return td != null && pawn.story.traits.HasTrait(td);
            }

            return true; // kein Gate
        }

        Pawn FindOperator()
        {
            IEnumerable<Pawn> pool = parent.Map.mapPawns.FreeColonistsSpawned;

            if (Props.requireOperatorAdjacent)
                pool = pool.Where(p => p.Position.DistanceTo(parent.Position) <= Props.operatorMaxDistance);

            return pool
                .Where(p => !p.Downed && p.Awake() && OperatorSatisfiesRank(p, forBeamOut: true))
                .OrderByDescending(p => p.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0)
                .FirstOrDefault();
        }

        bool TryFindTargetBeacon(out Building target)
        {
            target = null;

            IEnumerable<Building> candidates = parent.Map.listerThings.AllThings
                .OfType<Building>()
                .Where(b => b != parent && b.Spawned);

            // Muss ebenfalls unser Beacon sein (hat unsere Comp)
            candidates = candidates.Where(b => b.TryGetComp<CompTransporterBeacon>() != null);

            // optional: gleicher DefName
            if (!string.IsNullOrEmpty(Props.targetBeaconDefName))
                candidates = candidates.Where(b => b.def?.defName == Props.targetBeaconDefName);

            // powered & nicht defekt
            candidates = candidates.Where(b =>
            {
                var p = b.TryGetComp<CompPowerTrader>();
                return (p == null || p.PowerOn) && !b.IsBrokenDown();
            });

            // Reichweite (Settings-Cap berücksichtigen)
            int eff = EffectiveMaxDistance();
            if (eff > 0)
                candidates = candidates.Where(b => parent.Position.DistanceTo(b.Position) <= eff);

            target = candidates.OrderBy(b => parent.Position.DistanceTo(b.Position)).FirstOrDefault();
            return target != null;
        }

        string Disabled_NoTarget()  => "No valid target beacon. Place and power a second transporter beacon on this map.";
        string Disabled_NoPower()   => "This beacon has no power.";
        string Disabled_NoOperator() =>
            !string.IsNullOrEmpty(Props.minOperatorRankTraitDefName)
                ? $"No qualified operator nearby (min rank: {Props.minOperatorRankTraitDefName})."
                : "No operator nearby.";

        void PlaySound()
        {
            if (string.IsNullOrEmpty(Props.soundDefName)) return;
            var s = DefDatabase<SoundDef>.GetNamedSilentFail(Props.soundDefName);
            if (s != null) SoundStarter.PlayOneShot(s, SoundInfo.InMap(parent));
        }

        void DoBeam(Building target, IEnumerable<Pawn> pawns)
        {
            if (target == null) return;

            foreach (var p in pawns)
            {
                if (!p.Spawned) continue;

                var srcMap = p.Map;
                IntVec3 dest = CellFinder.RandomClosewalkCellNear(target.Position, target.Map, 1);

                p.DeSpawn();
                GenSpawn.Spawn(p, dest, target.Map, Rot4.Random);

                // kleine VFX (ersetzbar durch eigenen Transporter-Fleck)
                FleckMaker.Static(parent.Position, srcMap, FleckDefOf.PsycastAreaEffect);
                FleckMaker.Static(dest, target.Map, FleckDefOf.PsycastAreaEffect);
            }

            PlaySound();
        }

        TargetingParameters BuildTargetingParams()
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetAnimals = !Props.colonistsOnly,
                canTargetLocations = false,

                // Deine Version erwartet Predicate<TargetInfo>
                validator = (TargetInfo ti) =>
                {
                    var p = ti.Thing as Pawn;
                    if (p == null || !p.Spawned || p.Downed || p.Dead) return false;
                    if (Props.colonistsOnly && !p.IsColonist) return false;
                    // Auswahlradius um den QUELL-Beacon (UX)
                    return p.Position.DistanceTo(parent.Position) <= 4.0f;
                }
            };
        }

        // --------------------------------------------------------
        // GIZMOS (nur „Select Pawn“ via Command_Target)
        // --------------------------------------------------------
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            var cmdSelect = new Command_Target
            {
                defaultLabel = "Energize (select pawn)",
                defaultDesc  = "Select a pawn near this beacon to beam to the nearest powered target beacon.",
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ST_EnergizeSelect", false),
                targetingParams = BuildTargetingParams(),
                action = (LocalTargetInfo lti) =>
                {
                    var pawn = lti.Thing as Pawn;
                    if (pawn == null) return;

                    if (!PowerIsOn())
                    {
                        Messages.Message(Disabled_NoPower(), parent, MessageTypeDefOf.RejectInput);
                        return;
                    }
                    if (!TryFindTargetBeacon(out var target))
                    {
                        Messages.Message(Disabled_NoTarget(), parent, MessageTypeDefOf.RejectInput);
                        return;
                    }
                    var op = FindOperator();
                    if (op == null)
                    {
                        Messages.Message(Disabled_NoOperator(), parent, MessageTypeDefOf.RejectInput);
                        return;
                    }

                    // alles ok → nur diesen Pawn beamen
                    DoBeam(target, new[] { pawn });
                }
            };

            if (!PowerIsOn()) cmdSelect.Disable(Disabled_NoPower());
            else if (!TryFindTargetBeacon(out _)) cmdSelect.Disable(Disabled_NoTarget());
            else if (FindOperator() == null) cmdSelect.Disable(Disabled_NoOperator());

            yield return cmdSelect;
        }
    }
}
