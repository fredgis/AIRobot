using Newtonsoft.Json;

namespace RobotSimulator
{
    public class TelemetryMessage
    {
        [JsonProperty("Timestamp")]
        public long Timestamp { get; }
        
        [JsonProperty("drillingTemperature")]
        public double DrillingTemperature { get; }

        [JsonProperty("drillBitFriction")]
        public double DrillBitFriction { get; }

        [JsonProperty("drillingSpeed")]
        public double DrillingSpeed { get; }

        [JsonProperty("liquidCoolingTemperature")]
        public double LiquidCoolingTemperature { get; }

        public TelemetryMessage(long timestamp, double drillingTemperature, double drillBitFriction, double drillingSpeed,
            double liquidCoolingTemperature)
        {
            Timestamp = timestamp;
            DrillingTemperature = drillingTemperature;
            DrillBitFriction = drillBitFriction;
            DrillingSpeed = drillingSpeed;
            LiquidCoolingTemperature = liquidCoolingTemperature;
        }
    }
}