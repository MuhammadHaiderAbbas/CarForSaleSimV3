using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// One of Harbour City's under-construction plots.
    /// Stage advances deterministically with the day count (see CarSales.Sim.ConstructionStages):
    ///   0 Ground (cleared soil, stakes, fence)  →  1 Site (crane, excavator, mixer, hut)  →  2 Draft (concrete frame)  →  3 Final (finished building).
    /// The builder fills <see cref="stageRoots"/> with four prebuilt groups; this component just toggles them as days pass.
    /// Nothing else has to be saved - it is a pure function of the day.
    /// </summary>
    public class ConstructionSite : MonoBehaviour
    {
        [Tooltip("Plan id, e.g. NorthgateSite or DocksideSite.")]
        public string siteId = "NorthgateSite";
        public string displayName = "Northgate Site";
        [Tooltip("Game day the ground was cleared (negative = already under way when the game starts).")]
        public int startDay = 1;
        [Tooltip("Whole days per stage.")]
        [Min(1)] public int daysPerStage = 2;
        [Tooltip("[Ground, Site, Draft, Final] groups built by the Harbour City builder.")]
        public Transform[] stageRoots = new Transform[4];

        int shownStage = -1;
        public int Stage => shownStage < 0 ? CarSales.Sim.ConstructionStages.Ground : shownStage;
        public string StageLabel => CarSales.Sim.ConstructionStages.StageName(Stage);

        void OnEnable() => CarSales.Foundation.EventBus.Subscribe<CarSales.Foundation.DayStarted>(OnDayStarted);
        void OnDisable() => CarSales.Foundation.EventBus.Unsubscribe<CarSales.Foundation.DayStarted>(OnDayStarted);

        void Start() => Refresh();

        void OnDayStarted(CarSales.Foundation.DayStarted e) => Refresh();

        /// <summary>Force a stage now (e.g. a story beat) and remember it as seen.</summary>
        public void SetStage(int stage)
        {
            shownStage = Mathf.Clamp(stage, 0, CarSales.Sim.ConstructionStages.StageCount - 1);
            Apply();
        }

        /// <summary>Jump the site to finished (used when the final building has to be there now).</summary>
        public void Complete() => SetStage(CarSales.Sim.ConstructionStages.Final);

        void Refresh()
        {
            int today = CarSalesServices.DayCycle ? CarSalesServices.DayCycle.CurrentDay : CarRecords.Today;
            int computed = CarSales.Sim.ConstructionStages.StageAt(startDay, Mathf.Max(1, daysPerStage), today);
            int previous = shownStage;
            shownStage = computed;
            Apply();
            if (previous >= 0 && computed > previous && CarSalesServices.Messages)
            {
                string what = CarSales.Sim.ConstructionStages.StageName(computed);
                CarSalesServices.Messages.Add(MessageType.Info, $"{displayName} update",
                    computed >= CarSales.Sim.ConstructionStages.Final
                        ? $"The work at {displayName} is finished - the new building is open."
                        : $"{displayName}: {what}. Construction continues (stage {computed} of 4).");
            }
        }

        void Apply()
        {
            for (int i = 0; i < stageRoots.Length && i < CarSales.Sim.ConstructionStages.StageCount; i++)
                if (stageRoots[i]) stageRoots[i].gameObject.SetActive(i == shownStage);
        }
    }
}
