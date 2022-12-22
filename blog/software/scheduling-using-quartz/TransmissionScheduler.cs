using API.ScheduledJobs;
using Application.AppMessages.Domain.Interfaces;
using Google.Api.Gax;
using Microsoft.AspNetCore.HttpLogging;
using OK.Shared.AspNetCore.Logging.Logging.ContextProvider;
using Quartz;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using static API.Quartz.ITransmissionSchedulerOutboxRepository;

namespace API.Quartz
{
    internal class TransmissionScheduler : ITransmissionScheduler, ISchedulingEventsHandler
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ITransmissionSchedulerOutboxRepository _outboxRepository;
        private readonly LoggingContext _loggingContext;

        public TransmissionScheduler(ISchedulerFactory schedulerFactory, ITransmissionSchedulerOutboxRepository outboxRepository, ILoggingContextProvider<LoggingContext> loggingContextProvider)
        {
            _schedulerFactory = schedulerFactory;
            _outboxRepository = outboxRepository;
            _loggingContext = loggingContextProvider.Get() ?? new LoggingContext();
        }

        public async Task ScheduleTransmissionStart(long transmissionId, DateTimeOffset time)
        {
            var message = new TransmissionStartOutboxMessage
            { 
                JobId = $"{nameof(InappMessageTransmissionStartedJob)}-{transmissionId}",
                Time = time,
                JobData = new InappMessageTransmissionStartedJobData
                {
                    CorrelationId = _loggingContext.CorrelationId ?? Guid.NewGuid().ToString(),
                    TransmissionId = transmissionId,
                    TransmissionStartTime = time
                }
            };

            // Use outbox pattern to allow saving event in transaction scope
            await AddMessageToOutbox(message);
        }

        public async Task CancelScheduledTransmissionStart(long transmissionId)
        {
            var message = new TransmissionCancelOutboxMessage
            {
                JobId = $"{nameof(InappMessageTransmissionStartedJob)}-{transmissionId}"
            };

            // Use outbox pattern to allow saving event in transaction scope
            await AddMessageToOutbox(message);
        }

        public async Task HandleSchedulingEvents()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            while (true)
            {
                var outboxMessage = await _outboxRepository.GetOldestMessageFromOutbox();
                if (outboxMessage == null)
                {
                    // No pending messages in outbox
                    break;
                }

                if (outboxMessage.Type == typeof(TransmissionStartOutboxMessage).Name)
                {
                    var message = JsonSerializer.Deserialize<TransmissionStartOutboxMessage>(outboxMessage.Data);
                    if (message == null)
                    {
                        throw new ArgumentException($"Could not deserialize TransmissionStartOutboxMessage: {outboxMessage.Data}");
                    }
                    await scheduler.AddOneTimeJob<InappMessageTransmissionStartedJob, InappMessageTransmissionStartedJobData>(message.JobId, message.Time, message.JobData);
                    await _outboxRepository.DeleteMessageFromOutbox(outboxMessage.Id);
                }
                else if (outboxMessage.Type == typeof(TransmissionCancelOutboxMessage).Name)
                {
                    var message = JsonSerializer.Deserialize<TransmissionCancelOutboxMessage>(outboxMessage.Data);
                    if (message == null)
                    {
                        throw new ArgumentException($"Could not deserialize TransmissionCancelOutboxMessage: {outboxMessage.Data}");
                    }
                    await scheduler.CancelJob(message.JobId);
                    await _outboxRepository.DeleteMessageFromOutbox(outboxMessage.Id);
                }
                else
                {
                    throw new InvalidOperationException($"Unknown outbox message type: {outboxMessage.Type}");
                }
            }
        }

        private async Task AddMessageToOutbox<Tmessage>(Tmessage message) where Tmessage : class
        {
            var outboxMessage = new ITransmissionSchedulerOutboxRepository.OutboxMessage
            {
                Type = typeof(Tmessage).Name,
                Data = JsonSerializer.Serialize(message)
            };

            await _outboxRepository.AddMessageToOutbox(outboxMessage);
        }

        class TransmissionStartOutboxMessage
        {
            public string JobId { get; set; } = "";
            public DateTimeOffset Time { get; set; }
            public InappMessageTransmissionStartedJobData JobData { get; set; } = new InappMessageTransmissionStartedJobData();
        }

        class TransmissionCancelOutboxMessage
        {
            public string JobId { get; set; } = "";
        }
    }

    internal interface ITransmissionSchedulerOutboxRepository
    {
        Task AddMessageToOutbox(OutboxMessage message);
        Task DeleteMessageFromOutbox(long messageId);
        Task<OutboxMessage?> GetOldestMessageFromOutbox();

        public class OutboxMessage
        {
            public long Id { get; set; }
            public string Type { get; set; } = "";
            public string Data { get; set; } = "";
        }
    }
}
