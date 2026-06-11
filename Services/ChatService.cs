using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;

namespace Bewegdeal.Services
{
    public class ChatService(IChatRepository ChatRepository, UserService UserService)
    {
        public async Task<ChatEntity> Create(ChatEntity chat)
            => await ChatRepository.Create(chat);
        public async Task Update(ChatUpdateAreaEnum area, ChatEntity update)
            => await ChatRepository.Update(area, update);
        public async Task<ChatEntity?> Get(long id, string[]? properties = null)
            => await Get(new ChatFilter { Id = id }, properties);
        public async Task<ChatEntity?> Get(string key, string[]? properties = null)
            => await Get(new ChatFilter { Key = key }, properties);
        public async Task<ChatEntity?> GetOngoing(string? key = null, long? requestId = null)
            => await Get(
                        new ChatFilter { Key = key, RequestId = requestId, Status = ChatStatusEnum.Ongoing },
                        [nameof(ChatEntity.Id), nameof(ChatEntity.Key), nameof(ChatEntity.CompanyId), nameof(ChatEntity.CustomerId)]
                     );
        private async Task<ChatEntity?> Get(ChatFilter filter, string[]? properties = null)
            => await ChatRepository.Get(filter, properties);
        public async Task ReadMessages(long chatId, long viewerId)
            => await ChatRepository.ReadMessages(chatId, viewerId);
        public async Task<ChatMessageEntity> AddMessage(ChatMessageEntity message)
            => await ChatRepository.AddMessage(message);
        public async Task<List<ChatMessageEntity>> LoadMessages(long chatId)
            => await ChatRepository.LoadMessages(chatId);
        public async Task<List<ChatEntity>> Load(ChatFilter filter)
            => await ChatRepository.Load(filter);
        public async Task<int> Count(ChatFilter filter)
            => await ChatRepository.Count(filter);

        public async Task<GridResultModel<object>> LoadGrid(ChatFilter filter, int draw)
        {
            var chats = await Load(filter);
            var filtered = await Count(filter);
            var total = await Count(new ChatFilter());

            var allUserIds = chats.Select(c => c.CustomerId)
                                  .Concat(chats.Select(c => c.CompanyId))
                                  .Concat([0])
                                  .Distinct();
            var users = await UserService.Load(allUserIds, [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.Avatar)]);
            var userMap = users.ToDictionary(u => u.Id);

            return new GridResultModel<object>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = chats.Select(c =>
                {
                    userMap.TryGetValue(c.CustomerId, out var customer);
                    userMap.TryGetValue(c.CompanyId, out var company);
                    return (object)new
                    {
                        id = c.Id,
                        requestId = c.RequestId,
                        status = c.Status,
                        fraud = c.Fraud,
                        customer = UserService.GetAvatar(customer),
                        company = UserService.GetAvatar(company),
                        createDate = c.CreateDate.ToString("MMM d, yyyy"),
                    };
                })
            };
        }
    }
}