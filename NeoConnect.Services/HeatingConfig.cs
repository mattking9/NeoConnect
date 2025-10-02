namespace NeoConnect.Services
{
    public class HeatingConfig
    {   
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

        public List<TemperatureWeighting> ExternalTempROCWeightings { get; set; } = new List<TemperatureWeighting>();

        public List<SunnyAspectWeighting> SunnyAspectROCWeightings { get; set; } = new List<SunnyAspectWeighting>();

        public bool OnlyEnablePreheatForWakeSchedules { get; set; }
    }

    public class TemperatureWeighting
    {
        /// <summary>
        /// When the external temperature forecast is above this level then the MaxPreheatDuration setting will be overridden
        /// </summary>
        public double Temp { get; set; }
        /// <summary>
        /// The override for the maximum number of hours that preheat will run for
        /// </summary>
        public double Weighting { get; set; }
    }

    public class SunnyAspectWeighting
    {
        /// <summary>
        /// When the external temperature forecast is above this level then the MaxPreheatDuration setting will be overridden
        /// </summary>
        public string[] Devices { get; set; }
        /// <summary>
        /// The override for the maximum number of hours that preheat will run for
        /// </summary>
        public double Weighting { get; set; }
    }

    public class RecipeConfig
    {
        public bool Enabled { get; set; } = true;
        public double? ExternalTempThreshold { get; set; }
        public string SummerRecipeName { get; set; }
        public string WinterRecipeName { get; set; }
        public string LastRecipeRun { get; set; }
    }
}


