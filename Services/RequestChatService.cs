using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Bewegdeal.Hubs;
using Bewegdeal.Models;

namespace Bewegdeal.Services
{
    public class RequestChatService(RequestService RequestService, ChatService ChatService, ChatHub ChatHub)
    {

        public async Task<string> GetMode(string requestNumber, long userId, string userRole)
        {
            var request = await RequestService.Get(requestNumber, [nameof(RequestEntity.Id), nameof(RequestEntity.Status)]);
            var chat = await ChatService.GetActive(null, request?.Id ?? 0);

            return
                userRole == UserRoleEnum.Company &&
                request?.Status == RequestStatusEnum.Pending
                    ? ChatModeEnum.Initiate :

                request?.Status == RequestStatusEnum.Negotiation &&
                (chat?.CompanyId == userId || chat?.CustomerId == userId)
                    ? ChatModeEnum.Active :

                    ChatModeEnum.None;
        }

        public async Task<GenericResultModel<object>> Initiate(string requestNumber, long userId)
        {
            var request = await RequestService.Get(requestNumber, [nameof(RequestEntity.Id), nameof(RequestEntity.Status), nameof(RequestEntity.RequesterId)]);
            if (request is null || request.Status != RequestStatusEnum.Pending)
            {
                return GenericResultModel<object>.Fail("This request is no longer available.");
            }

            var chat = await ChatService.Create(new ChatEntity
            {
                Key = Guid.NewGuid().ToString("N"),
                RequestId = request.Id,
                CustomerId = request.RequesterId,
                CompanyId = userId,
                Status = ChatStatusEnum.Active,
                CreateDate = DateTime.UtcNow
            });

            await RequestService.Update(RequestUpdateAreaEnum.ChatActivate, new() { Id = request.Id, ExecutorId = userId });

            //var customer = await userService.Get(request.RequesterId);
            //var customerAvatar = await userService.GetAvatar(customer);

            return GenericResultModel<object>.Ok(new
            {
                ChatKey = chat.Key,
                OtherPartyName = "aaaa", //customerAvatar.Name,
                OtherPartyInitials = "a b", //customerAvatar.Initials,
                OtherPartyPictureUrl = (string?)null //customerAvatar.Url
            });
        }

        public async Task<ChatHistoryModel?> Conversation(string requestNumber, long userId)
        {
            var request = await RequestService.Get(requestNumber, [nameof(RequestEntity.Id), nameof(RequestEntity.Status)]);
            var chat = await ChatService.GetActive(null, request?.Id ?? 0);

            if (request is null)
            {
                return null;
            }

            if (chat is not null && chat?.CompanyId != userId && chat?.CustomerId != userId)
            {
                return null;
            }

            var messages = await ChatService.LoadMessages(chat?.Id ?? 0);

            return new ChatHistoryModel
            {
                Mode = request.Status == RequestStatusEnum.Pending ? ChatModeEnum.Initiate : ChatModeEnum.Active,
                ChatKey = chat?.Key ?? "",
                ViewerId = userId,
                ViewerInitials = "I J",
                ViewerPictureUrl = null,
                OtherPartyName = "OTHER",
                OtherPartyInitials = "O P",
                OtherPartyPictureUrl = null,
                Messages = messages
            };
        }

        public async Task Cancel(string requestNumber, long userId)
        {
            var request = await RequestService.Get(requestNumber, []);
            var chat = await ChatService.GetActive(null, request?.Id ?? 0);

            if (request is not null && (chat?.CompanyId == userId || chat?.CustomerId == userId))
            {
                await ChatHub.SendMessage(chat.Key, "Sorry, I kindly have to end our negotiation, because we couldn't reach an agreement. Wish you a good luck.", userId);
                await ChatService.Update(ChatUpdateAreaEnum.Status, new() { Id = chat.Id });
                await RequestService.Update(RequestUpdateAreaEnum.ChatDeactivate, new() { Id = request.Id });
                await ChatHub.LeaveChat(chat.Key);
            }
        }

    }
}
