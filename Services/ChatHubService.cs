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
        //var chat = await ChatService.Get(chatKey);
        //if (chat is null || !IsParticipant(chat, UserId)) { return; }
        //await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(chatKey));

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

            // notify the recipient's personal group (for other-page toast / browser notification)
            //var recipientId = UserId == chat.CompanyId ? chat.CustomerId : chat.CompanyId;
            //var sender = await userService.Get(UserId);
            //var request = await requestRepository.Get<RequestEntity>(chat.RequestId);
            //var preview = content.Length > 80 ? content[..80] + "…" : content;

            //await Clients.Group("user-" + recipientId).SendAsync("NewMessageNotification", new
            //{
            //    senderName = sender?.Name ?? "Someone",
            //    preview = preview,
            //    requestNumber = request?.Number ?? ""
            //});
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

        public async Task Notify()
        {
            return;

            // TODO: temporarly unavaliable, but we get back to this soon

            //if (UserId == 0) { return; }

            //await Groups.AddToGroupAsync(Context.ConnectionId, "user-" + UserId);

            //// Catchup: fire one notification per chat with unread messages
            //var unread = await chatRepository.LoadUnreadForUser(UserId);
            //foreach (var summary in unread)
            //{
            //    await Clients.Caller.SendAsync("NewMessageNotification", new
            //    {
            //        senderName = summary.SenderName,
            //        preview = summary.Preview,
            //        requestNumber = summary.RequestNumber
            //    });
            //}
        }

        public async Task NotifyProposal(string chatKey, long proposalId, string proposalStatus)
            => await HubContext.Clients
                               .Group(ChatTool.GroupName(chatKey))
                               .SendAsync("ProposalUpdated", new { proposalId, proposalStatus });

        private static bool IsParticipant(ChatEntity chat, long userId)
            => chat.CustomerId == userId || chat.CompanyId == userId;
    }
}
