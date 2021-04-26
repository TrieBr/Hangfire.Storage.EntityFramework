namespace Hangfire.Storage.EntityFramework.JobQueue
{
    public class EnqueuedAndFetchedCountDto
    {
        public int? EnqueuedCount { get; set; }
        public int? FetchedCount { get; set; }
    }
}