using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace YASTM.SafeUI
{
    [HarmonyPatch(typeof(SocialCardUtility), "DrawPawnRoleSelection")]
    public static class Patch_SocialCardRoleGuard
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Rect rect)
        {
            // Sicherung gegen Abstürze
            try
            {
                // Versuchen, die Starfleet-Komponente zu holen
                var careerComp = pawn.TryGetComp<CompStarfleetCareer>();
                
                // --- ENTSCHEIDUNG ---
                // Wenn der Pawn KEIN Offizier ist (oder die Komponente fehlt):
                // -> "return true" = Führe Vanilla-Code aus (zeige normalen "Assign Role" Knopf).
                if (careerComp == null || !careerComp.IsStarfleetMember) 
                {
                    return true; 
                }

                // Wenn er EIN Offizier ist:
                // -> Zeichne unseren eigenen Knopf.
                bool success = DrawStarfleetRoleButton(pawn, rect, careerComp);
                
                // Wenn wir erfolgreich gezeichnet haben, blockieren wir Vanilla (return false).
                // Wenn beim Zeichnen was schief ging, lassen wir Vanilla als Backup zu (return true).
                return !success; 
            }
            catch (System.Exception ex)
            {
                // Im Notfall: Vanilla-Button zeigen, damit UI nicht kaputt geht.
                Log.WarningOnce($"[YASTM] RoleButton Fehler: {ex.Message}", pawn.thingIDNumber);
                return true;
            }
        }

        private static bool DrawStarfleetRoleButton(Pawn pawn, Rect rect, CompStarfleetCareer career)
        {
            // Daten holen (Sicherheits-Checks inkl.)
            ST_RankDef rank = career.CurrentRank;
            
            // Icon laden
            Texture2D icon = null;
            if (rank != null && !rank.iconPath.NullOrEmpty())
            {
                icon = ContentFinder<Texture2D>.Get(rank.iconPath, true);
            }
            if (icon == null) icon = BaseContent.BadTex; // Fallback: Rotes X

            // Label bestimmen
            string label = rank != null ? rank.label.CapitalizeFirst() : "No Rank";

            // --- ZEICHNEN ---
            Widgets.DrawHighlightIfMouseover(rect);

            // 1. Icon malen
            Rect iconRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(2f);
            GUI.DrawTexture(iconRect, icon);

            // 2. Text malen
            Rect labelRect = new Rect(rect.x + rect.height + 5f, rect.y, rect.width - rect.height - 5f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            // 3. Button-Funktion (Klick)
            if (Widgets.ButtonInvisible(rect))
            {
                OpenStarfleetRoleMenu(pawn, career);
            }
            
            // Tooltip
            TooltipHandler.TipRegion(rect, $"Starfleet Rank: {label}\n(Click to manage)");

            return true; // Zeichnen war erfolgreich
        }

        private static void OpenStarfleetRoleMenu(Pawn pawn, CompStarfleetCareer career)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            list.Add(new FloatMenuOption("Open Career File", () => 
            {
                // Hier öffnen wir später das Dialog-Fenster
                Log.Message("[YASTM] Opening Career Dialog..."); 
            }));

            // Debug-Optionen (später entfernen oder mit Admin-Rechten koppeln)
            list.Add(new FloatMenuOption("Debug: Promote", () => 
            {
                Log.Message("[YASTM] Promote clicked");
                // career.Promote(); // Aktivieren, wenn Methode existiert
            }));

            Find.WindowStack.Add(new FloatMenu(list));
        }
    }
}