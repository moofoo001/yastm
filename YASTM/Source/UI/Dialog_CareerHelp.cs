using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace YASTM
{
    public class Dialog_CareerHelp : Window
    {
        private ST_CareerDef selectedCareer; 
        private Vector2 scrollPos;

        // Der Konstruktor ist jetzt "optional" (= null). 
        // Das repariert auch das Problem im Diplomacy-Comms automatisch!
        public Dialog_CareerHelp(ST_CareerDef career = null)
        {
            this.selectedCareer = career;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            // Sicherheits-Check: Falls keine Daten da sind (z.B. Aufruf via Comms)
            if (selectedCareer == null)
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0, 0, inRect.width, 40), "Starfleet Career Database");
                
                Text.Font = GameFont.Small;
                Rect msgRect = new Rect(0, 50, inRect.width, 100);
                Widgets.Label(msgRect, "No specific career data selected.\nAccessing via Diplomacy Console.");
                
                if (Widgets.ButtonText(new Rect(inRect.width / 2 - 60, inRect.height - 40, 120, 30), "Close"))
                {
                    Close();
                }
                return;
            }

            // Normaler Modus (mit Daten)
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 40), selectedCareer.label.CapitalizeFirst());
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(0, 50, inRect.width, inRect.height - 60);
            Rect viewRect = new Rect(0, 0, inRect.width - 16, 1000); 

            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            float curY = 0f;

            if (selectedCareer.requiredTrait != null)
            {
                Widgets.Label(new Rect(0, curY, viewRect.width, 30), "Required Trait: " + selectedCareer.requiredTrait.LabelCap);
                curY += 30f;
            }

            Widgets.Label(new Rect(0, curY, viewRect.width, 30), "Career Path:");
            curY += 35f;

            if (selectedCareer.stages != null)
            {
                foreach (var stage in selectedCareer.stages)
                {
                    string rankName = stage.rank != null ? stage.rank.label.CapitalizeFirst() : "Unknown Rank";
                    string rankLevel = stage.rank != null ? $" (Lvl {stage.rank.level})" : "";

                    Widgets.Label(new Rect(10, curY, viewRect.width - 10, 25), $"• {rankName}{rankLevel}");
                    curY += 25f;

                    if (stage.requirements != null)
                    {
                        string reqText = GetReqString(stage.requirements);
                        if (!reqText.NullOrEmpty())
                        {
                            Widgets.Label(new Rect(30, curY, viewRect.width - 30, 25), reqText);
                            curY += 25f;
                        }
                    }
                }
            }

            Widgets.EndScrollView();
        }

        private string GetReqString(CareerRequirements req)
        {
            List<string> entries = new List<string>();
            
            if (req.minSocialSkill > 0) entries.Add($"Social {req.minSocialSkill}+");
            if (req.minIntellectualSkill > 0) entries.Add($"Intellectual {req.minIntellectualSkill}+");
            if (req.minShootingSkill > 0) entries.Add($"Shooting {req.minShootingSkill}+");
            
            if (req.timeInRankYears > 0) entries.Add($"{req.timeInRankYears} years service");

            return string.Join(", ", entries);
        }
    }
}