using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace YASTM.Systems
{
    // Harmony einmalig initialisieren
    [StaticConstructorOnStartup]
    public static class DrugPolicyNullGuardInit
    {
        static DrugPolicyNullGuardInit()
        {
            new Harmony("YASTM.DrugPolicyNullGuard").PatchAll();
        }
    }

    internal static class DrugPolicySanitizer
    {
        // Holt die interne Liste via Reflection (privates Feld "entries")
        private static List<DrugPolicyEntry> GetEntries(DrugPolicy pol)
        {
            if (pol == null) return null;

            var fld = AccessTools.Field(typeof(DrugPolicy), "entries");
            if (fld == null)
            {
                // Fallback: irgendein Feld vom Typ List<DrugPolicyEntry>
                foreach (var f in AccessTools.GetDeclaredFields(typeof(DrugPolicy)))
                {
                    if (typeof(List<DrugPolicyEntry>).IsAssignableFrom(f.FieldType))
                    {
                        fld = f;
                        break;
                    }
                }
            }
            return fld?.GetValue(pol) as List<DrugPolicyEntry>;
        }

        public static void Sanitize(DrugPolicy pol)
        {
            var list = GetEntries(pol);
            if (list == null) return;

            // 1) Nulls raus
            list.RemoveAll(e => e == null || e.drug == null);

            // 2) Doppelte entfernen (Sicherheitsnetz)
            var seen = new HashSet<ThingDef>();
            list.RemoveAll(e => !seen.Add(e.drug));

            // 3) Stabil sortieren (Label, Fallback DefName)
            list.Sort((a, b) =>
            {
                string la = a?.drug?.label ?? a?.drug?.defName ?? string.Empty;
                string lb = b?.drug?.label ?? b?.drug?.defName ?? string.Empty;
                return string.Compare(la, lb, StringComparison.OrdinalIgnoreCase);
            });
        }
    }

    // FINALIZER: fängt die NullReference in InitializeIfNeeded ab und säubert dann
    [HarmonyPatch(typeof(DrugPolicy), "InitializeIfNeeded")]
    public static class Patch_DrugPolicy_InitializeIfNeeded_Finalizer
    {
        static Exception Finalizer(DrugPolicy __instance, Exception __exception)
        {
            if (__exception != null)
            {
                // NRE ignorieren und Liste säubern, damit die Policy verwendbar ist
                DrugPolicySanitizer.Sanitize(__instance);
                return null; // Exception unterdrücken
            }
            return null;
        }
    }

    // Postfix: neu erstellte Policies direkt säubern (nach Ctor)
    [HarmonyPatch(typeof(DrugPolicyDatabase), nameof(DrugPolicyDatabase.MakeNewDrugPolicy))]
    public static class Patch_DrugPolicyDatabase_MakeNewDrugPolicy
    {
        static void Postfix(DrugPolicy __result)
        {
            DrugPolicySanitizer.Sanitize(__result);
        }
    }

    // Postfix: nach Laden/Speichern komplette Liste säubern
    [HarmonyPatch(typeof(DrugPolicyDatabase), "ExposeData")]
    public static class Patch_DrugPolicyDatabase_ExposeData
    {
        static void Postfix(DrugPolicyDatabase __instance)
        {
            try
            {
                var field = AccessTools.Field(typeof(DrugPolicyDatabase), "policies"); // private List<DrugPolicy> policies
                var list = field?.GetValue(__instance) as List<DrugPolicy>;
                if (list == null) return;

                foreach (var pol in list)
                    DrugPolicySanitizer.Sanitize(pol);
            }
            catch (Exception)
            {
                // niemals crashen lassen
            }
        }
    }
}
