using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class ChatController(ChatService ChatService) : XBaseController
    {
        [Authorize(Roles = UserRoleEnum.Administrator)]
        public IActionResult List()
        {
            return View();
        }

        [Authorize(Roles = UserRoleEnum.Administrator)]
        [HttpGet]
        public async Task<IActionResult> LoadChats([FromQuery] ChatFilter filter, [FromQuery] int draw = 1)
        {
            return Json(await ChatService.LoadGrid(filter, draw));
        }

        [Authorize(Roles = UserRoleEnum.Administrator)]
        [HttpGet]
        public async Task<IActionResult> Conversation(string key)
        {
            var conversation = await ChatService.GetConversation(key);
            if (conversation is null)
            {
                return Content("");
            }
            return PartialView("~/Views/Chat/Conversation.cshtml", conversation);
        }

        [Authorize(Roles = UserRoleEnum.Administrator)]
        [HttpPost]
        public async Task<IActionResult> UpdateChatFraud(long id, string fraud)
        {
            var chat = await ChatService.Get(id, [nameof(ChatEntity.Id), nameof(ChatEntity.Fraud)]);

            if (chat is null || chat.Fraud != fraud)
            {
                return BadRequest();
            }

            await ChatService.Update(ChatUpdateAreaEnum.Fraud, new ChatEntity
            {
                Id = chat.Id,
                Fraud = ChatFraudEnum.Resolved
            });

            return Json(new { fraud = ChatFraudEnum.Resolved });
        }
    }
}
