using System;
using System.Collections.Generic;
using System.Data;
using Hangfire.Logging;
using System.Linq;
using System.Text;
using Hangfire.Annotations;
using Hangfire.Server;
using Hangfire.Storage.EntityFramework.JobQueue;
using Hangfire.Storage.EntityFramework.Locking;
using Hangfire.Storage.EntityFramework.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hangfire.Storage.EntityFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Storage.EntityFramework
{

    /// <summary>
    /// EntityFramework JobStorage. Mostly just a wrapper around other services registered in the service container.
    /// </summary>
    internal class EntityFrameworkStorage : JobStorage, IDisposable
    {
        private readonly ILogger<EntityFrameworkStorage> _logger;
        private readonly HangfireDbContext _dbContext;
        private readonly IStorageConnection _storageConnection;
        private readonly IMonitoringApi _monitoringApi;
        private readonly IOptions<EntityFrameworkStorageConfiguration> _options;
        private readonly IServiceProvider _serviceProvider;

        public EntityFrameworkStorage(ILogger<EntityFrameworkStorage> logger,
            HangfireDbContext dbContext,
            IStorageConnection storageConnection,
            IMonitoringApi monitoringApi,
            IOptions<EntityFrameworkStorageConfiguration> options,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _dbContext = dbContext;
            _storageConnection = storageConnection;
            _monitoringApi = monitoringApi;
            _options = options;
            _serviceProvider = serviceProvider;
        }



        public void Dispose() { }

        public override IStorageConnection GetConnection() => _storageConnection;

        public override IMonitoringApi GetMonitoringApi() => _monitoringApi;

        public override IEnumerable<IServerComponent> GetComponents() => _serviceProvider.GetServices<IServerComponent>();
        
        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for SQL Server job storage:");
            logger.InfoFormat("    Queue poll interval: {0}.", _options.Value.QueuePollInterval);
        }

    }

}
