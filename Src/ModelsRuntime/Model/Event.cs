namespace afpredict
{
    public class Event
    {
        public long ID { get; set; }
        public long Timestamp { get; set; }
        public double DrillingTemperature { get; set; }
        public double DrillBitFriction { get; set; }
        public double DrillingSpeed { get; set; }
        public double LiquidCoolingTemperature { get; set; }
    }
}