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

    }
}
