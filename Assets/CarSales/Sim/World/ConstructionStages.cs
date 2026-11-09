namespace CarSales.Sim
{
    /// <summary>
    /// The four stages of a building plot (pure C#, used by the Final Map's under-construction sites and the tests):
    ///   0 Ground  — cleared soil, survey stakes, fence and a "DANGER" board;
    ///   1 Site    — machines on site: tower crane, excavator, mixer, site hut, piles of materials;
    ///   2 Draft   — the concrete frame of the final building is up (it got a crane lift away from done);
    ///   3 Final   — the finished, signed, landscaped building.
    /// Stage advances deterministically with the day count, so nothing extra needs saving.
    /// </summary>
    public static class ConstructionStages
    {
        public const int Ground = 0, Site = 1, Draft = 2, Final = 3;
        public const int StageCount = 4;

        /// <summary>Stage for a site that started on <paramref name="startDay"/>, given today and the days each stage takes.</summary>
        public static int StageAt(int startDay, int daysPerStage, int today)
        {
            if (daysPerStage < 1) daysPerStage = 1;
            int elapsed = today - startDay;
            if (elapsed <= 0) return Ground;
            int stage = elapsed / daysPerStage;
            return stage < 0 ? Ground : (stage > Final ? Final : stage);
        }

        /// <summary>Whole days until the next stage (0 when already final).</summary>
        public static int DaysUntilNextStage(int startDay, int daysPerStage, int today)
        {
            int stage = StageAt(startDay, daysPerStage, today);
            if (stage >= Final) return 0;
            if (daysPerStage < 1) daysPerStage = 1;
            int elapsed = today - startDay;
            int into = elapsed - stage * daysPerStage;
            return daysPerStage - into;
        }

        public static string StageName(int stage)
        {
            switch (stage)
            {
                case Ground: return "Ground works";
                case Site: return "Construction site";
                case Draft: return "Frame (draft) up";
                case Final: return "Finished";
                default: return "Unknown";
            }
        }
    }
}
