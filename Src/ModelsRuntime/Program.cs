using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;

namespace afpredict
{
    public class Program
    {
        private const int ModelDatasetSize = 60;
        private const int MetricsCount = 4;

        static async Task Main(string[] args)
        {
            while (true)
            {
                try
                {
                    double errorThreshold;

                    if (!double.TryParse(Environment.GetEnvironmentVariable("ComputeModelThreshold"), out errorThreshold))
                        throw new Exception("ComputeModelThreshold app settings has bad format.");

                    var edgeSqlConnectionString = Environment.GetEnvironmentVariable("EdgeSqlConnectionString");
                    Logger.LogInformation($"Connecting to SQL Server {edgeSqlConnectionString}...");
                    using SqlConnection connection = new SqlConnection(edgeSqlConnectionString);
                    await connection.OpenAsync();

                    Logger.LogInformation("Connected.");

                    Logger.LogInformation("Loading model pipeline_std.onnx...");
                    var standardizationModel = await GetModelAsync("pipeline_std.onnx", connection);

                    Logger.LogInformation("Loading model model_final.onnx...");
                    var computeModel = await GetModelAsync("model_final.onnx", connection);

                    if (standardizationModel == null || computeModel == null)
                        throw new Exception("Required models not found.");

                    Logger.LogInformation("All models loaded.");

                    Logger.LogInformation("Getting events from database...");
                    var events = await GetEventsAsync(connection);
                    var dataset = ComputeDataset(events);
                    Logger.LogInformation("Done.");

                    Logger.LogInformation("Applying standardisation model...");
                    var standardisedDataset = ApplyStandardizationModel(dataset, standardizationModel);
                    Logger.LogInformation("Done.");

                    Logger.LogInformation("Applying value compute model...");
                    var computedDataset = ApplyComputeModel(standardisedDataset, computeModel);
                    Logger.LogInformation("Done.");

                    Logger.LogInformation("Computing error...");
                    var error = ComputeError(standardisedDataset, computedDataset);
                    Logger.LogInformation($"Done. Error found: {error}.");

                    if (error >= errorThreshold)
                    {
                        Logger.LogInformation("Error higher than configured threshold. Saving faulty dataset...");
                        await StoreFaultyDatasetAsync(error, events);
                        Logger.LogInformation("Done.");
                    }

                    Logger.LogInformation("Cleaning database...");
                    await DeleteEventsAsync(events, connection);
                    Logger.LogInformation("Done.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.ToString());
                }

                await Task.Delay(60 * 1000);
            }
        }

        static async Task StoreFaultyDatasetAsync(double errorScore, HashSet<Event> events)
        {
            CloudStorageAccount storageAccount;
            CloudStorageAccount.TryParse(Environment.GetEnvironmentVariable("EdgeBlobStorageConnectionString"),
                out storageAccount);

            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("data");
            await cloudBlobContainer.CreateIfNotExistsAsync();

            var blob = cloudBlobContainer.GetBlockBlobReference($"faulty-dataset-{Guid.NewGuid().ToString("D")}");
            await blob.UploadTextAsync(JsonConvert.SerializeObject(new
            {
                Score = errorScore,
                Events = events
            }));
        }

        static float[] ApplyStandardizationModel(double[] dataset, byte[] model)
        {
            var denseTensor = new DenseTensor<double>(dataset, new int[] { ModelDatasetSize, MetricsCount });

            var onnxValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<double>("input", denseTensor)
            };

            using var standardizationSession = new InferenceSession(model);
            using var standardisationModelResults = standardizationSession.Run(onnxValues).FirstOrDefault();

            return ((DenseTensor<Single>)standardisationModelResults.Value).ToArray();
        }

        static float[] ApplyComputeModel(float[] dataset, byte[] model)
        {
            var standardisedDenseTensor = new DenseTensor<float>(dataset, new int[] { 1, 60, 4 });

            var onnxValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>("lstm_8_input", standardisedDenseTensor)
            };

            using var computeSession = new InferenceSession(model);
            using var computedValues = computeSession.Run(onnxValues).FirstOrDefault();

            return ((DenseTensor<Single>)computedValues.Value).ToArray();
        }

        static double ComputeError(float[] a, float[] b)
        {
            double error = 0;

            for (var i = 0; i < a.Length; i++)
                error = error + Math.Abs(a[i] - b[i]);

            return error / a.Length;
        }

        static double[] ComputeDataset(HashSet<Event> events)
        {
            if (events.Count != ModelDatasetSize)
                throw new Exception("Current dataset size not valid.");

            var values = new double[ModelDatasetSize * MetricsCount];
            int i = 0;

            foreach (var e in events)
            {
                values[i] = e.DrillingTemperature;
                values[i + 1] = e.DrillBitFriction;
                values[i + 2] = e.DrillingSpeed;
                values[i + 3] = e.LiquidCoolingTemperature;
                i += MetricsCount;
            }

            return values;
        }

        static async Task<HashSet<Event>> GetEventsAsync(SqlConnection connection)
        {
            var events = await connection.QueryAsync<Event>(@"
                SELECT TOP 60 [ID],
                              [Timestamp],
                              [DrillingTemperature],
                              [DrillBitFriction],
                              [DrillingSpeed],
                              [LiquidCoolingTemperature]
                FROM dbo.Events ORDER BY ID");

            return events.ToHashSet();
        }

        static async Task DeleteEventsAsync(HashSet<Event> events, SqlConnection connection)
        {
            await connection.QueryAsync($"DELETE FROM dbo.Events WHERE [ID] IN @Ids", new
            {
                Ids = events.Select(p => p.ID)
            });
        }

        static async Task<byte[]> GetModelAsync(string modelName, SqlConnection connection)
        {
            var model = await connection.QueryFirstOrDefaultAsync<Model>(@"
                SELECT [ID], [Data], [Description] FROM dbo.Models WHERE [Description] = @ModelName",
                new { ModelName = modelName });

            return model?.Data;
        }
    }
}
