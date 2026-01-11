

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HarmonyLib;
using Verse;

namespace YASTM.Diagnostics
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatchInspector_PawnBody
    {
        static HarmonyPatchInspector_PawnBody()
        {
            try
            {
                var tPawnBody = AccessTools.TypeByName("Verse.PawnRenderNode_Body");
                var tPawn     = AccessTools.TypeByName("Verse.Pawn");
                if (tPawnBody == null || tPawn == null) return;

                var target = AccessTools.Method(tPawnBody, "GraphicFor", new Type[] { tPawn });
                if (target == null) return;

                var info = Harmony.GetPatchInfo(target);
                if (info == null) return;

                Dump("PREFIX",    info.Prefixes);
                Dump("POSTFIX",   info.Postfixes);
                Dump("FINALIZER", info.Finalizers);
                Dump("TRANSPILER",info.Transpilers);
            }
            catch (Exception e)
            {
                Log.Warning("[YASTM][PatchInspector] init failed: " + e);
            }
        }

        private static void Dump(string kind, ReadOnlyCollection<Patch> patches)
        {
            if (patches == null) return;
            foreach (var p in patches)
            {
                var owner = p.owner ?? "(unknown)";
                var decl  = p.PatchMethod?.DeclaringType?.FullName ?? "?";
                var sig   = p.PatchMethod?.ToString() ?? "?";
                Log.Message($"[YASTM][PatchInspector] {kind}: owner={owner} type={decl} sig={sig}");
            }
        }

        private static void Dump(string kind, IEnumerable<Patch> patches)
        {
            if (patches == null) return;
            foreach (var p in patches)
            {
                var owner = p.owner ?? "(unknown)";
                var decl  = p.PatchMethod?.DeclaringType?.FullName ?? "?";
                var sig   = p.PatchMethod?.ToString() ?? "?";
                Log.Message($"[YASTM][PatchInspector] {kind}: owner={owner} type={decl} sig={sig}");
            }
        }
    }
}

