using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cosmos.Perf.MongoApi.AzureFunc
{
    public static partial class SeedData
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

            var connectionString = Configurator.GetConfigValue("MongoDbConnectionString");
            var databaseName = Configurator.GetConfigValue("MongoDbDatabaseName");
            var containerName = Configurator.GetConfigValue("MongoDbContainerName");

            var mongoClient = new MongoClient(connectionString);
            var database = mongoClient.GetDatabase(databaseName);

            if (freshSeed)
            {
                await mongoClient.GetDatabase(databaseName).DropCollectionAsync(containerName);
                log.LogInformation("Re-seed option selected. Collection dropped.");
            }

            var collection = database.GetCollection<Dictionary<string, object>>(containerName);
            var watch = new Stopwatch();
            
            log.LogInformation($"Seeding database with {seedCount} items.");
            watch.Start();
            double requestCharge = await SeedItems(sampleJson, seedCount, collection, database);
            watch.Stop();

            double secondsElapsed = watch.Elapsed.TotalSeconds;
            //double requestUnitsPerSecond = requestCharge / secondsElapsed;

            log.LogInformation($"Seed operation completed in {secondsElapsed}");
            //log.LogInformation($"Seed operation charged at {requestCharge}");
            //log.LogInformation($"Seed operation charged at {requestUnitsPerSecond}/s");
            
            return new OkObjectResult($"Seed operation successfully finished with {requestCharge} RUs.");
        }

        private static async Task<double> SeedItems(string sampleJson, int seedCount, IMongoCollection<Dictionary<string, object>> container, IMongoDatabase database)
        {
            double requestCharge = 0;

            for (var i = 0; i < seedCount; i++)
            {
                var sampleDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(sampleJson);
                sampleDictionary["id"] = Guid.NewGuid().ToString();

                await container.InsertOneAsync(sampleDictionary);
                //Dictionary<string, object> stats = database.RunCommand(new GetLastRequestStatisticsCommand());
                //requestCharge += (double)stats["RequestCharge"];
            }

            return requestCharge;
        }
    }
}
