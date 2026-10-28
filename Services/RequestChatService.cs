using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.ViewModels;

namespace Bewegdeal.Services
{
    public class RequestChatService(
        RequestService RequestService,
        ProposalService ProposalService,
        UserService UserService,
        ChatService ChatService,
        ChatHubService ChatHubService,
        FileService FileService)
    {

        public async Task<(string mode, RequestEntity? request, ChatEntity? chat)> GetMode(string requestNumber, long userId, string userRole)
        {
            var request = await RequestService.Get(
                requestNumber,
                [nameof(RequestEntity.Id), nameof(RequestEntity.Status), nameof(RequestEntity.CustomerId)]
            );
            var requestChat = await ChatService.GetActual(requestNumber, userId, userRole);
            var companyChat = await ChatService.Get(new ChatFilter
            {
                CompanyId = userId,
                Status = ChatStatusEnum.Ongoing
            });
            var mode = ChatModeEnum.None;

            if (requestChat is not null)
            {
                mode = ChatModeEnum.Ongoing;
            }
            else if (userRole == UserRoleEnum.Company)
            {
                if (companyChat is not null)
                {
                    mode = ChatModeEnum.Queued;
                }
                else if (request?.Status == RequestStatusEnum.Negotiation)
                {
                    mode = ChatModeEnum.Reserved;
                }
                else if (request?.Status == RequestStatusEnum.Pending)
                {
                    mode = ChatModeEnum.Initiate;
                }
            }

            return (mode, request, requestChat);
        }

        public async Task<GenericResultModel> Initiate(string requestNumber, long userId, string userRole)
        {
            var data = await GetMode(requestNumber, userId, userRole);
            var chat = await ChatService.GetActual(requestNumber);

            if (data.request is null || chat is not null || data.mode != ChatModeEnum.Initiate)
            {
                return GenericResultModel.Fail();
            }

            await ChatService.Create(new ChatEntity
            {
                Key = Guid.NewGuid().ToString("N"),
                RequestId = data.request.Id,
                RequestNumber = requestNumber,
                CustomerId = data.request.CustomerId,
                CompanyId = userId,
                Fraud = ChatFraudEnum.Safe,
                Status = ChatStatusEnum.Ongoing,
                CreateDate = DateTime.Now
            });

            await RequestService.Update(RequestUpdateAreaEnum.ChatActivate, new() { Id = data.request.Id, CompanyId = userId });

            return GenericResultModel.Ok();
        }

        public async Task<ChatHistoryModel?> Conversation(string requestNumber, long userId, string userRole)
        {
            var data = await GetMode(requestNumber, userId, userRole);

            if (data.mode != ChatModeEnum.Ongoing)
            {
                return new ChatHistoryModel
                {
                    Mode = data.mode
                };
            }

            if (
                data.request?.Id != data.chat?.RequestId ||
                (data.chat?.CompanyId != userId && data.chat?.CustomerId != userId)
            )
            {
                return new ChatHistoryModel
                {
                    Mode = ChatModeEnum.None
                };
            }

            var messages = await ChatService.LoadMessages(data.chat?.Id ?? 0);
            var proposals = await ProposalService.Load(data.chat?.Id ?? 0);
            var contacts = await UserService.LoadContacts(proposals.Select(p => p.CompanyContactId));
            var users = await UserService.Load(
                [data.chat?.CustomerId ?? 0, data.chat?.CompanyId ?? 0],
                [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.Avatar),
                    nameof(UserEntity.Rating), nameof(UserEntity.Mobile), nameof(UserEntity.Email)]
            );

            var viewerAvatar = UserService.GetAvatar(users.FirstOrDefault(u => u.Id == userId));
            var otherPartyAvatar = UserService.GetAvatar(users.FirstOrDefault(u => u.Id != userId));

            var proposalCards = new Dictionary<long, ProposalCardModel> { };
            foreach (var proposal in proposals)
            {
                var contact = contacts.FirstOrDefault(c => c.Id == proposal.CompanyContactId);
                if (contact?.ServiceTerms != null)
                {
                    contact.ServiceTerms = FileService.GetUrl(contact.ServiceTerms);
                }

                proposalCards.Add(proposal.Id,
                    new ProposalCardModel
                    {
                        Proposal = proposal,
                        CompanyContact = contact,
                        Company = users.FirstOrDefault(u => u.Id == proposal.CompanyId)
                    });
            }

            return new ChatHistoryModel
            {
                Mode = ChatModeEnum.Ongoing,
                ChatKey = data.chat?.Key ?? "",
                ChatStatus = data.chat?.Status ?? "",
                RequestStatus = data.request?.Status ?? "",
                ViewerId = userId,
                ViewerInitials = viewerAvatar.Initials,
                ViewerPictureUrl = viewerAvatar.Url,
                OtherPartyName = otherPartyAvatar.Name,
                OtherPartyInitials = otherPartyAvatar.Initials,
                OtherPartyPictureUrl = otherPartyAvatar.Url,
                OtherPartyRating = otherPartyAvatar.Rating,
                Messages = messages,
                Proposals = proposalCards,
                ProposalPending = proposals.Any(p => p.Status == RequestProposalStatusEnum.Pending),
                AllowAgreementCancel = data.chat?.Status == ChatStatusEnum.Agreed &&
                                       proposals.Any(p =>
                                            data.request?.Status == RequestStatusEnum.Agreed &&
                                            p.Status == RequestProposalStatusEnum.Accepted &&
                                            p.Date is not null && p.Time is not null &&
                                            p.Date.Value.ToDateTime(p.Time.Value) >= DateTime.Now
                                       )
            };
        }

        public async Task Cancel(string requestNumber, long userId)
        {
            var request = await RequestService.Get(
                requestNumber,
                [nameof(RequestEntity.Id), nameof(RequestEntity.Status)]
            );
            var chat = await ChatService.GetActual(requestNumber);
            var proposal = await ProposalService.GetActual(chat?.Id ?? 0);

            if (request?.Status == RequestStatusEnum.Negotiation && chat?.Status == ChatStatusEnum.Ongoing && (chat?.CompanyId == userId || chat?.CustomerId == userId))
            {
                await ProposalReact(userId, proposal?.Id ?? 0, false, null);
                await ChatHubService.Send(userId, chat, "Es tut mir leid, ich muss unsere Verhandlung beenden, da wir keine Einigung erzielen konnten. Ich wünsche Ihnen viel Erfolg.", true);
                await ChatService.Update(ChatUpdateAreaEnum.Status, new() { Id = chat.Id, Status = ChatStatusEnum.Cancelled });
                await RequestService.Update(RequestUpdateAreaEnum.ChatDeactivate, new() { Id = request.Id });
                await ChatHubService.Leave(chat.Key);
            }
        }

        public async Task CancelAgreement(string requestNumber, long userId)
        {
            var request = await RequestService.Get(
                requestNumber,
                [nameof(RequestEntity.Id), nameof(RequestEntity.Status)]
            );
            var chat = await ChatService.Get(new ChatFilter
            {
                RequestId = request?.Id ?? 0,
                Status = ChatStatusEnum.Agreed
            }, [nameof(ChatEntity.Id), nameof(ChatEntity.Key), nameof(ChatEntity.Status), nameof(ChatEntity.CompanyId), nameof(ChatEntity.CustomerId)]);
            var proposals = await ProposalService.Load(new RequestProposalFilter
            {
                ChatId = chat?.Id ?? 0,
                Status = RequestProposalStatusEnum.Accepted
            });

            var allowCancel = proposals.All(p =>
                p.Date is not null && p.Time is not null &&
                p.Date.Value.ToDateTime(p.Time.Value) >= DateTime.Now
            );

            if (allowCancel && request?.Status == RequestStatusEnum.Agreed && chat?.Status == ChatStatusEnum.Agreed && (chat?.CompanyId == userId || chat?.CustomerId == userId))
            {
                foreach (var proposal in proposals)
                {
                    await ProposalService.Update(proposal.Id, userId, RequestProposalStatusEnum.Declined);
                    await ChatHubService.NotifyProposal(chat.Key, proposal.Id, RequestProposalStatusEnum.Declined);
                }
                await ChatHubService.Send(userId, chat,
                    userId == chat?.CompanyId ?
                    "Leider müssen wir die Durchführung des gebuchten Auftrags absagen. Wir entschuldigen uns für die Unannehmlichkeiten."
                    : "Leider muss ich den gebuchten Auftrag stornieren. Ich bitte um Verständnis.",
                    true
                );
                //await ChatHubService.Send(userId, chat,
                //    userId == chat?.CompanyId ?
                //    "Hinweis von Bewegdeal: Entstehen Ihnen durch die Absage Mehrkosten, können Sie unter den gesetzlichen Voraussetzungen Ersatz verlangen. Ihre Rechte gegenüber der Firma bleiben bestehen."
                //    : "Hinweis von Bewegdeal: Ob Kosten anfallen, richtet sich nach den wirksam vereinbarten Stornobedingungen und den gesetzlichen Vorschriften. Ein bestehendes gesetzliches Rücktrittsrecht bleibt unberührt.",
                //    true
                //);
                await ChatService.Update(ChatUpdateAreaEnum.Status, new() { Id = chat!.Id, Status = ChatStatusEnum.Cancelled });
                await RequestService.Update(RequestUpdateAreaEnum.ChatDeactivate, new() { Id = request.Id });
            }
        }

        public async Task ProposalCancel(long userId, string requestNumber)
        {
            var chat = await ChatService.GetActual(requestNumber);
            var proposal = await ProposalService.GetActual(chat?.Id ?? 0);

            if (proposal?.Status == RequestProposalStatusEnum.Pending && proposal?.CompanyId == userId)
            {
                await ProposalService.Update(proposal.Id, userId, RequestProposalStatusEnum.Canceled);
                await ChatHubService.NotifyProposal(chat?.Key ?? "-", proposal.Id, RequestProposalStatusEnum.Canceled);
            }
        }

        public async Task Propose(long userId, RequestProposalViewModel model)
        {
            var request = await RequestService.Get(
                model.RequestNumber ?? "-",
                [nameof(RequestEntity.Id), nameof(RequestEntity.Status), nameof(RequestEntity.Service)]
            );
            var chat = await ChatService.GetActual(model.RequestNumber ?? "-");
            var existing = await ProposalService.GetActual(chat?.Id ?? 0);
            var company = await UserService.Get(userId, [nameof(UserEntity.ContactId)]);

            if (request?.Status == RequestStatusEnum.Negotiation && chat?.Status == ChatStatusEnum.Ongoing && existing is null)
            {
                model.ChatId = chat?.Id ?? 0;
                model.RequestId = request?.Id ?? 0;
                var proposal = await ProposalService.Create(new RequestProposalEntity
                {
                    CompanyId = userId,
                    CompanyContactId = company?.ContactId ?? 0,
                    CustomerId = chat?.CustomerId ?? 0,
                    ChatId = model.ChatId,
                    RequestId = model.RequestId,
                    CreateDate = DateTime.Now,
                    Cost = model.Cost,
                    Currency = model.Currency,
                    Date = DateOnly.Parse(model.Date!),
                    Time = TimeOnly.Parse(model.Time!),
                    Status = RequestProposalStatusEnum.Pending,
                    Service = request?.Service ?? "-",
                    InvoiceId = 0
                });

                // await ChatHubService.Send(userId, chat, "Kindly, consider my proposal.");
                await ChatHubService.Send(userId, chat, "Angebot erhalten.", true);
                await ChatHubService.Send(userId, chat, "#bewegdeal-proposal-" + proposal.Id, false);
            }
        }

        public async Task ProposalReact(long userId, long proposalId, bool accepted, string? reason = null)
        {
            var proposal = await ProposalService.Get(
                proposalId,
                [nameof(RequestProposalEntity.ChatId), nameof(RequestProposalEntity.Status)]
            );
            var chat = await ChatService.Get(proposal?.ChatId ?? 0);

            if (chat?.Status == ChatStatusEnum.Ongoing && proposal?.Status == RequestProposalStatusEnum.Pending)
            {
                reason = accepted ? null : reason;
                var status = accepted ? RequestProposalStatusEnum.Accepted : RequestProposalStatusEnum.Rejected;

                await ProposalService.Update(proposalId, userId, status, reason);
                if (accepted)
                {
                    await ChatService.Update(
                        ChatUpdateAreaEnum.Status,
                        new() { Id = chat?.Id ?? 0, Status = ChatStatusEnum.Agreed }
                    );
                    await RequestService.Update(
                        RequestUpdateAreaEnum.ChatDeal,
                        new() { Id = chat?.RequestId ?? 0, CompanyId = chat?.CompanyId ?? 0, AgreementId = proposalId }
                    );
                }
                await ChatHubService.NotifyProposal(chat?.Key ?? "-", proposalId, status);

                if (accepted)
                {
                    // await ChatHubService.Send(userId, chat, "Deal, i accept!");
                    await ChatHubService.Send(userId, chat, "Einverstanden, ich nehme an!", true);
                }
                else
                {
                    // await ChatHubService.Send(userId, chat, "Sorry, i have to reject");
                    await ChatHubService.Send(userId, chat, "Es tut mir leid, ich muss ablehnen.", true);
                    await ChatHubService.Send(userId, chat, reason ?? "", false);
                }
            }
        }

        public async Task<ProposalCardModel?> GetProposal(long proposalId)
        {
            var proposal = await ProposalService.Get(
                proposalId,
                [
                    nameof(RequestProposalEntity.Id),
                    nameof(RequestProposalEntity.Cost),
                    nameof(RequestProposalEntity.Currency),
                    nameof(RequestProposalEntity.Date),
                    nameof(RequestProposalEntity.Time),
                    nameof(RequestProposalEntity.Status),
                    nameof(RequestProposalEntity.CompanyId),
                    nameof(RequestProposalEntity.CompanyContactId)
                ]
            );

            var company = await UserService.Get(
                proposal?.CompanyId ?? 0,
                [nameof(UserEntity.Id), nameof(UserEntity.Mobile), nameof(UserEntity.Email)]
            );

            var contact = await UserService.GetContact(proposal?.CompanyContactId ?? 0);
            if (contact?.ServiceTerms != null)
            {
                contact.ServiceTerms = FileService.GetUrl(contact.ServiceTerms);
            }

            return new ProposalCardModel
            {
                Proposal = proposal,
                Company = company,
                CompanyContact = contact,
            };
        }

    }
}
