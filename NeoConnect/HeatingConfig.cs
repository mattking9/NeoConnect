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
        /// <summary>
        /// The maximum number of hours that preheat will run for if it is not cancelled.
        /// </summary>
        public int? DefaultPreheatDuration { get; set; }

        /// <summary>
        /// If the external temperature is lower than this number then preheat will not be cancelled.
        /// </summary>
        public decimal? ExternalTempThresholdForCancel { get; set; }

        /// <summary>
        /// If the current temperature of the room is this number of degrees below the target temperature then the preheat will not be cancelled.
        /// </summary>
        public decimal? MaxTempDifferenceForCancel { get; set; }
    }

    public class RecipeConfig
    {
        public decimal? ExternalTempThreshold { get; set; }
        public string SummerRecipeName { get; set; }
        public string WinterRecipeName { get; set; }
        public string LastRecipeRun { get; set; }
    }
}


