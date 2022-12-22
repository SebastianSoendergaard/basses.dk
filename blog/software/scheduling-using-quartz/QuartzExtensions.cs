using API.Options;
using API.ScheduledJobs;
using Application.AppMessages.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OK.Services.Apps.UserCommunicationService.API.Quartz;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuartzUtil
{
    internal static class QuartzExtensions
    {
        internal static void AddQuartz(this IServiceCollection services, IConfiguration configuration, Action<IServiceCollectionQuartzConfigurator> quarztConfigurator, bool startScheduler)
        {
            var options = configuration.Get<RootOptions>().QuartzJobScheduler;
            var cronSchedules = options.RecurringJobs.ToDictionary(j => j.Type, j => j.CronSchedule);

            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();

                // Setup recurring job
                q.AddRecurringJob<TransmitPendingPushMessagesJob>(cronSchedules);
                q.AddRecurringJob<ArchiveInappTransmissionRecipientsJob>(cronSchedules);

                quarztConfigurator(q);
            });

            services.AddScoped<ITransmissionScheduler, TransmissionScheduler>();
            services.AddScoped<ISchedulingEventsHandler, TransmissionScheduler>();
            services.AddScoped<ITransmissionSchedulerOutboxRepository, TransmissionSchedulerOutboxRepository>();

            if (startScheduler)
            {
                services.AddQuartzHostedService(o =>
                {
                    o.WaitForJobsToComplete = true;
                });
            }
        }

        internal static void AddRecurringJob<TJob>(this IServiceCollectionQuartzConfigurator configurator, IDictionary<string, string> cronSchedules) where TJob : IJob
        {
            var type = typeof(TJob).Name;
            configurator.AddRecurringJob<TJob>(type, cronSchedules[type]);
        }
    }
}
