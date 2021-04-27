using System;
using System.Data;
using System.Threading;
using Hangfire.Logging;
using Hangfire.Storage.EntityFramework.Locking;

namespace Hangfire.Storage.EntityFramework
{
	public class EntityFrameworkDistributedLock: IDisposable, IComparable
	{
		private static readonly ILog Logger = LogProvider.GetLogger(typeof(EntityFrameworkDistributedLock));

		private readonly string _resource;
		private readonly TimeSpan _timeout;
		private readonly EntityFrameworkStorage _storage;
		private readonly EntityFrameworkStorageOptions _storageOptions;
		private readonly DateTime _start;
		private readonly CancellationToken _cancellationToken;

		private const int DelayBetweenPasses = 100;

		private readonly IDbConnection _connection;

		public EntityFrameworkDistributedLock(
			IDbConnection connection, string resource, TimeSpan timeout,
			EntityFrameworkStorageOptions storageOptions): 
			this(connection, resource, timeout, storageOptions, CancellationToken.None) { }

		public EntityFrameworkDistributedLock(
			IDbConnection connection, string resource, TimeSpan timeout,
			EntityFrameworkStorageOptions storageOptions, CancellationToken cancellationToken)
		{
			Logger.TraceFormat("EntityFrameworkDistributedLock resource={0}, timeout={1}", resource, timeout);

			_storageOptions = storageOptions;
			_resource = resource;
			_timeout = timeout;
			_connection = connection;
			_cancellationToken = cancellationToken;
			_start = DateTime.UtcNow;
		}

		public string Resource
		{
			get { return _resource; }
		}

		private int AcquireLock()
		{
			return
				_connection
					.Execute(
						$"INSERT INTO `{_storageOptions.TablesPrefix}DistributedLock` (Resource, CreatedAt) "
						+
						"  SELECT @resource, @now " +
						"  FROM dual " +
						"  WHERE NOT EXISTS ( " +
						$"  		SELECT * FROM `{_storageOptions.TablesPrefix}DistributedLock` " +
						"     	WHERE Resource = @resource " +
						"       AND CreatedAt > @expired);",
						new {
							resource = _resource,
							now = DateTime.UtcNow,
							expired = DateTime.UtcNow.Add(_timeout.Negate())
						});
		}

		public void Dispose()
		{
			Release();

			if (_storage != null)
			{
				_storage.ReleaseConnection(_connection);
			}
		}

		internal EntityFrameworkDistributedLock Acquire()
		{
			Logger.TraceFormat("Acquire resource={0}, timeout={1}", _resource, _timeout);

			int insertedObjectCount;
			do
			{
				_cancellationToken.ThrowIfCancellationRequested();

				var timeLeft = _start.Add(_timeout).Subtract(DateTime.UtcNow);
				using (ResourceLock.AcquireOne(
					_connection, _storageOptions.TablesPrefix, 
					timeLeft, _cancellationToken,
					LockableResource.Lock))
				{
					insertedObjectCount = AcquireLock();
				}

				if (ContinueCondition(insertedObjectCount))
				{
					_cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
					_cancellationToken.ThrowIfCancellationRequested();
				}
			}
			while (ContinueCondition(insertedObjectCount));

			if (insertedObjectCount == 0)
			{
				throw new EntityFrameworkDistributedLockException("cannot acquire lock");
			}

			return this;
		}

		private bool ContinueCondition(int insertedObjectCount)
		{
			return insertedObjectCount == 0 && _start.Add(_timeout) > DateTime.UtcNow;
		}

		internal void Release()
		{
			Logger.TraceFormat("Release resource={0}", _resource);

			using (ResourceLock.AcquireOne(
				_connection, _storageOptions.TablesPrefix, 
				_timeout, CancellationToken.None,
				LockableResource.Lock))
			{
				_connection.Execute(
					$"DELETE FROM `{_storageOptions.TablesPrefix}DistributedLock`  " +
					"WHERE Resource = @resource",
					new { resource = _resource });
			}
		}

		public int CompareTo(object obj)
		{
			if (obj == null) return 1;

			var EntityFrameworkDistributedLock = obj as EntityFrameworkDistributedLock;
			if (EntityFrameworkDistributedLock != null)
				return string.Compare(
					this.Resource, EntityFrameworkDistributedLock.Resource,
					StringComparison.OrdinalIgnoreCase);

			throw new ArgumentException("Object is not a EntityFrameworkDistributedLock");
		}
	}
}
