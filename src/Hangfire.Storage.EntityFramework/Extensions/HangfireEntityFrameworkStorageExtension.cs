using Hangfire.Server;
using Hangfire.Storage.EntityFramework.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.EntityFramework.Extensions
{
    public static class HangfireEntityFrameworkStorageExtension
    {
        /// <summary>
        /// Adds services to the services container to support EntityFramework storage layer for Hangfire.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHangfireEntityFramework(this IServiceCollection services)
        {
            services.AddSingleton<IServerComponent, ExpirationManager>();
            services.AddSingleton<IServerComponent, CountersAggregator>();
            services.AddSingleton<IStorageConnection, EntityFrameworkStorageConnection>();
            services.AddSingleton<IMonitoringApi, EntityFrameworkMonitoringApi>();

            




            return services;
        }
    }
}
