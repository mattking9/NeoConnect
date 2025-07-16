namespace NeoConnect
{
    public class ActionsConfig
    {
        public PreHeatOverride PreHeatOverride { get; set; }        
    }

    public class PreHeatOverride
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
}


