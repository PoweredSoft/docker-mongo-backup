using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.Docker.MongoBackup.Backup
{
    public class BackupOptions
    {
        public string BasePath { get; set; } = "postgres_backups";
        public string Databases { get; set; } = "*";
        public string ExcludedDatabases { get; set; } = "admin,local,config";
    }
}
