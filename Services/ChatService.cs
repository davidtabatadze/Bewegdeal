using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;

namespace Bewegdeal.Services
{
    public class ChatService(IChatRepository ChatRepository, UserService UserService, RequestService RequestService)
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
            => await Get(new ChatFilter { Key = key, RequestId = requestId, Status = ChatStatusEnum.Ongoing });
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

            var users = await UserService.Load(
                chats.Select(c => c.CustomerId)
                     .Concat(chats.Select(c => c.CompanyId))
                     .Concat([0])
                     .Distinct(),
                [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.Email), nameof(UserEntity.Avatar)]
            );
            var userMap = users.ToDictionary(u => u.Id);

            var requests = await RequestService.Load(
                chats.Select(c => c.RequestId)
                     .Concat([0])
                     .Distinct(),
                [nameof(RequestEntity.Id), nameof(RequestEntity.Number)]
            );
            var requestMap = requests.ToDictionary(r => r.Id);

            return new GridResultModel<object>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = chats.Select(c =>
                {
                    userMap.TryGetValue(c.CustomerId, out var customer);
                    userMap.TryGetValue(c.CompanyId, out var company);
                    requestMap.TryGetValue(c.RequestId, out var request);
                    return (object)new
                    {
                        id = c.Id,
                        key = c.Key,
                        requestId = c.RequestId,
                        requestNumber = request?.Number ?? "-",
                        status = c.Status,
                        fraud = c.Fraud,
                        customer = UserService.GetAvatar(customer),
                        customerEmail = customer?.Email ?? "-",
                        company = UserService.GetAvatar(company),
                        companyEmail = company?.Email ?? "-",
                        createDate = c.CreateDate.ToString("MMM d, yyyy"),
                    };
                })
            };
        }

        public async Task<ChatHistoryModel?> GetConversation(string key)
        {
            var chat = await Get(key, [
                nameof(ChatEntity.Id), nameof(ChatEntity.Key),
                nameof(ChatEntity.CustomerId), nameof(ChatEntity.CompanyId)
            ]);
            if (chat is null) { return null; }

            var messages = await LoadMessages(chat.Id);
            var proposals = await RequestService.LoadProposals(null, chat.Id);
            var users = await UserService.Load(
                [chat.CustomerId, chat.CompanyId],
                [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.Avatar)]
            );
            var customer = users.FirstOrDefault(u => u.Id == chat.CustomerId);
            var company = users.FirstOrDefault(u => u.Id == chat.CompanyId);
            var customerAvatar = UserService.GetAvatar(customer);
            var companyAvatar = UserService.GetAvatar(company);

            return new ChatHistoryModel
            {
                Mode = ChatModeEnum.Ongoing,
                ChatKey = chat.Key,
                ViewerId = chat.CustomerId,
                ViewerInitials = customerAvatar.Initials,
                ViewerPictureUrl = customerAvatar.Url,
                OtherPartyName = companyAvatar.Name,
                OtherPartyInitials = companyAvatar.Initials,
                OtherPartyPictureUrl = companyAvatar.Url,
                Messages = messages,
                Proposals = proposals.ToDictionary(p => p.Id)
            };
        }
    }
}