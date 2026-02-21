using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class Dialog_MemoryAlpha : Window
    {
        public override Vector2 InitialSize => new Vector2(900f, 650f);

        private ST_HelpDef selectedArticle;
        private string selectedCategory;
        
        private Vector2 rightScrollPosition; 

        public Dialog_MemoryAlpha()
        {
            this.doCloseButton = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;

            var allDefs = DefDatabase<ST_HelpDef>.AllDefs.OrderBy(d => d.listOrder).ToList();
            if (allDefs.Any())
            {
                selectedArticle = allDefs.First();
                selectedCategory = selectedArticle.category;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 40f), "Memory Alpha Database".Colorize(Color.cyan));
            Text.Font = GameFont.Small;

            Widgets.DrawLineHorizontal(0, 40f, inRect.width);

            Rect leftPanel = new Rect(0, 50f, 250f, inRect.height - 100f);
            Rect rightPanel = new Rect(270f, 50f, inRect.width - 270f, inRect.height - 100f);

            DrawLeftMenu(leftPanel);
            DrawRightContent(rightPanel);
        }

        private void DrawLeftMenu(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(4f);
            
            var categories = DefDatabase<ST_HelpDef>.AllDefs.Select(d => d.category).Distinct().OrderBy(c => c).ToList();
            
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(innerRect);

            foreach (var category in categories)
            {
                // Kategorie-Header 
                Rect catRect = ls.GetRect(30f);
                if (selectedCategory == category)
                    Widgets.DrawHighlightSelected(catRect);
                else
                    Widgets.DrawHighlightIfMouseover(catRect);

                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(catRect, " " + category);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;

                if (Widgets.ButtonInvisible(catRect))
                {
                    selectedCategory = category;
                }

                // articels in this category
                if (selectedCategory == category)
                {
                    var articlesInCat = DefDatabase<ST_HelpDef>.AllDefs.Where(d => d.category == category).OrderBy(d => d.listOrder).ToList();
                    foreach (var article in articlesInCat)
                    {
                        Rect btnRect = ls.GetRect(24f);
                        btnRect.xMin += 15f; // tree structure

                        bool isSelected = (selectedArticle == article);
                        if (isSelected)
                            Widgets.DrawHighlightSelected(btnRect);
                        else
                            Widgets.DrawHighlightIfMouseover(btnRect);

                        Text.Anchor = TextAnchor.MiddleLeft;
                        Widgets.Label(btnRect, " - " + article.title);
                        Text.Anchor = TextAnchor.UpperLeft;

                        if (Widgets.ButtonInvisible(btnRect))
                        {
                            selectedArticle = article;
                        }
                    }
                }
                ls.Gap(4f);
            }
            ls.End();
        }

        private void DrawRightContent(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);

            if (selectedArticle == null)
            {
                Widgets.Label(innerRect, "Please select an entry from the database.");
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 35f), selectedArticle.title.Colorize(Color.yellow));
            Text.Font = GameFont.Small;
            Widgets.DrawLineHorizontal(innerRect.x, innerRect.y + 35f, innerRect.width);

            Rect textRect = new Rect(innerRect.x, innerRect.y + 45f, innerRect.width, innerRect.height - 45f);
            
            if (!string.IsNullOrEmpty(selectedArticle.texturePath))
            {
                Texture2D image = ContentFinder<Texture2D>.Get(selectedArticle.texturePath, false);
                if (image != null)
                {
                    Rect imgRect = new Rect(textRect.x, textRect.y, 200f, 200f);
                    GUI.DrawTexture(imgRect, image, ScaleMode.ScaleToFit);
                    textRect.yMin += 210f; 
                }
            }

            Widgets.LabelScrollable(textRect, selectedArticle.text, ref rightScrollPosition);
        }
    }
}