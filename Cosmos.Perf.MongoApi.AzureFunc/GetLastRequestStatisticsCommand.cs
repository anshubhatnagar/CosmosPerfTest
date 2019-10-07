using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Cosmos.Perf.MongoApi.AzureFunc
{
    public static partial class SeedData
    {
        public class GetLastRequestStatisticsCommand : Command<Dictionary<string, object>>
        {
            public override RenderedCommand<Dictionary<string, object>> Render(IBsonSerializerRegistry serializerRegistry)
            {
                return new RenderedCommand<Dictionary<string, object>>(new BsonDocument("getLastRequestStatistics", 1), serializerRegistry.GetSerializer<Dictionary<string, object>>());
            }
        }
    }
}
