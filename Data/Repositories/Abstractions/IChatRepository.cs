using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Models;

namespace Bewegdeal.Data.Repositories.Abstractions
{
    public interface IChatRepository : IRepository
    {
        Task<ChatEntity?> Get(string key);
        Task<ChatEntity?> GetActive(long requestId);
        Task<ChatMessageEntity> CreateMessage(ChatMessageEntity message);
        Task<List<ChatMessageEntity>> LoadMessages(long chatId);
        Task MarkRead(long chatId, long viewerId);
        Task Cancel(long chatId);
        Task<List<ChatUnreadSummary>> LoadUnreadForUser(long userId);
    }
}
