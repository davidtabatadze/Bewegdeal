using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Data.Repositories
{
    public class RequestProposalRepository(SqlContext SqlContext) : BaseRepository(SqlContext), IRequestProposalRepository
    {

        public async Task Update(RequestProposalUpdateAreaEnum area, RequestProposalEntity update)
        {
            switch (area)
            {

                case RequestProposalUpdateAreaEnum.Status:
                    await Context.RequestProposals.Where(p => p.Id == update.Id)
                                                  .ExecuteUpdateAsync(p => p
                                                       .SetProperty(p => p.Status, update.Status)
                                                       .SetProperty(p => p.ReactionDate, DateTime.Now)
                                                       .SetProperty(p => p.ReactionReason, update.ReactionReason)
                                                  );
                    break;

                default:
                    throw new ArgumentException("Invalid update area", nameof(area));
            }
        }

        public async Task<List<RequestProposalEntity>> Load(List<long>? requestIds, long? chatId, string? status)
        {
            var query = Context.RequestProposals.AsQueryable();

            if (requestIds is not null)
            {
                requestIds.Add(0);
                query = query.Where(i => requestIds.Contains(i.RequestId));
            }

            if (chatId is not null)
            {
                query = query.Where(i => i.ChatId == chatId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status == status);
            }

            return await query.ToListAsync();
        }

    }
}
