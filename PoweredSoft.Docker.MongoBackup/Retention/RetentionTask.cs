using PoweredSoft.Docker.MongoBackup.Backup;
using PoweredSoft.Storage.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MongoBackup.Retention
{
    public class RetentionTask : ITask
    {
        private readonly IStorageProvider storageProvider;
        private readonly RetentionOptions retention;
        private readonly BackupOptions backupOptions;

        public RetentionTask(IStorageProvider storageProvider, RetentionOptions retention, BackupOptions backupOptions)
        {
            this.storageProvider = storageProvider;
            this.retention = retention;
            this.backupOptions = backupOptions;
        }

        public int Priority => 2;
        public string Name => "Retention Task";

        public async Task<int> RunAsync()
        {
            if (!retention.Enabled)
            {
                Console.WriteLine($"Retention is disabled, moving on...");
                return 0;
            }

            // determine timestamp.
            var timeSpan = TimeSpan.FromDays(retention.Days).Add(TimeSpan.FromDays(retention.Months * 30));

/*
#if DEBUG
            timeSpan = TimeSpan.FromMinutes(2);
#endif
*/

            var now = DateTime.UtcNow;
            var retentionDate = now.Subtract(timeSpan);


            var files = await storageProvider.GetListAsync(backupOptions.BasePath);
            foreach (var file in files)
            {
                if (file is IFileInfo fileInfo)
                {
                    Console.WriteLine($"Checking if {file.Path} with {fileInfo.CreatedTimeUtc} is small than {retentionDate}");
                    if (fileInfo.FileName.EndsWith(".archive.gz") && fileInfo.CreatedTimeUtc < retentionDate)
                    {
                        Console.WriteLine($"Attempting to delete file {fileInfo.FileName} because {fileInfo.CreatedTimeUtc} < {retentionDate}");
                        await storageProvider.DeleteFileAsync(fileInfo.Path);
                    }
                }
            }

            return 0;
        }
    }
}
