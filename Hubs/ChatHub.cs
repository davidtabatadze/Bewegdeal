using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Bewegdeal.Hubs
{
    public class ChatHub(ChatHubService ChatHubService) : Hub
    {

        public async Task JoinChat(string chatKey)
            => await ChatHubService.Join(UserId, chatKey, Context.ConnectionId);

        public async Task SendMessage(string chatKey, string content)
            => await ChatHubService.Send(UserId, chatKey, content);

        public async Task MarkRead(string chatKey)
            => await ChatHubService.MarkRead(UserId, chatKey, Context.ConnectionId);

        public async Task JoinNotifications()
            => await ChatHubService.Notify();

        public static string GroupName(string chatKey) => "bewegdeal-chat-" + chatKey;
        private long UserId => long.TryParse(Context.User?.FindFirstValue(IdentityFieldEnum.Id), out var id) ? id : 0;

    }
}
