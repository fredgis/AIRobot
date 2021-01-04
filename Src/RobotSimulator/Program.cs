using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Client;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

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
                Logger.LogInfo("TERM signal received.");
                cancellationTokenSource.Cancel();
            };

            Logger.LogInfo("Loading configuration.");
            var configuration = BuildConfiguration();
            var connectionString = GetConnectionString(configuration);

            Logger.LogInfo("Installing client certificate.");
            InstallCACert(configuration.GetSection("CACertificatePath").Value);

            var client = DeviceClient.CreateFromConnectionString(connectionString);
            client.OperationTimeoutInMilliseconds = GetIoTHubTimeout(configuration) * 1000;

            bool isConnected = false;

            try
            {
                Logger.LogInfo("Connecting to IoT Hub.");
                await client.OpenAsync();
                isConnected = true;
                Logger.LogInfo("Connected to IoT Hub.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }

            int temporalVariation = 0;

            while (isConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Logger.LogInfo("Computing telemetry.");
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    temporalVariation = temporalVariation % 100;
                    var drillingTemp = TelemetryGenerator.ComputeDrillingTemperature(temporalVariation);
                    var drillBitFriction = TelemetryGenerator.ComputeDrillBitFriction(temporalVariation);
                    var drillingSpeed = TelemetryGenerator.ComputeDrillingSpeed(temporalVariation);
                    var liquidCoolingTemp = TelemetryGenerator.ComputeLiquidCoolingTemperature(temporalVariation);

                    Logger.LogInfo("Sending telemetry to IoT Hub.");
                    var telemetry = new TelemetryMessage(timestamp, drillingTemp, drillBitFriction, drillingSpeed, liquidCoolingTemp);
                    await client.SendEventAsync(new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetry))));
                    Logger.LogInfo("Telemetry sent.");

                    temporalVariation += 1;
                    await Task.Delay(TELEMETRY_FREQ_SEC * 1000, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.ToString());
                    cancellationTokenSource.Cancel();
                }
            }

            if (isConnected)
            {
                await client.CloseAsync();
                client.Dispose();
            }

            Logger.LogInfo("Closing.");
        }

        private static void InstallCACert(string trustedCACertPath)
        {
            if (!string.IsNullOrWhiteSpace(trustedCACertPath))
            {
                Logger.LogInfo($"User configured CA certificate path: {trustedCACertPath}");

                if (!File.Exists(trustedCACertPath))
                {
                    // cannot proceed further without a proper cert file
                    Logger.LogInfo($"Certificate file not found: {trustedCACertPath}");
                    throw new InvalidOperationException("Invalid certificate file.");
                }
                else
                {
                    Logger.LogInfo($"Attempting to install CA certificate: {trustedCACertPath}");

                    X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(trustedCACertPath)));

                    Logger.LogInfo($"Successfully added certificate: {trustedCACertPath}");
                    store.Close();
                }
            }
            else
            {
                Logger.LogInfo("CA_CERTIFICATE_PATH was not set or null, not installing any CA certificate");
            }
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(processModule?.FileName))
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

        private static uint GetIoTHubTimeout(IConfigurationRoot configuration)
        {
            var timeout = configuration.GetSection("IoTHubTimeoutInSec").Value;

            if (string.IsNullOrWhiteSpace(timeout))
                throw new Exception("App Settings IoTHubTimeoutInSec not found.");

            if (uint.TryParse(timeout, out uint parsedTimeout))
                return parsedTimeout;

            throw new Exception("App Settings IoTHubTimeoutInSec has bad format.");
        }
    }
}
