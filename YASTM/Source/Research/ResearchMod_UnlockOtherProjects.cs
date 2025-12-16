using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    /// <summary>
    /// When the parent research project is completed, this mod will automatically
    /// finish a list of other research projects (usually vanilla ones).
    /// This allows YASTM research to replace parts of the vanilla tech tree.
    /// </summary>
    public class ResearchMod_UnlockOtherProjects : ResearchMod
    {
        // List of research projects that should be finished when this project completes.
        public List<ResearchProjectDef> projects;

        public override void Apply()
        {
            // Do not call base.Apply() here - it is abstract in ResearchMod.

            if (projects == null || projects.Count == 0)
                return;

            ResearchManager manager = Find.ResearchManager;
            if (manager == null)
                return;

            foreach (ResearchProjectDef project in projects)
            {
                if (project == null)
                    continue;

                // Check current progress instead of using ProjectIsFinished / IsFinished
                float progress = manager.GetProgress(project);

                // If progress is lower than the project cost, it is not finished yet
                if (progress < project.baseCost)
                {
                    manager.FinishProject(
                        project,
                        doCompletionDialog: false,
                        researcher: null
                    );
                }
            }
        }
    }
}
