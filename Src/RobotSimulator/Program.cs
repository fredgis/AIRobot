using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Client;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Unicode;
using System.Text;
using Newtonsoft.Json;

namespace RobotSimulator
{
    class Program
    {
        private const int TELEMETRY_FREQ_SEC = 1;

        static async Task Main(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) =>
            {
                cancellationTokenSource.Cancel();
            };

            var configuration = BuildConfiguration();
            var connectionString = GetConnectionString(configuration);
            var client = DeviceClient.CreateFromConnectionString(connectionString);

            await client.OpenAsync(cancellationToken);
            int temporalVariation = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                temporalVariation = temporalVariation % 100;
                var drillingTemp = TelemetryGenerator.ComputeDrillingTemperature(temporalVariation);
                var drillBitFriction = TelemetryGenerator.ComputeDrillBitFriction(temporalVariation);
                var drillingSpeed = TelemetryGenerator.ComputeDrillingSpeed(temporalVariation);
                var liquidCoolingTemp = TelemetryGenerator.ComputeLiquidCoolingTemperature(temporalVariation);

                var telemetry = new TelemetryMessage(timestamp, drillingTemp, drillBitFriction, drillingSpeed, liquidCoolingTemp);
                await client.SendEventAsync(new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetry))), cancellationToken);

                temporalVariation += 1;
                await Task.Delay(TELEMETRY_FREQ_SEC * 1000, cancellationToken);
            }
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json", false, false);

            return configurationBuilder.Build();
        }

        private static string GetConnectionString(IConfigurationRoot configuration)
        {
            var connectionString = configuration.GetSection("EdgeHubConnectionString").Value;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("App Settings EdgeHubConnectionString not found.");

            return connectionString;
        }
    }
}
