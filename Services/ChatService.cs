using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;

namespace Bewegdeal.Services
{
    public class ChatService(IChatRepository ChatRepository)
    {
        public async Task<ChatEntity> Create(ChatEntity chat)
            => await ChatRepository.Create(chat);
        public async Task Update(ChatUpdateAreaEnum area, ChatEntity update)
            => await ChatRepository.Update(area, update);
        public async Task<ChatEntity?> Get(string key, string[]? properties = null)
            => await Get(new ChatFilter { Key = key }, properties);
        public async Task<ChatEntity?> GetActive(string? key = null, long? requestId = null)
            => await Get(
                        new ChatFilter { Key = key, RequestId = requestId, Status = ChatStatusEnum.Active },
                        [nameof(ChatEntity.Id), nameof(ChatEntity.Key), nameof(ChatEntity.CompanyId), nameof(ChatEntity.CustomerId)]
                     );
        public async Task ReadMessages(long chatId, long viewerId)
            => await ChatRepository.ReadMessages(chatId, viewerId);
        public async Task<ChatMessageEntity> AddMessage(ChatMessageEntity message)
            => await ChatRepository.AddMessage(message);
        public async Task<List<ChatMessageEntity>> LoadMessages(long chatId)
            => await ChatRepository.LoadMessages(chatId);
        private async Task<ChatEntity?> Get(ChatFilter filter, string[]? properties = null)
            => await ChatRepository.Get(filter, properties);
    }
}