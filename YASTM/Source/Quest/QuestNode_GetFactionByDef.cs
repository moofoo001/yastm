using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace YASTM
{
    public class QuestNode_GetFactionByDef : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> factionDef;

        [NoTranslate]
        public SlateRef<string> storeAs;

        public bool allowEnemy = true;
        public bool allowNeutral = true;

        protected override bool TestRunInt(Slate slate)
        {
            return FindFaction(slate) != null;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Faction faction = FindFaction(slate);
            if (faction != null && !storeAs.GetValue(slate).NullOrEmpty())
            {
                slate.Set(storeAs.GetValue(slate), faction, false);
            }
        }

        private Faction FindFaction(Slate slate)
        {
            string defName = factionDef.GetValue(slate);
            if (defName.NullOrEmpty()) return null;

            FactionDef targetDef = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
            if (targetDef == null) return null;

            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (f.def == targetDef)
                {
                    if (!allowEnemy && f.HostileTo(Faction.OfPlayer)) continue;
                    if (!allowNeutral && !f.HostileTo(Faction.OfPlayer)) continue;
                    return f;
                }
            }
            return null;
        }
    }
}