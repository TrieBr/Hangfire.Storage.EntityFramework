using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage.EntityFramework.Configuration;
using Hangfire.Storage.EntityFramework.Entities;
using Hangfire.Storage.EntityFramework.Entities.Blobs;
using Hangfire.Storage.EntityFramework.JobQueue;
using Hangfire.Storage.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.Storage.EntityFramework.Monitoring
{
    /// <summary>
    /// Extension for changing IEnumberable Job to the specified DTo using the expression.
    /// </summary>
    internal static class JobListDtoExtension
    {
        /// <summary>
        /// Converts a list of jobs to the specified Dto using the expression. Automatically parses the State data into a dictionary and passes the state data to the expression function.
        /// </summary>
        public static JobList<TDto> ToJobList<TDto>(this IEnumerable<Entities.Job> jobList, Func<Entities.Job, Dictionary<string, string>, TDto> expr)
        {
            return new JobList<TDto>(jobList.Select(e => new KeyValuePair<string, TDto>(e.Id.ToString(), expr.Invoke(e, Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(e.StateHistory.First().Data)))));
        }
    }

    internal class DistributedDbLock : IDisposable
    {
        public DistributedDbLock(HangfireDbContext dbContext)
        {

        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    internal class EntityFrameworkMonitoringApi : IMonitoringApi
    {
        private readonly HangfireDbContext _dbContext;
        private readonly IOptions<EntityFrameworkStorageConfiguration> _options;
        private readonly IEnumerable<IPersistentJobQueueProvider> _persistentJobQueueProviders;

        public EntityFrameworkMonitoringApi(HangfireDbContext dbContext, IOptions<EntityFrameworkStorageConfiguration> options, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _options = options;
            _persistentJobQueueProviders = serviceProvider.GetServices<IPersistentJobQueueProvider>();
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            var tuples = _persistentJobQueueProviders
                .Select(x => x.GetJobQueueMonitoringApi())
                .SelectMany(x => x.GetQueues(), (monitoring, queue) => new { Monitoring = monitoring, Queue = queue })
                .OrderBy(x => x.Queue)
                .ToArray();

            var result = new List<QueueWithTopEnqueuedJobsDto>(tuples.Length);

            foreach (var tuple in tuples)
            {
                var enqueuedJobIds =
                    tuple.Monitoring.GetEnqueuedJobIds(tuple.Queue, 0, 5).ToArray();
                var counters = tuple.Monitoring.GetEnqueuedAndFetchedCount(tuple.Queue);
                var firstJobs = EnqueuedJobs(enqueuedJobIds, dbLock);

                result.Add(new QueueWithTopEnqueuedJobsDto
                {
                    Name = tuple.Queue,
                    Length = counters.EnqueuedCount ?? 0,
                    Fetched = counters.FetchedCount,
                    FirstJobs = firstJobs
                });
            }

            return result;
        }

        public IList<ServerDto> Servers()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Servers.ToList().Select(e =>
            {
                var data = SerializationHelper.Deserialize<ServerData>(e.Data);
                return new ServerDto
                {
                    Name = e.Id.ToString(),
                    Heartbeat = e.LastHeartbeat,
                    Queues = data.Queues,
                    StartedAt = data.StartedAt ?? DateTime.MinValue,
                    WorkersCount = data.WorkerCount
                };
            }).ToList();
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            var jobIdInt = int.Parse(jobId);
            return
                _dbContext.Jobs
                .Include(e => e.Parameters)
                .Include(e => e.StateHistory)
                .Where(e => e.Id == jobIdInt)
                .Select(e => new JobDetailsDto
                {
                    CreatedAt = e.CreatedAt,
                    ExpireAt = e.ExpireAt,
                    Job = DeserializeJob(e.InvocationData, e.Arguments),
                    History = e.StateHistory.Select(s => new StateHistoryDto
                    {
                        StateName = s.Name,
                        CreatedAt = s.CreatedAt,
                        Reason = s.Reason,
                        Data = new Dictionary<string, string>(
                                    SerializationHelper.Deserialize<Dictionary<string, string>>(s.Data),
                                    StringComparer.OrdinalIgnoreCase),
                    }).ToList(),
                    Properties = new Dictionary<string, string>(e.Parameters.Select(p => new KeyValuePair<string, string>(p.Name, p.Value)))
                }).FirstOrDefault();
        }

        public StatisticsDto GetStatistics()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return new StatisticsDto
            {
                Enqueued = _dbContext.Jobs.Where(e => e.StateName == "Enqueued").Count(),
                Failed = _dbContext.Jobs.Where(e => e.StateName == "Failed").Count(),
                Processing = _dbContext.Jobs.Where(e => e.StateName == "Processing").Count(),
                Scheduled = _dbContext.Jobs.Where(e => e.StateName == "Scheduled").Count(),
                Servers = _dbContext.Servers.Count(),
                Succeeded = _dbContext.Counters.Where(e => e.Key == "stats:succeeded").Sum(e => e.Value) + _dbContext.AggregatedCounters.Where(e => e.Key == "stats:succeeded").Sum(e => e.Value),
                Deleted = _dbContext.Counters.Where(e => e.Key == "stats:deleted").Sum(e => e.Value) + _dbContext.AggregatedCounters.Where(e => e.Key == "stats:succeedeletedded").Sum(e => e.Value),
                Recurring = _dbContext.Sets.Where(e => e.Key == "recurring-jobs").Count(),
                Queues = _persistentJobQueueProviders
                .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
                .Count()
            };
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, from, perPage).ToArray();
            using var dbLock = new DistributedDbLock(_dbContext);
            return EnqueuedJobs(enqueuedJobIds, dbLock);
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage).ToArray();
            using var dbLock = new DistributedDbLock(_dbContext);
            return FetchedJobs(fetchedJobIds, dbLock);
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int @from, int count)
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
                .Where(e => e.StateName == ProcessingState.StateName)
                .Include(e=> e.StateHistory.OrderByDescending(e=>e.Id).FirstOrDefault())
                .Skip(from)
                .Take(count)
                .ToJobList((e, stateData) =>
                    new ProcessingJobDto
                    {
                        Job = DeserializeJob(e.InvocationData,e.Arguments),
                        ServerId = stateData.ContainsKey("ServerId") ? stateData["ServerId"] : stateData["ServerName"],
                        StartedAt = JobHelper.DeserializeDateTime(stateData["StartedAt"]),
                    });
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int @from, int count)
        {

            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
                .Where(e => e.StateName == ScheduledState.StateName)
                .Include(e => e.StateHistory.OrderByDescending(e => e.Id).FirstOrDefault())
                .Skip(from)
                .Take(count)
                .ToJobList((e, stateData) =>
                    new ScheduledJobDto
                    {
                        Job = DeserializeJob(e.InvocationData, e.Arguments),
                        EnqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]),
                        ScheduledAt = JobHelper.DeserializeDateTime(stateData["ScheduledAt"]),
                    });
        }

        public JobList<SucceededJobDto> SucceededJobs(int @from, int count)
        {
            long? ExtractTotalDuration(IReadOnlyDictionary<string, string> stateData)
            {
                const string durationName = "PerformanceDuration";
                const string latencyName = "Latency";
                return stateData.ContainsKey(durationName) && stateData.ContainsKey(latencyName)
                    ? long.Parse(stateData[durationName]) + long.Parse(stateData[latencyName])
                    : default(long?);
            }

            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
                .Where(e => e.StateName == SucceededState.StateName)
                .Include(e => e.StateHistory.OrderByDescending(e => e.Id).FirstOrDefault())
                .Skip(from)
                .Take(count)
                .ToJobList((e, stateData) =>
                    new SucceededJobDto
                    {
                        Job = DeserializeJob(e.InvocationData, e.Arguments),
                        Result = stateData.ContainsKey("Result") ? stateData["Result"] : null,
                        TotalDuration = ExtractTotalDuration(stateData),
                        SucceededAt = JobHelper.DeserializeDateTime(stateData["SucceededAt"]),
                    });
        }

        public JobList<FailedJobDto> FailedJobs(int @from, int count)
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
               .Where(e => e.StateName == FailedState.StateName)
               .Include(e => e.StateHistory.OrderByDescending(e => e.Id).FirstOrDefault())
               .Skip(from)
               .Take(count)
               .ToJobList((e, stateData) =>
                   new FailedJobDto
                   {
                       Job = DeserializeJob(e.InvocationData, e.Arguments),
                       Reason = e.StateHistory.First().Reason,
                       ExceptionDetails = stateData["ExceptionDetails"],
                       ExceptionMessage = stateData["ExceptionMessage"],
                       ExceptionType = stateData["ExceptionType"],
                       FailedAt = JobHelper.DeserializeNullableDateTime(stateData["FailedAt"])
                   });
        }

        public JobList<DeletedJobDto> DeletedJobs(int @from, int count)
        {

            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
               .Where(e => e.StateName == DeletedState.StateName)
               .Include(e => e.StateHistory.OrderByDescending(e => e.Id).FirstOrDefault())
               .Skip(from)
               .Take(count)
               .ToJobList((e, stateData) =>
                   new DeletedJobDto
                   {
                       Job = DeserializeJob(e.InvocationData, e.Arguments),
                       DeletedAt = JobHelper.DeserializeNullableDateTime(stateData["DeletedAt"])
                   });
        }

        public long ScheduledCount()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
                .Where(e => e.StateName == DeletedState.StateName)
                .LongCount();
        }

        public long EnqueuedCount(string queue)
        {
            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);
            return counters.EnqueuedCount ?? 0;
        }

        public long FetchedCount(string queue)
        {
            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            return counters.FetchedCount ?? 0;
        }

        public long FailedCount()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
                .Where(e => e.StateName == FailedState.StateName)
                .LongCount();
        }

        public long ProcessingCount()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
                .Where(e => e.StateName == ProcessingState.StateName)
                .LongCount();
        }

        public long SucceededListCount()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
                .Where(e => e.StateName == SucceededState.StateName)
                .LongCount();
        }

        public long DeletedListCount()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return _dbContext.Jobs
                .Where(e => e.StateName == DeletedState.StateName)
                .LongCount();
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return GetTimelineStats("succeeded", dbLock);
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return GetTimelineStats("failed", dbLock);
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return GetHourlyTimelineStats("succeeded", dbLock);
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            using var dbLock = new DistributedDbLock(_dbContext);
            return GetHourlyTimelineStats("failed", dbLock);
        }

        private IPersistentJobQueueMonitoringApi GetQueueApi(string queueName)
        {
            // TODO: Hash queueName->provider for O(1) lookup.
            var provider = _persistentJobQueueProviders.First(e=>e.GetProviderName()==queueName);
            var monitoringApi = provider.GetJobQueueMonitoringApi();
            return monitoringApi;
        }

        private static Common.Job DeserializeJob(string invocationData, string arguments)
        {
            var data = SerializationHelper.Deserialize<InvocationData>(invocationData);
            data.Arguments = arguments;
            try
            {
                return data.DeserializeJob();
            }
            catch (JobLoadException)
            {
                return null;
            }
        }

        private Dictionary<DateTime, long> GetTimelineStats(
           string type, DistributedDbLock dbLock)
        {
            var endDate = DateTime.UtcNow.Date;
            var beginDate = endDate;
            var dates = new List<DateTime>();
            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                beginDate = endDate.AddDays(-1);
            }

            var keyMaps = dates.ToDictionary(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd")), x => x);

            return GetTimelineStats(keyMaps, dbLock);
        }

        private Dictionary<DateTime, long> GetTimelineStats(
           IDictionary<string, DateTime> keyMaps, DistributedDbLock dbLock)
        {
            var keys = keyMaps.Keys;
            var valuesMap = _dbContext.AggregatedCounters.Where(e => keys.Any(i=>i==e.Key)).Select(e => new { Key = e.Key, Count = e.Value }).ToDictionary(x => (string)x.Key, x => (long)x.Count);

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
            }

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        /// <summary>
        /// dbLock is unused, but ensures the caller is holding a lock.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="dbLock"></param>
        /// <returns></returns>
        private JobList<EnqueuedJobDto> EnqueuedJobs(int[] ids, DistributedDbLock dbLock)
        {


            return _dbContext.Jobs
               .Where(e => e.StateName == EnqueuedState.StateName)
               .Where(e => ids.Any(i => i == e.Id))
               .Include(e => e.StateHistory.OrderByDescending(e => e.Id).FirstOrDefault())
               .ToJobList((e, stateData) =>
                   new EnqueuedJobDto
                   {
                       Job = DeserializeJob(e.InvocationData, e.Arguments),
                       State = e.StateName,
                       EnqueuedAt = e.StateName == EnqueuedState.StateName
                        ? JobHelper.DeserializeNullableDateTime(stateData["EnqueuedAt"])
                        : null
                   });
        }

        private JobList<FetchedJobDto> FetchedJobs(
           int[] ids, DistributedDbLock dbLock)
        {
            return _dbContext.Jobs
              .Where(e => e.StateName == EnqueuedState.StateName)
              .Where(e => ids.Any(i => i == e.Id))
              .Include(e => e.StateHistory.OrderByDescending(e => e.Id).FirstOrDefault())
              .ToJobList((e, stateData) =>
                  new FetchedJobDto
                  {
                      Job = DeserializeJob(e.InvocationData, e.Arguments),
                      State = e.StateName,
                  });
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(
           string type, DistributedDbLock dbLock)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => $"stats:{type}:{x:yyyy-MM-dd-HH}", x => x);

            return GetTimelineStats(keyMaps, dbLock);
        }

    }
}
