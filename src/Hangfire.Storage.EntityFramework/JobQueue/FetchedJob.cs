﻿namespace Hangfire.Storage.EntityFramework.JobQueue
{
    internal class FetchedJob
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string Queue { get; set; }
    }
}
