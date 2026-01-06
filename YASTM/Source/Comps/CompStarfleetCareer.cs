using System;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompStarfleetCareer : ThingComp
    {
        // FIX: Dies ist die Variable, die im StatPart_Rank.cs gesucht wurde (Fehler CS1061)
        public ST_RankDef CurrentRank;

        // Speicher-Logik, damit der Rang beim Laden nicht verloren geht
        public override void PostExposeData()
        {
            base.PostExposeData();
            // Speichert den Rang als Referenz (Def)
            Scribe_Defs.Look(ref CurrentRank, "ST_CurrentRank");
        }

        public override string CompInspectStringExtra()
        {
            if (CurrentRank != null)
            {
                return "ST_RankLabel".Translate() + ": " + CurrentRank.LabelCap;
            }
            return null;
        }

        // Hilfsmethode zum Setzen des Rangs (für Beförderungen)
        public void Promote(ST_RankDef newRank)
        {
            this.CurrentRank = newRank;
            // Hier könnte man später noch Letter/Messages auslösen
        }
    }
}