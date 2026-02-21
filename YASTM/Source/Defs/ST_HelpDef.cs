using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace YASTM
{
    // helper class for ingame help articles
    public class ST_HelpDef : Def
    {
        public string category = "General"; // For the tabs (e.g. "Factions", "Technology")
        public string title = "Missing Title";
        public string text = "No description provided.";
        public string texturePath = ""; // Optional: Image for the article
        public float listOrder = 0f;    // Sorting in the list
    }
}