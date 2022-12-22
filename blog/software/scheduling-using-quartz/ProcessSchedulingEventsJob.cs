using API.Quartz;
using Application.AppMessages.UseCases.PushMessages.ReadPushMessages;
using Application.AppMessages.UseCases.PushMessages.SendPushMessagesReadyForTransmission;
using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.ScheduledJobs
{
    [DisallowConcurrentExecution]
    public class ProcessSchedulingEventsJob : IJob
    {
        private readonly ISchedulingEventsHandler _eventHandler;
        private readonly ILogger<ProcessSchedulingEventsJob> _logger;

        public ProcessSchedulingEventsJob(ISchedulingEventsHandler eventHandler, ILogger<ProcessSchedulingEventsJob> logger)
        {
            _eventHandler = eventHandler;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = context.JobDetail.Key.Name;

            _logger.LogDebug("Executing job: {jobId}", jobId);

            try
            {
                await _eventHandler.HandleSchedulingEvents();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in ProcessSchedulingEventsJob: {message}", ex.Message);
            }
        }
    }

    public interface ISchedulingEventsHandler
    {
        Task HandleSchedulingEvents();
    }
}
