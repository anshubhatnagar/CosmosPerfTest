using System;
using System.Collections.Generic;
using System.Text;

namespace Cosmos.Perf.MongoApi.AzureFunc
{
    public class Configurator
    {
        public static string GetConfigValue(string key)
        {
            return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
        }
    }
}
