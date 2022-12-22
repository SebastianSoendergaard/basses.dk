using API.Quartz;
using Microsoft.EntityFrameworkCore;
using OK.Shared.Metrics.Histogram;
using OK.Shared.Metrics.Histogram.Db;
using Shared.Database;
using Shared.Database.Models;
using System.Linq;
using System.Threading.Tasks;
using static Application.AppMessages.UseCases.PushMessages.PushConstants;

namespace OK.Services.Apps.UserCommunicationService.API.Quartz
{
    internal class TransmissionSchedulerOutboxRepository : ITransmissionSchedulerOutboxRepository
    {
        private readonly AppMessageContext _dbContext;
        private readonly IHistogramBuilder<DbHistogramOptions> _metricsFactory;

        public TransmissionSchedulerOutboxRepository(AppMessageContext dbContext, IDbHistogramProvider dbHistogramProvider)
        {
            _dbContext = dbContext;
            _metricsFactory = dbHistogramProvider.CreateBuilder(new DbHistogramBuilderOptions(GetType().Name));
        }

        public async Task AddMessageToOutbox(ITransmissionSchedulerOutboxRepository.OutboxMessage message)
        {
            using var _ = _metricsFactory.Create().Timer();

            var entity = new TransmissionSchedulerOutboxEntity
            {
                Type = message.Type,
                Data = message.Data
            };

            await _dbContext.TransmissionSchedulerOutboxMessages.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteMessageFromOutbox(long messageId)
        {
            using var _ = _metricsFactory.Create().Timer();

            var entity = await _dbContext.TransmissionSchedulerOutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (entity != null)
            {
                _dbContext.TransmissionSchedulerOutboxMessages.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<ITransmissionSchedulerOutboxRepository.OutboxMessage?> GetOldestMessageFromOutbox()
        {
            using var _ = _metricsFactory.Create().Timer();

            var entity = await _dbContext.TransmissionSchedulerOutboxMessages.OrderBy(m => m.Id).FirstOrDefaultAsync();
            if (entity != null)
            {
                return new ITransmissionSchedulerOutboxRepository.OutboxMessage
                {
                    Id = entity.Id,
                    Type = entity.Type,
                    Data = entity.Data
                };
            }

            return null;
        }
    }
}
