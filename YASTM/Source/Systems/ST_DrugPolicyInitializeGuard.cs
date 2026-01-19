using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace YASTM.Systems
{

    [StaticConstructorOnStartup]
    public static class DrugPolicyInitializeGuard
    {
        static DrugPolicyInitializeGuard()
        {
            var h = new Harmony("YASTM.Systems.DrugPolicyInitializeGuard");

          
            TryFinalizer(h, typeof(DrugPolicy), "InitializeIfNeeded", new[] { typeof(bool) });
            TryFinalizer(h, typeof(DrugPolicy), "Initialize",         new[] { typeof(bool) });
            TryFinalizer(h, typeof(DrugPolicy), "InitializeIfNeeded", null); 
            TryFinalizer(h, typeof(DrugPolicy), "Initialize",         null);

          
            TryPostfix(h, typeof(DrugPolicyDatabase), "MakeNewDrugPolicy", null);
            TryPostfix(h, typeof(DrugPolicyDatabase), "MakePolicy",        null);
            TryPostfix(h, typeof(DrugPolicyDatabase), "GenerateStartingDrugPolicies", null);
        }

        private static void TryFinalizer(Harmony h, Type type, string methodName, Type[] paramTypes)
        {
            try
            {
                MethodInfo m = (paramTypes == null)
                    ? AccessTools.Method(type, methodName)
                    : AccessTools.Method(type, methodName, paramTypes);

                if (m == null) return;
                h.Patch(m, finalizer: new HarmonyMethod(typeof(DrugPolicyInitializeGuard), nameof(Finalizer)));
            }
            catch {} // ignore on failure – goal is maximum robustness
        }

        private static void TryPostfix(Harmony h, Type type, string methodName, Type[] paramTypes)
        {
            try
            {
                MethodInfo m = (paramTypes == null)
                    ? AccessTools.Method(type, methodName)
                    : AccessTools.Method(type, methodName, paramTypes);

                if (m == null) return;
                h.Patch(m, postfix: new HarmonyMethod(typeof(DrugPolicyInitializeGuard), nameof(PostfixSanitizeFactory)));
            }
            catch {} // ignore on failure – goal is maximum robustness
        }

        // ---- Harmony hooks ----

        
        public static Exception Finalizer(object __instance, Exception __exception)
        {
            try
            {
                if (__instance is DrugPolicy dp)
                    SanitizePolicy(dp);
            }
            catch {} // ignore  

          
            return null;
        }

       
        public static void PostfixSanitizeFactory(object __result)
        {
            try
            {
                if (__result is DrugPolicy dp)
                    SanitizePolicy(dp);
                else
                {
                  
                    if (__result is IEnumerable enumerable)
                    {
                        foreach (var obj in enumerable)
                            if (obj is DrugPolicy p) SanitizePolicy(p);
                    }
                }
            }
            catch { /* ignore */ }
        }

        // ---- Kern: Sanitizer ----

        private static void SanitizePolicy(DrugPolicy policy)
        {
            if (policy == null) return;

           
            List<DrugPolicyEntry> typed = null;
            IList untyped = null;

            foreach (var f in policy.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (typeof(List<DrugPolicyEntry>).IsAssignableFrom(f.FieldType))
                {
                    typed = (List<DrugPolicyEntry>)f.GetValue(policy);
                    break;
                }
                if (typeof(IList).IsAssignableFrom(f.FieldType) && f.FieldType.IsGenericType)
                {
                    var genArg = f.FieldType.GetGenericArguments()[0];
                    if (genArg.FullName == "RimWorld.DrugPolicyEntry")
                        untyped = (IList)f.GetValue(policy);
                }
            }

            if (typed != null)
            {
             
                typed.RemoveAll(e => e == null || e.drug == null);
           
                typed.Sort((a, b) =>
                {
                    string la = a?.drug?.label ?? string.Empty;
                    string lb = b?.drug?.label ?? string.Empty;
                    return string.Compare(la, lb, StringComparison.OrdinalIgnoreCase);
                });
                return;
            }

            if (untyped != null)
            {
             
                var keep = new List<object>(untyped.Count);
                for (int i = 0; i < untyped.Count; i++)
                {
                    var entry = untyped[i];
                    if (entry == null) continue;

                  
                    var drugField = entry.GetType().GetField("drug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var drug = drugField?.GetValue(entry);
                    if (drug == null) continue;

                    keep.Add(entry);
                }

              
                keep = keep
                    .OrderBy(e =>
                    {
                        var drugField = e.GetType().GetField("drug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        var drug = drugField?.GetValue(e);
                        var labelProp = drug?.GetType().GetProperty("label", BindingFlags.Instance | BindingFlags.Public);
                        string label = labelProp?.GetValue(drug) as string ?? string.Empty;
                        return label.ToUpperInvariant();
                    })
                    .ToList();

                untyped.Clear();
                foreach (var e in keep) untyped.Add(e);
            }
        }
    }


    public class GameComponent_DrugPolicySanitizer : GameComponent
    {
        public GameComponent_DrugPolicySanitizer(Game game) { }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            SanitizeAllPolicies();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            SanitizeAllPolicies();
        }

        private static void SanitizeAllPolicies()
        {
            try
            {
                var db = Current.Game?.drugPolicyDatabase;
                if (db == null) return;

              
                List<DrugPolicy> list = null;

                var prop = db.GetType().GetProperty("AllPolicies", BindingFlags.Instance | BindingFlags.Public);
                if (prop != null && typeof(IEnumerable<DrugPolicy>).IsAssignableFrom(prop.PropertyType))
                {
                    list = (prop.GetValue(db) as IEnumerable<DrugPolicy>)?.ToList();
                }
                if (list == null)
                {
                    foreach (var f in db.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (typeof(List<DrugPolicy>).IsAssignableFrom(f.FieldType))
                        {
                            list = (List<DrugPolicy>)f.GetValue(db);
                            break;
                        }
                    }
                }

                if (list == null) return;
                foreach (var p in list) DrugPolicyInitializeGuard_SafeSanitize(p);
            }
            catch { /* ignore */ }
        }

        
        private static void DrugPolicyInitializeGuard_SafeSanitize(DrugPolicy dp)
        {
            try { DrugPolicyInitializeGuard_SanitizeShim(dp); } catch { /* ignore */ }
        }

        private static void DrugPolicyInitializeGuard_SanitizeShim(DrugPolicy dp)
        {
       
            var mi = typeof(DrugPolicyInitializeGuard).GetMethod("SanitizePolicy", BindingFlags.NonPublic | BindingFlags.Static);
            mi?.Invoke(null, new object[] { dp });
        }
    }
}

