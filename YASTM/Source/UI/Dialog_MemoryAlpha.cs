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

        //  LCARS colors
        private readonly Color lcarsOrange = new Color(1f, 0.6f, 0f);
        private readonly Color lcarsPurple = new Color(0.8f, 0.6f, 1f);
        private readonly Color lcarsBlue = new Color(0.6f, 0.8f, 1f);
        private readonly Color lcarsLightBlue = new Color(0.4f, 0.7f, 1f);

        public Dialog_MemoryAlpha()
        {
            this.doCloseButton = false; // LCARS Button
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
            // === LCARS HEADER BAR ===
            Rect headerBar = new Rect(0, 0, inRect.width - 120f, 35f);
            Widgets.DrawBoxSolid(headerBar, lcarsOrange);
            
            Text.Font = GameFont.Medium;
            GUI.color = Color.black;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(headerBar.x, headerBar.y, headerBar.width - 15f, headerBar.height), "MEMORY ALPHA DATABASE");
            
            // LCARS Close Button
            Rect closeRect = new Rect(inRect.width - 110f, 0, 110f, 35f);
            Widgets.DrawBoxSolid(closeRect, lcarsLightBlue);
            Widgets.Label(new Rect(closeRect.x, closeRect.y, closeRect.width - 15f, closeRect.height), "CLOSE");
            if (Widgets.ButtonInvisible(closeRect))
            {
                this.Close();
            }

            // Reset Text
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // === PANELS ===
            Rect leftPanel = new Rect(0, 50f, 250f, inRect.height - 50f);
            Rect rightPanel = new Rect(270f, 50f, inRect.width - 270f, inRect.height - 50f);

            DrawLeftMenu(leftPanel);
            DrawRightContent(rightPanel);
        }

        private void DrawLeftMenu(Rect rect)
        {
            var categories = DefDatabase<ST_HelpDef>.AllDefs.Select(d => d.category).Distinct().OrderBy(c => c).ToList();
            
            float currentY = rect.y;

            foreach (var category in categories)
            {
                // LCARS Category Block (Wide, right-aligned text)
                Rect catRect = new Rect(rect.x, currentY, rect.width, 30f);
                Color blockColor = (selectedCategory == category) ? lcarsPurple : lcarsBlue;
                Widgets.DrawBoxSolid(catRect, blockColor);

                GUI.color = Color.black;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(new Rect(catRect.x, catRect.y, catRect.width - 10f, catRect.height), category.ToUpper());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(catRect))
                {
                    selectedCategory = category;
                }
                currentY += 35f;

                // Draw sub-items when category is open
                if (selectedCategory == category)
                {
                    var articlesInCat = DefDatabase<ST_HelpDef>.AllDefs.Where(d => d.category == category).OrderBy(d => d.listOrder).ToList();
                    foreach (var article in articlesInCat)
                    {
                        //  Smaller LCARS Block, slightly indented
                        Rect btnRect = new Rect(rect.x + 50f, currentY, rect.width - 50f, 22f);
                        Color artColor = (selectedArticle == article) ? lcarsOrange : lcarsLightBlue;
                        
                        // A vertical line next to it, typical LCARS
                        Rect sideLine = new Rect(rect.x, currentY, 40f, 22f);
                        Widgets.DrawBoxSolid(sideLine, artColor);
                        Widgets.DrawBoxSolid(btnRect, artColor);

                        GUI.color = Color.black;
                        Text.Anchor = TextAnchor.MiddleRight;
                        Widgets.Label(new Rect(btnRect.x, btnRect.y, btnRect.width - 10f, btnRect.height), article.title.ToUpper());
                        GUI.color = Color.white;
                        Text.Anchor = TextAnchor.UpperLeft;

                        if (Widgets.ButtonInvisible(btnRect) || Widgets.ButtonInvisible(sideLine))
                        {
                            selectedArticle = article;
                        }
                        currentY += 26f;
                    }
                    currentY += 10f; // space after an open category
                }
            }
        }

        private void DrawRightContent(Rect rect)
        {
            if (selectedArticle == null) return;

            // Content Header (LCARS Style)
            Rect titleBar = new Rect(rect.x, rect.y, rect.width, 35f);
            Widgets.DrawBoxSolid(titleBar, lcarsOrange);
            
            GUI.color = Color.black;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(titleBar.x + 15f, titleBar.y, titleBar.width, titleBar.height), selectedArticle.title.ToUpper());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            Rect innerRect = new Rect(rect.x, rect.y + 50f, rect.width, rect.height - 50f);
            
            if (!string.IsNullOrEmpty(selectedArticle.texturePath))
            {
                Texture2D image = ContentFinder<Texture2D>.Get(selectedArticle.texturePath, false);
                if (image != null)
                {
                    Rect imgRect = new Rect(innerRect.x, innerRect.y, 200f, 200f);
                    GUI.DrawTexture(imgRect, image, ScaleMode.ScaleToFit);
                    innerRect.yMin += 210f; 
                }
            }

            Widgets.LabelScrollable(innerRect, selectedArticle.text, ref rightScrollPosition);
        }
    }
}