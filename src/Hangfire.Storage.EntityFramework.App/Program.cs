using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Storage.EntityFramework.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedParameter.Local

namespace Hangfire.Storage.EntityFramework.App
{
	internal static class Program
	{

		public static void Main(string[] args)
		{
			var serviceCollection = new ServiceCollection();
			serviceCollection.AddLogging(e => e.AddConsole());
			Configure(serviceCollection);
			var serviceProvider = serviceCollection.BuildServiceProvider();
			Execute(serviceProvider, args);
		}

		private static void Configure(IServiceCollection services) {

			services.AddHangfireEntityFramework();
			services.AddDbContext<ApplicationDbContext>(options =>
				  options.UseMySql(Configuration.GetConnectionString("DefaultConnection"), new MariaDbServerVersion(new Version(10, 3)),
					  b =>
					  {


						  b.MigrationsAssembly("WebUI.Server");

					  })
				  );

		}

		private static void Execute(IServiceProvider serviceProvider, string[] args)
		{
			var connectionString = "Server=localhost;Database=hangfire;Uid=test;Pwd=test";
			var tablePrefix = "with_locks_";

			using (var storage = new MySqlStorage(
				connectionString, new MySqlStorageOptions { TablesPrefix = tablePrefix }))
			{
				var cancel = new CancellationTokenSource();
				var task = Task.WhenAll(
					Task.Run(() => Producer(loggerFactory, storage, cancel.Token), cancel.Token),
					Task.Run(() => Consumer(loggerFactory, storage, cancel.Token), cancel.Token),
					Task.CompletedTask
				);

				Console.ReadLine();
				cancel.Cancel();
				task.Wait(CancellationToken.None);
			}
		}

		private static Task Producer(
			ILoggerFactory loggerFactory, JobStorage storage, CancellationToken token)
		{
			var logger = loggerFactory.CreateLogger("main");
			var counter = 0;
			var client = new BackgroundJobClient(storage);

			void Create()
			{
				while (!token.IsCancellationRequested)
				{
					var i = Interlocked.Increment(ref counter);
					try
					{
						client.Schedule(() => HandleJob(i), DateTimeOffset.UtcNow);
						Ticks.OnNext(Unit.Default);
					}
					catch (Exception e)
					{
						logger.LogError(e, "Scheduling failed");
					}
				}
			}

			return Task.WhenAll(
				Task.Run(Create, token),
				Task.Run(Create, token),
				Task.Run(Create, token),
				Task.Run(Create, token));
		}

		private static Task Consumer(
			ILoggerFactory loggerFactory, JobStorage storage, CancellationToken token)
		{
			var server = new BackgroundJobServer(
				new BackgroundJobServerOptions { WorkerCount = 16 },
				storage);

			return Task.Run(
				() => {
					token.WaitHandle.WaitOne();
					server.SendStop();
					server.WaitForShutdown(TimeSpan.FromSeconds(30));
				}, token);
		}

		public static void HandleJob(int i) { Ticks.OnNext(Unit.Default); }
	}
}
