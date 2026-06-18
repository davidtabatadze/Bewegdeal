using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Bewegdeal.Tools;
using Microsoft.AspNetCore.SignalR;

namespace Bewegdeal.Services
{
    public class ChatHubService(ChatService ChatService, FraudWordService FraudWordService, IHubContext<ChatTool> HubContext)
    {

        public async Task Join(long userId, string chatKey, string connectionId)
        {
            var chat = await ChatService.Get(chatKey);
            if (chat is null || !IsParticipant(chat, userId))
            {
                return;
            }

            await HubContext.Groups.AddToGroupAsync(connectionId, ChatTool.GroupName(chatKey));
            await MarkRead(userId, chatKey, connectionId, chat);
        }

        public async Task Leave(string chatKey)
            => await HubContext.Clients.Group(ChatTool.GroupName(chatKey)).SendAsync("ChatCancelled");

        public async Task Send(long userId, string chatKey, string content)
            => await Send(userId, await ChatService.Get(chatKey), content);

        public async Task Send(long userId, ChatEntity? chat, string content)
        {
            content = (content ?? "").Trim();
            if (string.IsNullOrWhiteSpace(content) || content.Length > 1024)
            {
                return;
            }

            if (chat is null || !IsParticipant(chat, userId) || chat.Status == ChatStatusEnum.Cancelled)
            {
                return;
            }

            var isFraud = chat.Status == ChatStatusEnum.Ongoing && await FraudWordService.IsFraud(content);

            var message = await ChatService.AddMessage(new ChatMessageEntity
            {
                ChatId = chat.Id,
                SenderId = userId,
                Content = content,
                SentDate = DateTime.UtcNow,
                IsRead = false,
                IsFraud = isFraud
            });

            await HubContext.Clients.Group(ChatTool.GroupName(chat.Key)).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = message.SenderId,
                content = message.Content,
                sentDate = message.SentDate.ToString("HH:mm"),
                sentDay = message.SentDate.ToString("yyyy-MM-dd")
            });

            if (isFraud && chat.Fraud == ChatFraudEnum.Safe)
            {
                await ChatService.Update(ChatUpdateAreaEnum.Fraud, new() { Id = chat.Id, Fraud = ChatFraudEnum.Dubious });
            }

            if (!content.StartsWith(ConstantEnum.ProposalPrefix))
            {
                await Notify(userId == chat.CompanyId ? chat.CustomerId : chat.CompanyId);
            }
        }

        public async Task MarkRead(long userId, string chatKey, string connectionId, ChatEntity? chat = null)
        {
            chat ??= await ChatService.Get(chatKey);
            if (chat is null || !IsParticipant(chat, userId))
            {
                return;
            }

            await ChatService.ReadMessages(chat.Id, userId);
            await HubContext.Clients
                            .GroupExcept(ChatTool.GroupName(chatKey), connectionId)
                            .SendAsync("MessagesRead");
        }

        public async Task Notify(long userId, string connectionId)
        {
            await HubContext.Groups.AddToGroupAsync(connectionId, "user-" + userId);
            await Notify(userId);
        }

        public async Task Notify(long userId)
        {
            var notification = await ChatService.GetMessageUnread(userId);
            if (notification is not null)
            {
                await HubContext.Clients.Group("user-" + userId).SendAsync("NewMessageNotification", notification);
            }
        }

        public async Task NotifyProposal(string chatKey, long proposalId, string proposalStatus)
            => await HubContext.Clients
                               .Group(ChatTool.GroupName(chatKey))
                               .SendAsync("ProposalUpdated", new { proposalId, proposalStatus });

        private static bool IsParticipant(ChatEntity chat, long userId)
            => chat.CustomerId == userId || chat.CompanyId == userId;
    }
}
