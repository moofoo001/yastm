using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Verse.Sound;

namespace YASTM
{
    [StaticConstructorOnStartup]
    public class CompShuttleLauncher : ThingComp
    {
        public CompProperties_ShuttleLauncher Props => (CompProperties_ShuttleLauncher)props;
        
        private static readonly Texture2D LaunchIcon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;

            CompTransporter transporter = parent.TryGetComp<CompTransporter>();
            if (transporter == null) yield break;

            Command_Action launch = new Command_Action();
            launch.defaultLabel = "CommandLaunchGroup".Translate();
            launch.defaultDesc = "CommandLaunchGroupDesc".Translate();
            launch.icon = LaunchIcon;
            launch.action = delegate
            {
                StartChoosingDestination();
            };
            
            if (!transporter.LoadingInProgressOrReadyToLaunch)
            {
                launch.Disable("CommandLaunchGroupFailNotConnectedToFuelingPort".Translate());
            }
            else if (transporter.innerContainer.Count == 0)
            {
                launch.Disable("CommandLaunchGroupFailEmpty".Translate());
            }

            yield return launch;
        }

        private void StartChoosingDestination()
        {
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(parent));
            Find.WorldSelector.ClearSelection();
            Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, true, null, true, null, null, null);
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid) return false;

            // store data
            Map map = parent.Map;
            int startTile = parent.Map.Tile;
            IntVec3 startPosition = parent.Position;
            
            // search transport system
            WorldObjectDef podDef = null;
            MethodInfo addMethod = null;
            Type infoType = null;
            bool usesVanillaAddPod = false;

            foreach (var def in DefDatabase<WorldObjectDef>.AllDefs)
            {
                if (def.worldObjectClass == null) continue;

                var mAddPod = def.worldObjectClass.GetMethod("AddPod", BindingFlags.Public | BindingFlags.Instance);
                if (mAddPod != null && mAddPod.GetParameters().Length >= 1)
                {
                    podDef = def;
                    addMethod = mAddPod;
                    infoType = mAddPod.GetParameters()[0].ParameterType;
                    usesVanillaAddPod = true;
                    if (def.defName == "TravelingTransportPods" && def.worldObjectClass.Name != "TravellingTransporters") break;
                }

                var mAddTransporter = def.worldObjectClass.GetMethod("AddTransporter", BindingFlags.Public | BindingFlags.Instance);
                if (mAddTransporter != null && mAddTransporter.GetParameters().Length >= 1)
                {
                    podDef = def;
                    addMethod = mAddTransporter;
                    infoType = mAddTransporter.GetParameters()[0].ParameterType;
                    usesVanillaAddPod = false;
                    break; 
                }
            }

            if (podDef == null)
            {
                Log.Error("[YASTM] CRITICAL: No compatible transport method found via Reflection.");
                return false;
            }

            try
            {
                
                object podInfo = Activator.CreateInstance(infoType);
                IThingHolder podInfoHolder = podInfo as IThingHolder;
                CompTransporter transporter = parent.TryGetComp<CompTransporter>();

                // move inventory
                if (podInfoHolder != null)
                {
                    podInfoHolder.GetDirectlyHeldThings().TryAddRangeOrTransfer(transporter.innerContainer, true);
                }
                else
                {
                    // Fallback for Mods (Odyssey)
                    var innerContainerProp = infoType.GetProperty("innerContainer", BindingFlags.Public | BindingFlags.Instance);
                    if (innerContainerProp != null)
                    {
                        var innerContainer = innerContainerProp.GetValue(podInfo) as ThingOwner;
                        if (innerContainer != null)
                        {
                            innerContainer.TryAddRangeOrTransfer(transporter.innerContainer, true);
                        }
                    }
                }

                // minifiy shuttle
                if (transporter != null)
                {
                    transporter.groupID = -1; 
                }

                // re-claim shuttle
                if (parent.def.Minifiable) 
                {
                    MinifiedThing minifiedShuttle = parent.MakeMinified();
                    if (minifiedShuttle != null) 
                    {
                        if (podInfoHolder != null)
                            podInfoHolder.GetDirectlyHeldThings().TryAdd(minifiedShuttle);
                        else
                        {
                            var innerContainerProp = infoType.GetProperty("innerContainer", BindingFlags.Public | BindingFlags.Instance);
                            if (innerContainerProp != null)
                            {
                                var innerContainer = innerContainerProp.GetValue(podInfo) as ThingOwner;
                                innerContainer?.TryAdd(minifiedShuttle);
                            }
                        }
                    }
                }

                // spawn world object
                WorldObject travelingPods = WorldObjectMaker.MakeWorldObject(podDef);
                travelingPods.Tile = startTile;
                travelingPods.SetFaction(Faction.OfPlayer);
                
                FieldInfo destTileField = podDef.worldObjectClass.GetField("destinationTile", BindingFlags.Public | BindingFlags.Instance);
                if (destTileField != null) destTileField.SetValue(travelingPods, target.Tile);

                // arrival
                object arrivalAction = null;
                Type arrivalActionType = null;

                if (target.WorldObject is MapParent targetMapParent && targetMapParent.HasMap)
                {
                    arrivalActionType = GenTypes.AllTypes.FirstOrDefault(t => t.Name.Contains("TransportPodsArrivalAction_LandInSpecificCell"));
                    if (arrivalActionType != null)
                    {
                        IntVec3 landCell = DropCellFinder.GetBestShuttleLandingSpot(targetMapParent.Map, Faction.OfPlayer);
                        try {
                            arrivalAction = Activator.CreateInstance(arrivalActionType, new object[] { targetMapParent, landCell, false });
                        } catch {
                            arrivalAction = Activator.CreateInstance(arrivalActionType, new object[] { targetMapParent, landCell });
                        }
                    }
                }
                
                if (arrivalAction == null)
                {
                    arrivalActionType = GenTypes.AllTypes.FirstOrDefault(t => t.Name.Contains("TransportPodsArrivalAction_FormCaravan"));
                    if (arrivalActionType != null) 
                    {
                        try {
                            arrivalAction = Activator.CreateInstance(arrivalActionType, new object[] { "ST_ShuttleArrived".Translate() });
                        } catch {
                            arrivalAction = Activator.CreateInstance(arrivalActionType);
                        }
                    }
                }

                if (arrivalAction != null)
                {
                    FieldInfo arrivalActionField = podDef.worldObjectClass.GetField("arrivalAction", BindingFlags.Public | BindingFlags.Instance);
                    if (arrivalActionField != null) arrivalActionField.SetValue(travelingPods, arrivalAction);
                }

                // launch
                Find.WorldObjects.Add(travelingPods);

                object[] parameters;
                if (usesVanillaAddPod) parameters = new object[] { podInfo, true };
                else
                {
                    if (addMethod.GetParameters().Length == 1) parameters = new object[] { podInfo };
                    else parameters = new object[] { podInfo, true };
                }
                
                addMethod.Invoke(travelingPods, parameters);
                // sound

                 ST_SoundDefOf.ST_Shuttle_Launch.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
                // animation
                if (Props.skyfallerLeaving != null && map != null)
                {
                    SkyfallerMaker.SpawnSkyfaller(Props.skyfallerLeaving, startPosition, map);
                }
                
                // clear artefacts
                if (!parent.Destroyed) parent.Destroy(DestroyMode.Vanish);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[YASTM] Launch Exception: {ex}");
                return false;
            }
        }
    }

    public class CompProperties_ShuttleLauncher : CompProperties
    {
        public float maxDistance = 60f;
        public ThingDef skyfallerLeaving;

        public CompProperties_ShuttleLauncher()
        {
            this.compClass = typeof(CompShuttleLauncher);
        }
    }
}