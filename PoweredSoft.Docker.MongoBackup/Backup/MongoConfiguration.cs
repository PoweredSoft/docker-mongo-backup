using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoweredSoft.Docker.MongoBackup.Backup
{
    public class MongoConfiguration
    {
        public string AuthenticatingDatabase { get; set; } = "admin";
        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public List<MongoConfigurationExtraArgs> ExtraArgs { get; set; } = new List<MongoConfigurationExtraArgs>();
        public string PathToMongoDump { get; set; }

        public string GetConnectionString()
        {
            var connectionString = $"mongodb://{User}:{Password}@{Host}";
            connectionString += $"/{AuthenticatingDatabase}";

            if (ExtraArgs != null && ExtraArgs.Count > 0)
            {
                var args = ExtraArgs.Select(t => $"{t.Name}={t.Value}");
                var argsDelimited = string.Join("&", args);
                connectionString += $"?{argsDelimited}";
            }

            return connectionString;
        }
    }
}
