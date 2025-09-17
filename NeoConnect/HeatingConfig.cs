namespace NeoConnect
{
    public class HeatingConfig
    {
        public string SummerProfileName { get; set; }
        
        public PreHeatOverrideConfig PreHeatOverride { get; set; } = new PreHeatOverrideConfig();      
        public RecipeConfig Recipes { get; set; } = new RecipeConfig();
    }

    public class PreHeatOverrideConfig
    {
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// The maximum number of hours that preheat will run for if no overrides are applicable.
        /// </summary>
        public int MaxPreheatHours { get; set; } = 5;

        public List<MaxPreHeatOverride> Overrides { get; set; } = new List<MaxPreHeatOverride>();

        public bool OnlyEnablePreheatForWakeSchedules { get; set; }
    }

    public class MaxPreHeatOverride
    {
        /// <summary>
        /// When the external temperature forecast is above this level then the MaxPreheatDuration setting will be overridden
        /// </summary>
        public decimal ExternalTempAbove { get; set; }
        /// <summary>
        /// The override for the maximum number of hours that preheat will run for
        /// </summary>
        public int MaxPreheatHours { get; set; }
    }

    public class RecipeConfig
    {
        public bool Enabled { get; set; } = true;
        public decimal? ExternalTempThreshold { get; set; }
        public string SummerRecipeName { get; set; }
        public string WinterRecipeName { get; set; }
        public string LastRecipeRun { get; set; }
    }
}


