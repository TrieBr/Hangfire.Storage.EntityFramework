using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Entities.Blobs
{
    internal class ServerData
    {
        public int WorkerCount { get; set; }
        public string[] Queues { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}
