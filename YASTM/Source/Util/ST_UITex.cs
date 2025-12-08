using UnityEngine;
using Verse;

namespace YASTM
{
    
    [StaticConstructorOnStartup]
    public static class ST_UITex
    {
        private static Texture2D Load(string path, bool hard = false)
        {
            var tex = ContentFinder<Texture2D>.Get(path, !hard ? false : true);
            if (tex == null) tex = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/Placeholder", false) ?? BaseContent.BadTex;
            return tex;
        }

        public static readonly Texture2D RedAlert    = Load("UI/Icons/Gizmos/RedAlert");
        public static readonly Texture2D YellowAlert = Load("UI/Icons/Gizmos/YellowAlert");
        public static readonly Texture2D GreenAlert  = Load("UI/Icons/Gizmos/GreenAlert");
        public static readonly Texture2D ForceField  = Load("UI/Icons/Gizmos/ForceField");
    }
}

