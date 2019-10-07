using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cosmos.Perf.SqlApi.AzureFunc
{
    public static class SeedData
    {
        [FunctionName("SeedData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string sampleJson = await new StreamReader(req.Body).ReadToEndAsync();

            if (JsonConvert.DeserializeObject(sampleJson) == null)
            {
                return new BadRequestObjectResult("Sample Json required in request body.");
            }

            bool freshSeed = Convert.ToBoolean(req.Query["freshSeed"]);
            int seedCount = Convert.ToInt32(req.Query["seedCount"]);

            if (seedCount > 5000)
            {
                return new BadRequestObjectResult("'seedCount' cannot exceed 5000.");
            }

            var connectionString = Configurator.GetConfigValue("CosmosDbConnectionString");
            var databaseName = Configurator.GetConfigValue("CosmosDbDatabaseName");
            var containerName = Configurator.GetConfigValue("CosmosDbContainerName");

            var cosmosClient = new CosmosClient(connectionString);
            var database = (Database)await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);

            if (freshSeed)
            {
                await database.GetContainer(containerName).DeleteContainerAsync();
                log.LogInformation("Re-seed option selected. Container removed.");
            }

            var container = (Container)await database.CreateContainerIfNotExistsAsync(containerName, "/id");
            var watch = new Stopwatch();

            log.LogInformation($"Seeding database with {seedCount} items.");
            watch.Start();
            double requestCharge = await SeedItems(sampleJson, seedCount, container);
            watch.Stop();

            double secondsElapsed = watch.Elapsed.TotalSeconds;
            //double requestUnitsPerSecond = requestCharge / secondsElapsed;

            log.LogInformation($"Seed operation completed in {secondsElapsed}");
            //log.LogInformation($"Seed operation charged at {requestCharge}");
            //log.LogInformation($"Seed operation charged at {requestUnitsPerSecond}/s");

            cosmosClient.Dispose();

            return new OkObjectResult($"Seed operation successfully finished with {requestCharge} RUs.");
        }

        private static async Task<double> SeedItems(string sampleJson, int seedCount, Container container)
        {
            double requestCharge = 0;

            for (var i = 0; i < seedCount; i++)
            {
                var sampleDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(sampleJson);
                sampleDictionary["id"] = Guid.NewGuid().ToString();

                var response = await container.CreateItemAsync(sampleDictionary);
                //requestCharge += response.RequestCharge;
            }

            return requestCharge;
        }
    }
}
