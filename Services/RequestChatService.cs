using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.ViewModels;

namespace Bewegdeal.Services
{
    public class RequestChatService(
        RequestService RequestService,
        ChatService ChatService,
        ChatHubService ChatHubService,
        FileService2 FileService)
    {

        public async Task<string> GetMode(string requestNumber, long userId, string userRole)
        {
            var request = await RequestService.Get(requestNumber, [nameof(RequestEntity.Id), nameof(RequestEntity.Status)]);
            var chat = await ChatService.GetOngoing(null, request?.Id ?? 0);

            return
                userRole == UserRoleEnum.Company &&
                request?.Status == RequestStatusEnum.Pending
                    ? ChatModeEnum.Initiate :

                request?.Status == RequestStatusEnum.Negotiation &&
                (chat?.CompanyId == userId || chat?.CustomerId == userId)
                    ? ChatModeEnum.Ongoing :

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
                Fraud = ChatFraudEnum.Safe,
                Status = ChatStatusEnum.Ongoing,
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
            var chat = await ChatService.GetOngoing(null, request?.Id ?? 0);

            if (request is null)
            {
                return null;
            }

            if (chat?.CompanyId != userId && chat?.CustomerId != userId)
            {
                return null;
            }

            var messages = await ChatService.LoadMessages(chat?.Id ?? 0);
            var proposals = await RequestService.LoadProposals(request.Id, chat?.Id ?? 0);

            foreach (var proposal in proposals)
            {
                proposal?.ServiceTerms = FileService.GetUrl(proposal.ServiceTerms);
            }

            return new ChatHistoryModel
            {
                Mode = request.Status == RequestStatusEnum.Pending ? ChatModeEnum.Initiate : ChatModeEnum.Ongoing,
                ChatKey = chat?.Key ?? "",
                ViewerId = userId,
                ViewerInitials = "I J",
                ViewerPictureUrl = null,
                OtherPartyName = "OTHER",
                OtherPartyInitials = "O P",
                OtherPartyPictureUrl = null,
                Messages = messages,
                Proposals = proposals.ToDictionary(p => p.Id)
            };
        }

        public async Task Cancel(string requestNumber, long userId)
        {
            var request = await RequestService.Get(requestNumber, [nameof(RequestEntity.Id)]);
            var chat = await ChatService.GetOngoing(null, request?.Id ?? 0);

            if (request is not null && (chat?.CompanyId == userId || chat?.CustomerId == userId))
            {
                await ChatHubService.Send(userId, chat, "Sorry, I kindly have to end our negotiation, because we couldn't reach an agreement. Wish you a good luck.");
                await ChatService.Update(ChatUpdateAreaEnum.Status, new() { Id = chat.Id });
                await RequestService.Update(RequestUpdateAreaEnum.ChatDeactivate, new() { Id = request.Id });
                await ChatHubService.Leave(chat.Key);
            }
        }

        public async Task Propose(long userId, RequestProposalViewModel model)
        {
            var request = await RequestService.Get(model.RequestNumber ?? "-", [nameof(RequestEntity.Id)]);
            var chat = await ChatService.GetOngoing(null, request?.Id ?? 0);

            model.ChatId = chat?.Id ?? 0;
            model.RequestId = request?.Id ?? 0;
            var proposal = await RequestService.CreateProposal(userId, model);

            if (proposal is not null && chat?.CompanyId == userId)
            {
                await ChatHubService.Send(userId, chat, "Kindly, consider my proposal");
                await ChatHubService.Send(userId, chat, "#bewegdeal-proposal-" + proposal.Id);
            }
        }

        public async Task ProposalReact(long userId, long proposalId, bool accepted, string? reason = null)
        {
            var proposal = await RequestService.GetProposal(proposalId);
            var chat = await ChatService.GetOngoing(null, proposal?.RequestId ?? 0);

            if (chat is not null && proposal?.Status == RequestProposalStatusEnum.Pending)
            {
                reason = accepted ? null : reason;
                var status = accepted ? RequestProposalStatusEnum.Accepted : RequestProposalStatusEnum.Rejected;

                await RequestService.UpdateProposal(proposalId, status, reason);
                await ChatHubService.NotifyProposal(chat.Key, proposalId, status);

                if (accepted)
                {
                    await ChatHubService.Send(userId, chat, "Deal, i accept!");
                }
                else
                {
                    await ChatHubService.Send(userId, chat, "Sorry, i have to reject");
                    await ChatHubService.Send(userId, chat, reason ?? "");
                }
            }
        }

        public async Task<RequestProposalEntity?> GetProposal(long proposalId)
        {
            var proposal = await RequestService.GetProposal(
                proposalId,
                [
                    nameof(RequestProposalEntity.Id),
                    nameof(RequestProposalEntity.Cost),
                    nameof(RequestProposalEntity.Currency),
                    nameof(RequestProposalEntity.Date),
                    nameof(RequestProposalEntity.Time),
                    nameof(RequestProposalEntity.Status),
                    nameof(RequestProposalEntity.ServiceTerms),
                ]
            );

            proposal?.ServiceTerms = FileService.GetUrl(proposal.ServiceTerms);
            return proposal;
        }

    }
}
