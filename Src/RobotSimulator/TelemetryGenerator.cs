using System;

namespace RobotSimulator
{
    public class TelemetryGenerator
    {
        private static readonly Random random = new Random();
        private static readonly double drillingBaseTemp = 70.0;
        private static readonly double drillBitBaseFriction = 0.5;
        private static readonly double drillingBaseSpeed = 1500.0;
        private static readonly double liquidCoolingBaseTemp = 10;

        public static double ComputeDrillingTemperature(int temporalVariation)
        {
            return drillingBaseTemp + 2 * Math.Sin(temporalVariation) + random.Next(-4, 4) / 10;
        }

        public static double ComputeDrillBitFriction()
        {
            return drillBitBaseFriction + (double)random.Next(-1, 1) / 10;
        }

        public static double ComputeDrillingSpeed(int temporalVariation)
        {
            return drillingBaseSpeed + 50 * Math.Sin(temporalVariation) + random.Next(-10, 10);
        }

        public static double ComputeLiquidCoolingTemperature(int temporalVariation)
        {
            return liquidCoolingBaseTemp + Math.Cos(temporalVariation) + random.Next(-2, 2) / 10;
        }
    }
}