using Ionic.Zip;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using PoweredSoft.Docker.MongoBackup.Notifications;
using PoweredSoft.Storage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MongoBackup.Backup
{
    public class BackupTask : ITask
    {
        private readonly INotifyService notifyService;
        private readonly IStorageProvider storageProvider;
        private readonly IConfiguration configuration;
        private readonly BackupOptions backupOptions;
        private readonly MongoConfiguration mongoConfiguration;

        public BackupTask(INotifyService notifyService, IStorageProvider storageProvider, IConfiguration configuration, BackupOptions backupOptions, MongoConfiguration mongoConfiguration)
        {
            this.notifyService = notifyService;
            this.storageProvider = storageProvider;
            this.configuration = configuration;
            this.backupOptions = backupOptions;
            this.mongoConfiguration = mongoConfiguration;
        }

        public int Priority { get; } = 1;
        public string Name { get; } = "Mongo Database backup task.";

        protected virtual IMongoClient GetDatabaseConnection()
        {
            var mongoClient = new MongoClient(this.mongoConfiguration.ConnectionString);
            return mongoClient;
        }

        protected virtual async Task<List<string>> GetDatabaseNamesAsync(IMongoClient client)
        {
            var ret = new List<string>();
            using (var cursor = await client.ListDatabasesAsync())
            {
                var databaseDocuments = await cursor.ToListAsync();
                foreach (var databaseDocument in databaseDocuments)
                    ret.Add(databaseDocument["name"].AsString);
            }

            ret.RemoveAll(t => backupOptions.ExcludedDatabases.Contains(t, StringComparison.InvariantCultureIgnoreCase));
            return ret;
        }

        public async Task<int> RunAsync()
        {
            var client = GetDatabaseConnection();
            Console.WriteLine("Fetching database names to backup...");
            var databaseNames = await GetDatabaseNamesAsync(client);

            foreach (var databaseName in databaseNames)
            {
                if (backupOptions.Databases != "*")
                {
                    var databasesToBackup = backupOptions.Databases.Split(',');
                    if (!databasesToBackup.Any(t => t.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        Console.WriteLine($"Skipping {databaseName} not part of {backupOptions.Databases}");
                        continue;
                    }
                }

                Console.WriteLine($"attempting backup of {databaseName}");
                var tempFileName = Path.GetTempFileName();
                ExecuteDump(databaseName, tempFileName);

                var destination = $"{backupOptions.BasePath}/{databaseName}_{DateTime.Now:yyyyMMdd_hhmmss_fff}.archive.gz";
                using (var fs = new FileStream(tempFileName, FileMode.Open, FileAccess.Read))
                {
                    await storageProvider.WriteFileAsync(fs, destination);
                    Console.WriteLine("Succesfully transfered backup to storage");
                }

                try
                {
                    File.Delete(tempFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not clean up temp file {tempFileName} {ex.Message}");
                }
            };

            return 0;
        }

        protected void ExecuteDump(string databaseName, string tempFileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ExecuteWindowsDump(databaseName, tempFileName);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                ExecuteLinuxDump(databaseName, tempFileName);
        }

        protected string BuildUriWithDatabase(string databaseName)
        {
            var uri = mongoConfiguration.ConnectionString;
            
            // Parse MongoDB URI: mongodb://[user:pass@]host[:port][/database][?options]
            // or mongodb+srv://[user:pass@]host[/database][?options]
            var queryIndex = uri.IndexOf('?');
            var queryString = queryIndex >= 0 ? uri.Substring(queryIndex) : "";
            var baseUri = queryIndex >= 0 ? uri.Substring(0, queryIndex) : uri;
            
            // Find the last '/' before any query parameters
            var lastSlash = baseUri.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                // Replace existing database path
                baseUri = baseUri.Substring(0, lastSlash + 1) + databaseName;
            }
            else
            {
                // Add database path
                baseUri = baseUri + "/" + databaseName;
            }
            
            return baseUri + queryString;
        }

        protected void ExecuteWindowsDump(string databaseName, string tempFileName)
        {
            var uri = BuildUriWithDatabase(databaseName);
            var command = $"{mongoConfiguration.PathToMongoDump} --uri \"{uri}\" --archive=\"{tempFileName}\" --gzip";

            var batFilePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid() + ".bat");

            File.WriteAllText(
                    batFilePath,
                    command,
                    Encoding.ASCII);

            if (File.Exists(tempFileName))
                File.Delete(tempFileName);

            try
            {
                var oInfo = new ProcessStartInfo(batFilePath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(oInfo))
                {
                    if (proc == null) return;
                    proc.WaitForExit();
                    proc.Close();
                }
            }
            finally
            {
                File.Delete(batFilePath);
            }
        }

        protected void ExecuteLinuxDump(string databaseName, string tempFileName)
        {
            var uri = BuildUriWithDatabase(databaseName);

            var result = "";
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "mongodump";
                proc.StartInfo.ArgumentList.Add("--uri");
                proc.StartInfo.ArgumentList.Add(uri);
                proc.StartInfo.ArgumentList.Add("--archive");
                proc.StartInfo.ArgumentList.Add(tempFileName);
                proc.StartInfo.ArgumentList.Add("--gzip");
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();

                result += proc.StandardOutput.ReadToEnd();
                result += proc.StandardError.ReadToEnd();

                Console.WriteLine(result);
                proc.WaitForExit();
            }
        }
    }
}
