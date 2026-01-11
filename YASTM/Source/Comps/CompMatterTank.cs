using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace YASTM
{
    public class CompProperties_MatterTank : CompProperties
    {
        public CompProperties_MatterTank()
        {
            this.compClass = typeof(CompMatterTank);
        }
    }

    public class CompMatterTank : ThingComp
    {
        public float MaxCapacity => 200f; 
        public float storedMatter = 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storedMatter, "storedMatter", 0f);
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (parent.IsHashIntervalTick(60))
            {
                if (storedMatter >= MaxCapacity) return;
                AbsorbEverythingAround();
            }
        }

        private void AbsorbEverythingAround()
        {
            // radius 2.9f
            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(parent.Position, 2.9f, true);

            foreach (var cell in cells)
            {
                if (!cell.InBounds(parent.Map)) continue;

                
                var things = cell.GetThingList(parent.Map).ListFullCopy();
                
                foreach (Thing t in things)
                {
                    if (t.def.defName == "ST_ReplicatorFeedstock")
                    {
                        ConsumeItem(t);
                    }
                    else if (t is Pawn p)
                    {
                        CheckPawnInventory(p);
                    }
                }
            }
        }

        private void CheckPawnInventory(Pawn p)
        {
            // carried thing
            if (p.carryTracker != null && p.carryTracker.CarriedThing != null)
            {
                Thing carried = p.carryTracker.CarriedThing;
                if (carried.def.defName == "ST_ReplicatorFeedstock")
                {

                    int count = carried.stackCount;
                    float space = MaxCapacity - storedMatter;
                    int toTake = Mathf.Min(count, (int)space);
                    
                    if (toTake > 0)
                    {
                        AddMatter(toTake);
                        ShowEffect(p.DrawPos, toTake);
                        
                        // remove carried items
                        if (toTake >= count) p.carryTracker.DestroyCarriedThing();
                        else carried.stackCount -= toTake;
                    }
                }
            }

            // inventory items
            if (p.inventory != null && p.inventory.innerContainer != null)
            {
                for (int i = p.inventory.innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing item = p.inventory.innerContainer[i];
                    if (item.def.defName == "ST_ReplicatorFeedstock")
                    {
                        ConsumeItem(item); 
                    }
                }
            }
        }

        private void ConsumeItem(Thing t)
        {
            float space = MaxCapacity - storedMatter;
            int countToTake = Mathf.Min(t.stackCount, (int)space);

            if (countToTake > 0)
            {
                AddMatter(countToTake);
                ShowEffect(t.DrawPos, countToTake);
                
                if (countToTake >= t.stackCount)
                    t.Destroy();
                else
                    t.stackCount -= countToTake;
            }
        }

        private void ShowEffect(Vector3 pos, int amount)
        {
             // Visual effect
            if (parent.Spawned && parent.Map == Find.CurrentMap)
            {
                FleckMaker.ThrowLightningGlow(pos, parent.Map, 0.4f);
                MoteMaker.ThrowText(pos, parent.Map, $"+{amount}", Color.cyan);
            }
        }

        public void AddMatter(float amount)
        {
            storedMatter = Mathf.Min(storedMatter + amount, MaxCapacity);
        }

        public bool TryConsume(float amount)
        {
            if (storedMatter >= amount)
            {
                storedMatter -= amount;
                return true;
            }
            return false;
        }

        public override string CompInspectStringExtra()
        {
            return $"Matter Reserve: {storedMatter:F0} / {MaxCapacity:F0}";
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            float pct = MaxCapacity > 0 ? storedMatter / MaxCapacity : 0f;
            yield return new Gizmo_MatterStatus
            {
                tank = this,
                label = "Matter Tank",
                fillPercent = pct
            };

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Fill Tank",
                    action = () => { storedMatter = MaxCapacity; }
                };
            }
        }
    }

    [StaticConstructorOnStartup]
    public class Gizmo_MatterStatus : Gizmo
    {
        public CompMatterTank tank;
        public string label;
        public float fillPercent;

        private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.6f, 1f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f));

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            Rect barRect = rect.ContractedBy(6f);
            barRect.height = rect.height / 2f;
            
            Widgets.FillableBar(barRect, fillPercent, BarTex, EmptyBarTex, false);
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            if(tank != null)
                Widgets.Label(barRect, $"{tank.storedMatter:F0} / {tank.MaxCapacity:F0}");
            Text.Anchor = TextAnchor.UpperLeft;

            Rect labelRect = new Rect(rect.x, rect.y + 35f, rect.width, 30f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}