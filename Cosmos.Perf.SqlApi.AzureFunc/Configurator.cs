using System;
using System.Collections.Generic;
using System.Text;

namespace Cosmos.Perf.SqlApi.AzureFunc
{
    public class Configurator
    {
        public static string GetConfigValue(string key)
        {
            return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
        }
    }
}
