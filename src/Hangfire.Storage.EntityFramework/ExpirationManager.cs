using System;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage.EntityFramework.Locking;
using Microsoft.Extensions.Logging;

namespace Hangfire.Storage.EntityFramework
{


	internal class ExpirationManager : IServerComponent
	{
		private readonly ILogger<ExpirationManager> _logger;

		private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromSeconds(1);
		private const int NumberOfRecordsInSinglePass = 1000;

		private static readonly (string, LockableResource)[] ProcessedTables = {
			("AggregatedCounter", LockableResource.Counter),
			("Job", LockableResource.Job),
			("List", LockableResource.List),
			("Set", LockableResource.Set),
			("Hash", LockableResource.Hash)
		};
		public ExpirationManager(ILogger<ExpirationManager> logger)
        {
			_logger = logger;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
