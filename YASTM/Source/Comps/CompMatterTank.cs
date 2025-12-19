using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM.Source.Comps
{
    public class CompProperties_MatterTank : CompProperties
    {
        public float capacity = 100f;
        public CompProperties_MatterTank()
        {
            this.compClass = typeof(CompMatterTank);
        }
    }

    public class CompMatterTank : ThingComp
    {
        public CompProperties_MatterTank Props => (CompProperties_MatterTank)props;
        public float storedMatter = 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storedMatter, "storedMatter", 0f);
        }

        public void AddMatter(float amount)
        {
            storedMatter = Mathf.Min(storedMatter + amount, Props.capacity);
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
            return $"Matter: {storedMatter:F0} / {Props.capacity:F0}";
        }

        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            float pct = Props.capacity > 0 ? storedMatter / Props.capacity : 0f;
            
            Gizmo_MatterBar bar = new Gizmo_MatterBar
            {
                tank = this,
                label = "Matter Tank",
                fillPercent = pct
            };
            yield return bar;
        }
    }

    // Die visuelle Darstellung des Balkens
    [StaticConstructorOnStartup]
    public class Gizmo_MatterBar : Gizmo
    {
        
        public CompMatterTank tank;

        public string label;
        public float fillPercent;

        // Caching der Texturen für Performance
        private static Texture2D barTex;
        private static Texture2D BarTex
        {
            get
            {
                if (barTex == null)
                    barTex = SolidColorMaterials.NewSolidColorTexture(Color.yellow);
                return barTex;
            }
        }

        private static Texture2D emptyBarTex;
        private static Texture2D EmptyBarTex
        {
            get
            {
                if (emptyBarTex == null)
                    emptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.gray);
                return emptyBarTex;
            }
        }

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            Rect barRect = rect.ContractedBy(6f);
            barRect.height = rect.height / 2f;
            
            // FIX: Wir nutzen hier unsere gecachten Texture2D statt Materials
            Widgets.FillableBar(barRect, fillPercent, BarTex, EmptyBarTex, false);
            
            // Text
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, $"{tank.storedMatter:F0} / {tank.Props.capacity:F0}");
            Text.Anchor = TextAnchor.UpperLeft;

            // Label oben drüber
            Rect labelRect = new Rect(rect.x, rect.y + 35f, rect.width, 30f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}