using UnityEngine;
using Verse;
using RimWorld;
using System.Text;
using System.Linq; 

namespace YASTM
{
    public class Dialog_CareerHelp : Window
    {
        private Vector2 scrollPosition;
        private static string selectedTab = "Federation"; // standard tab 

        public override Vector2 InitialSize => new Vector2(700f, 600f);

        public Dialog_CareerHelp()
        {
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // titel
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 40), "YASTM Career Database");
            Text.Font = GameFont.Small;

            // tabs
            Rect tabRect = new Rect(0, 45, inRect.width, 30);
            DrawTabs(tabRect);

            // content area
            Rect outRect = new Rect(0, 80, inRect.width, inRect.height - 90);
            string content = GetDynamicContent(selectedTab);
            
            // scrollview
            float height = Text.CalcHeight(content, outRect.width - 20f);
            Rect viewRect = new Rect(0, 0, outRect.width - 20f, height);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Widgets.Label(viewRect, content);
            Widgets.EndScrollView();
        }

        private void DrawTabs(Rect rect)
        {
            float tabWidth = rect.width / 4f;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, tabWidth, 30), "Federation")) selectedTab = "Federation";
            if (Widgets.ButtonText(new Rect(rect.x + tabWidth, rect.y, tabWidth, 30), "Klingon")) selectedTab = "Klingon";
            if (Widgets.ButtonText(new Rect(rect.x + tabWidth * 2, rect.y, tabWidth, 30), "Romulan")) selectedTab = "Romulan";
            if (Widgets.ButtonText(new Rect(rect.x + tabWidth * 3, rect.y, tabWidth, 30), "Ferengi")) selectedTab = "Ferengi";
        }

        private string GetDynamicContent(string tab)
        {
            // def name
            string defName = "";
            switch (tab)
            {
                case "Federation": defName = "ST_Career_Federation_Standard"; break;
                case "Klingon":    defName = "ST_Career_Klingon_Warrior"; break;
                case "Romulan":    defName = "ST_Career_Romulan_Navy"; break;
                case "Ferengi":    defName = "ST_Career_Ferengi_Commerce"; break;
                default: return "Unknown Data.";
            }

            // database lookup
            CareerDef def = DefDatabase<CareerDef>.GetNamedSilentFail(defName);
            
            if (def == null) 
                return $"Error: Could not find CareerDef named '{defName}'. Please check your XML files.";

            // content creation
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== {def.label.ToUpper()} ===");
            sb.AppendLine(def.description);
            sb.AppendLine("");
            sb.AppendLine("Promotion Requirements:");
            sb.AppendLine("------------------------------------------------");

            // sort
            if (def.stages != null)
            {
                foreach (var stage in def.stages.OrderBy(s => s.targetDegree))
                {
                    // get rank name
                    string rankName = "Unknown Rank";
                    if (def.trait != null)
                    {
                        var degreeData = def.trait.degreeDatas.FirstOrDefault(d => d.degree == stage.targetDegree);
                        if (degreeData != null) rankName = degreeData.label.CapitalizeFirst();
                    }

                    sb.AppendLine($"\n>>> PROMOTION TO: {rankName.ToUpper()}");

                    var r = stage.requirements;
                    if (r != null)
                    {
                        // show skills
                        if (r.minSocialSkill > 0)       sb.AppendLine($" - Social Skill: {r.minSocialSkill}+");
                        if (r.minIntellectualSkill > 0) sb.AppendLine($" - Intellectual: {r.minIntellectualSkill}+");
                        if (r.minShootingSkill > 0)     sb.AppendLine($" - Shooting: {r.minShootingSkill}+");
                        if (r.minMeleeSkill > 0)        sb.AppendLine($" - Melee: {r.minMeleeSkill}+");

                        // time in rank
                        if (r.timeInRankYears > 0.01f)  sb.AppendLine($" - Service Time: {r.timeInRankYears} Year(s)");

                        // points 
                        if (r.careerPoints != null && r.careerPoints.Count > 0)
                        {
                            foreach (var cp in r.careerPoints)
                            {
                                // make nice name
                                string niceName = GenText.SplitCamelCase(cp.category);
                                sb.AppendLine($" - Task: {niceName} (x{cp.count})");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine(" - No specific requirements.");
                    }
                }
            }
            
            return sb.ToString();
        }
    }
}