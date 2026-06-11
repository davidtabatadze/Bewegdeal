using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IChatRepository : IRepository
    {
        Task Update(ChatUpdateAreaEnum area, ChatEntity update);
        Task<ChatEntity?> Get(ChatFilter filter, string[]? properties = null);
        Task<List<ChatEntity>> Load(ChatFilter filter);
        Task<int> Count(ChatFilter filter);
        Task ReadMessages(long chatId, long viewerId);
        Task<ChatMessageEntity> AddMessage(ChatMessageEntity message);
        Task<List<ChatMessageEntity>> LoadMessages(long chatId);
    }
}
