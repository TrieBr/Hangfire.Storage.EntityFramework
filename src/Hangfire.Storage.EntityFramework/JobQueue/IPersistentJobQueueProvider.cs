namespace Hangfire.Storage.EntityFramework.JobQueue
{
    public interface IPersistentJobQueueProvider
    {
        string GetProviderName();
        IPersistentJobQueue GetJobQueue();
        IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi();
    }
}