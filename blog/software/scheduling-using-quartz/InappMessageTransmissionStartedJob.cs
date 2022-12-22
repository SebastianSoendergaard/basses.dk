using API.Quartz;
using Application.AppMessages.UseCases.PushMessages.ReadPushMessages;
using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.ScheduledJobs
{
    [DisallowConcurrentExecution]
    public class InappMessageTransmissionStartedJob : IJob
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InappMessageTransmissionStartedJob> _logger;

        public InappMessageTransmissionStartedJob(IMediator mediator, ILogger<InappMessageTransmissionStartedJob> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = context.JobDetail.Key.Name;
            var data = context.GetData<InappMessageTransmissionStartedJobData>();

            if (data == null)
            {
                _logger.LogCritical("Could not deserialize data for job: {jobId}", jobId);
                return;
            }

            var loggingContext = new LoggingContext
            {
                CorrelationId = data.CorrelationId,
                Caller = "OK.Services.Apps.UserCommunicationService"
            };
            using var logScope = _logger.BeginScope(loggingContext.GetScope());

            _logger.LogDebug("Executing job: {jobId}, data: {jobData}", jobId, JsonSerializer.Serialize(data));

            try
            {
                await _mediator.Send(new CreatePushMessageFromActiveInappMessageWithPushNotification.Command(data.TransmissionId, data.TransmissionStartTime));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in InappMessageTransmissionStartedJob: {message}", ex.Message);
            }
        }
    }

    public class InappMessageTransmissionStartedJobData
    {
        public string CorrelationId { get; set; } = "";
        public long TransmissionId { get; set; }
        public DateTimeOffset TransmissionStartTime { get; set; }
    }
}
