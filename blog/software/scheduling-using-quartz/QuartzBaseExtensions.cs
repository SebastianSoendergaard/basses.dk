using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuartzUtil
{
    public static class QuartzBaseExtensions
    {
        public static void AddRecurringJob<TJob>(this IServiceCollectionQuartzConfigurator configurator, string jobId, string cronSchedule) where TJob : IJob
        {
            var jobKey = new JobKey(jobId);
            configurator.AddJob<TJob>(o => o.WithIdentity(jobKey));
            configurator.AddTrigger(o => o
                .ForJob(jobKey)
                .WithIdentity(jobKey.Name + "-trigger")
                .WithCronSchedule(cronSchedule));
        }

        public static async Task AddOneTimeJob<TJob, TData>(this IScheduler scheduler, string jobId, DateTimeOffset time, TData data) where TJob : IJob where TData : class
        {
            var jobKey = new JobKey(jobId);

            var job = JobBuilder
                .Create<TJob>()
                .WithIdentity(jobKey)
                .WithData(data)
                .Build();

            var trigger = TriggerBuilder
                .Create()
                .WithIdentity($"{jobKey.Name}-trigger")
                .StartAt(time)
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        public static async Task CancelJob(this IScheduler scheduler, string jobId)
        {
            var jobKey = new JobKey(jobId);

            await scheduler.DeleteJob(jobKey);
        }

        public static JobBuilder WithData<TData>(this JobBuilder builder, TData data) where TData : class
        {
            return builder.UsingJobData("serialized-json-data", JsonSerializer.Serialize(data));
        }

        public static TData? GetData<TData>(this IJobExecutionContext context) where TData : class
        {
            var dataStr = context.JobDetail.JobDataMap.GetString("serialized-json-data");
            return string.IsNullOrEmpty(dataStr) ? null : JsonSerializer.Deserialize<TData?>(dataStr);
        }

        public static async Task DeleteAllJobs(this IScheduler scheduler)
        {
            var jobGroups = await scheduler.GetJobGroupNames();
            var jobKeys = new List<JobKey>();

            foreach (var group in jobGroups)
            {
                var matcher = GroupMatcher<JobKey>.GroupContains(group);
                jobKeys.AddRange(await scheduler.GetJobKeys(matcher));
            }

            if (!jobKeys.Any())
            {
                return;
            }

            await scheduler.DeleteJobs(jobKeys);
        }
    }
}
