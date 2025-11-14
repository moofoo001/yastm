using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_CommsGizmo : CompProperties
    {
        public int cooldownDays = 7; // Aid-Cooldown in Tagen
        public CompProperties_CommsGizmo() { compClass = typeof(CompCommsGizmo); }
    }

    public class CompCommsGizmo : ThingComp
    {
        public CompProperties_CommsGizmo Props => (CompProperties_CommsGizmo)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;

            // Contact Starfleet (max. 2 aktive ST-Quests)
            yield return new Command_Action
            {
                defaultLabel = "Contact Starfleet Command",
                defaultDesc  = "Initiiert eine neue Missionsübertragung (max. 2 aktive ST-Quests).",
                icon         = ContentFinder<Texture2D>.Get("Things/UI/Icons/Gizmos/CallInSupply", false),
                action       = TryContactStarfleet
            };

            // Request Aid (mit Map-weitem Cooldown)
            var mc = parent.Map?.GetComponent(MapComponentResolver.ResolveType());
            int now = Find.TickManager.TicksGame;
            int next = CommsProgressAccessor.GetNextAidAllowedTick(parent.Map);
            bool onCd = now < next;

            var cmdAid = new Command_Action
            {
                defaultLabel = onCd ? $"Request Starfleet Aid (CD { (next - now).ToStringTicksToPeriod() })" : "Request Starfleet Aid",
                defaultDesc  = "Fordert Unterstützung an (Cooldown).",
                icon         = ContentFinder<Texture2D>.Get("Things/UI/Icons/Gizmos/CallInSupply", false),
                action       = TryRequestAid
            };
            if (onCd) cmdAid.Disable($"Cooldown aktiv: { (next - now).ToStringTicksToPeriod() }");
            yield return cmdAid;
        }

        void TryContactStarfleet()
        {
            if (ActiveStarfleetQuests() >= 2)
            {
                Messages.Message("Starfleet ist ausgelastet – max. 2 aktive Missionen gleichzeitig.", MessageTypeDefOf.RejectInput);
                return;
            }

            var map = parent.Map;
            var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, map);

            // Nimm deinen bestehenden Incident für Queststart – Fallbacks möglich
            var def = DefDatabase<IncidentDef>.GetNamedSilentFail("ST_StartObeliskQuest")
                   ?? DefDatabase<IncidentDef>.AllDefs.FirstOrDefault(d => d.defName.StartsWith("GiveQuest_", StringComparison.OrdinalIgnoreCase));
            if (def == null || !def.Worker.CanFireNow(parms))
            {
                Messages.Message("Keine Antwort vom Sternenflottenkommando.", MessageTypeDefOf.RejectInput);
                return;
            }

            def.Worker.TryExecute(parms);
            Messages.Message("Subspace-Kontakt hergestellt. Missionsdaten werden übertragen …", MessageTypeDefOf.PositiveEvent);
        }

        void TryRequestAid()
        {
            var map = parent.Map;
            int now = Find.TickManager.TicksGame;
            int next = CommsProgressAccessor.GetNextAidAllowedTick(map);
            if (now < next)
            {
                Messages.Message($"Aid-Cooldown aktiv ({(next - now).ToStringTicksToPeriod()}).", MessageTypeDefOf.RejectInput);
                return;
            }

            // Beispiel: Trader Arrival (wähle hier deinen Aid-Event)
            var def = DefDatabase<IncidentDef>.GetNamedSilentFail("OrbitalTraderArrival")
                   ?? DefDatabase<IncidentDef>.GetNamedSilentFail("ResourcePodCrash")
                   ?? DefDatabase<IncidentDef>.AllDefs.FirstOrDefault(d => d.category == IncidentCategoryDefOf.Misc);

            if (def == null)
            {
                Messages.Message("Kein Aid-Incident definiert.", MessageTypeDefOf.RejectInput);
                return;
            }

            var parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            if (def.Worker.TryExecute(parms))
            {
                CommsProgressAccessor.SetNextAidAllowedTick(map, now + Props.cooldownDays * 60000);
                Messages.Message("Starfleet-Unterstützung eingeleitet.", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Aid-Übertragung fehlgeschlagen.", MessageTypeDefOf.RejectInput);
            }
        }

        int ActiveStarfleetQuests()
        {
            return Find.QuestManager.QuestsListForReading
                .Count(q => q != null && q.root != null && !string.IsNullOrEmpty(q.root.defName) && q.root.defName.StartsWith("STQ_", StringComparison.OrdinalIgnoreCase));
        }
    }

    // ---------- Reflection-Helfer für MapComponent_CommsProgress ----------
    static class MapComponentResolver
    {
        static Type cached;
        public static Type ResolveType()
        {
            if (cached != null) return cached;
            // suche nach "YASTM.MapComponent_CommsProgress"
            cached = GenTypes.AllTypes.FirstOrDefault(t =>
                t != null && t.Namespace == "YASTM" && t.Name == "MapComponent_CommsProgress");
            return cached ?? typeof(MapComponent); // fallback (wird nie instanziiert)
        }
    }

    static class CommsProgressAccessor
    {
        static FieldInfo fiLower;   // nextAidAllowedTick
        static PropertyInfo piUpper; // NextAidAllowedTick
        static bool triedResolve;

        static void EnsureResolved(object instance)
        {
            if (triedResolve || instance == null) return;
            var t = instance.GetType();
            piUpper = t.GetProperty("NextAidAllowedTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fiLower = t.GetField("nextAidAllowedTick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            triedResolve = true;
        }

        public static int GetNextAidAllowedTick(Map map)
        {
            if (map == null) return 0;
            var mc = map.GetComponent(MapComponentResolver.ResolveType());
            if (mc == null) return 0;
            EnsureResolved(mc);
            if (piUpper != null) return (int)piUpper.GetValue(mc);
            if (fiLower != null) return (int)fiLower.GetValue(mc);
            return 0;
        }

        public static void SetNextAidAllowedTick(Map map, int value)
        {
            if (map == null) return;
            var mc = map.GetComponent(MapComponentResolver.ResolveType());
            if (mc == null) return;
            EnsureResolved(mc);
            if (piUpper != null) { piUpper.SetValue(mc, value); return; }
            if (fiLower != null) { fiLower.SetValue(mc, value); return; }
        }
    }
}
