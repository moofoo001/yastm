using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    // Die Daten-Klasse für das Fass
    public class CompBloodwineVat : ThingComp
    {
        public int wormCount;
        public float fermentationProgress;
        
        // Konfiguration
        public const int MaxCapacity = 25;
        public const float DaysToFerment = 6f; // Dauert 6 Tage (Klingonen mögen es stark)
        public const float MinIdealTemp = 10f; // Warm
        public const float MaxIdealTemp = 40f; 

        public bool Empty => wormCount <= 0;
        public bool Full => wormCount >= MaxCapacity;
        public bool Fermented => fermentationProgress >= 1f;

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!Empty && !Fermented)
            {
                float temp = parent.AmbientTemperature;
                if (temp > MinIdealTemp && temp < MaxIdealTemp)
                {
                    fermentationProgress += 1f / (DaysToFerment * 240f); // 240 RareTicks pro Tag
                }
            }
        }

        public void AddWorms(int count)
        {
            wormCount += count;
            if (wormCount > MaxCapacity) wormCount = MaxCapacity;
            fermentationProgress = 0f; // Neues Blut stoppt Gärung kurz oder setzt zurück? Hier: Reset wenn neu befüllt.
        }

        public Thing TakeOutWine()
        {
            if (!Fermented) return null;

            Thing wine = ThingMaker.MakeThing(ThingDef.Named("ST_Bloodwine"));
            wine.stackCount = wormCount;
            
            // Reset
            wormCount = 0;
            fermentationProgress = 0f;

            return wine;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            if (!Empty)
            {
                if (Fermented)
                    sb.AppendLine("ST_VatReady".Translate(wormCount));
                else
                    sb.AppendLine("ST_VatFermenting".Translate(fermentationProgress.ToStringPercent(), wormCount));
                
                if (parent.AmbientTemperature < MinIdealTemp)
                    sb.AppendLine("ST_VatTooCold".Translate());
                else if (parent.AmbientTemperature > MaxIdealTemp)
                    sb.AppendLine("ST_VatTooHot".Translate());
            }
            else
            {
                sb.AppendLine("ST_VatEmpty".Translate());
            }
            return sb.ToString().TrimEnd();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref wormCount, "wormCount", 0);
            Scribe_Values.Look(ref fermentationProgress, "progress", 0f);
        }
    }
}