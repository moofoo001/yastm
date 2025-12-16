// File: YASTM/YASTMMod.cs
using HarmonyLib;
using Verse;

namespace YASTM
{
    public class YASTMMod : Mod
    {
        public YASTMMod(ModContentPack content) : base(content)
        {
            new Harmony("yastm.ideo.culture").PatchAll();
        }
    }
}

