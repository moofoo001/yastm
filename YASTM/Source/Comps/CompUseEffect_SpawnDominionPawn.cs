using RimWorld;
using Verse;

namespace YASTM
{
    public class CompProperties_SpawnDominionPawn : CompProperties_UseEffect
    {
        public PawnKindDef pawnKind;

        public CompProperties_SpawnDominionPawn()
        {
            this.compClass = typeof(CompUseEffect_SpawnDominionPawn);
        }
    }

    public class CompUseEffect_SpawnDominionPawn : CompUseEffect
    {
        public CompProperties_SpawnDominionPawn Props => (CompProperties_SpawnDominionPawn)this.props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (Props.pawnKind == null)
            {
                Log.Error("[YASTM] SpawnDominionPawn failed: pawnKind is null.");
                return;
            }

            // 1. generate clone
            PawnGenerationRequest request = new PawnGenerationRequest(
                Props.pawnKind,
                usedBy.Faction, // added to colony
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                colonistRelationChanceFactor: 0f
            );

            Pawn newPawn = PawnGenerator.GeneratePawn(request);

            // 2. place on map (next to the one who opens the pod)
            GenSpawn.Spawn(newPawn, usedBy.Position, usedBy.Map);

            // 3. cool special effects & lore message
            FleckMaker.ThrowSmoke(usedBy.Position.ToVector3(), usedBy.Map, 2f);
            
            if (Props.pawnKind.defName.Contains("JemHadar"))
            {
                Messages.Message($"{newPawn.Name.ToStringShort} has been decanted. Victory is life!", newPawn, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message($"A new {newPawn.Name.ToStringShort} clone has been activated to serve the Founders.", newPawn, MessageTypeDefOf.PositiveEvent);
            }
        }


        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (p.Faction != Faction.OfPlayer)
            {
                return "Only colony members can initiate decanting.";
            }
            return base.CanBeUsedBy(p);
        }
    }
}