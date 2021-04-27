using Hangfire.Storage.EntityFramework.Configuration;
using Hangfire.Storage.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.EntityFramework
{
    internal class HangfireDbContext : DbContext
    {
        private readonly IOptions<EntityFrameworkStorageConfiguration> _storageOptions;

        public HangfireDbContext(DbContextOptions<HangfireDbContext> options, IOptions<EntityFrameworkStorageConfiguration> storageOptions)
            : base(options)
        {
            _storageOptions = storageOptions;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Job>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(Job)}s"));
            modelBuilder.Entity<AggregatedCounter>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(AggregatedCounter)}s"));
            modelBuilder.Entity<Counter>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(Counter)}s"));
            modelBuilder.Entity<DistributedLock>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(DistributedLock)}s"));
            modelBuilder.Entity<Hash>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(Hash)}es"));
            modelBuilder.Entity<JobParameter>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(JobParameter)}s"));
            modelBuilder.Entity<JobState>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(JobState)}s"));
            modelBuilder.Entity<Entities.JobQueue>(e =>
            {
                e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(JobQueue)}s");
                e.HasIndex(e => new { e.Name, e.FetchedAt });
            });
            modelBuilder.Entity<Set>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(Entities.Set)}s"));
            modelBuilder.Entity<State>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(State)}s"));
            modelBuilder.Entity<List>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(List)}s"));
            modelBuilder.Entity<Entities.Server>(e => e.ToTable($"{_storageOptions.Value.TablesPrefix}{nameof(Entities.Server)}s"));

        }


        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<AggregatedCounter> AggregatedCounters => Set<AggregatedCounter>();
        public DbSet<Counter> Counters => Set<Counter>();
        public DbSet<DistributedLock> DistributedLocks => Set<DistributedLock>();
        public DbSet<Hash> Hashes => Set<Hash>();
        public DbSet<JobParameter> JobParameters => Set<JobParameter>();
        public DbSet<JobState> JobStates => Set<JobState>();
        public DbSet<Entities.JobQueue> JobQueues => Set<Entities.JobQueue>();
        public DbSet<Set> Sets => Set<Set>();
        public DbSet<State> States => Set<State>();
        public DbSet<List> Lists => Set<List>();
        public DbSet<Entities.Server> Servers => Set<Entities.Server>();

        



    }
}
