using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.Docker.MongoBackup.Retention
{
    public class RetentionOptions
    {
        public bool Enabled { get; set; } = false;

        public int Days { get; set; } = 0;
        public int Months { get; set; } = 0;
    }
}
