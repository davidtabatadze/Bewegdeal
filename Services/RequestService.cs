using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.ViewModels;

namespace Bewegdeal.Services
{
    public class RequestService(
        IRequestRepository RequestRepository,
        IRequestFileRepository RequestFileRepository,
        ProposalService ProposalService,
        InvoiceService InvoiceService,
        UserService UserService,
        FileService FileService,
        SettingService SettingService)
    {

        public async Task<GenericResultModel<RequestEntity>> Create(long userId, RequestViewModel model)
        {
            // do create
            var request = await RequestRepository.Create(
                BuildRequest(null, model, userId)
            );

            // upload media
            model.Id = request.Id;
            var upload = await UploadMedia(model, []);
            if (!upload.Success)
            {
                return GenericResultModel<RequestEntity>.Fail(upload.Message);
            }

            return GenericResultModel<RequestEntity>.Ok(request);
        }

        public async Task<GenericResultModel<RequestEntity>> Update(long userId, RequestViewModel model)
        {
            // existings
            var request = await RequestRepository.Get<RequestEntity>(model.Id);
            var requestFiles = await RequestFileRepository.Load(model.Id);

            // ...
            if (request is null || request.RequesterId != userId || request.Status != RequestStatusEnum.Pending)
            {
                return GenericResultModel<RequestEntity>.Fail("The request cannot be updated, try again later.");
            }

            // do update
            await Update(RequestUpdateAreaEnum.Full, BuildRequest(request, model, userId));

            // upload media
            var upload = await UploadMedia(model, requestFiles);
            if (!upload.Success)
            {
                return GenericResultModel<RequestEntity>.Fail(upload.Message);
            }

            return GenericResultModel<RequestEntity>.Ok(request);
        }

        public async Task Update(RequestUpdateAreaEnum area, RequestEntity update)
            => await RequestRepository.Update(area, update);

        public async Task<int> Count(RequestFilter filter)
            => await RequestRepository.Count(filter);

        public async Task<List<RequestEntity>> Load(RequestFilter filter)
            => await RequestRepository.Load(filter);

        public async Task<RequestModel> Get()
            => new() { Data = null, Requester = null, Settings = await SettingService.GetCached() };

        public async Task<RequestEntity?> Get(string number, string[]? properties = null)
            => await Get(new RequestFilter { Number = number ?? "-" }, properties);

        public async Task<GenericResultModel<RequestModel>> Get(long id, long userId)
            => await Get(userId, await Get(new RequestFilter { Id = id }), true);

        public async Task<GenericResultModel<RequestModel>> Get(string number, long userId)
            => await Get(userId, await Get(number), false);

        public async Task<GenericResultModel> Cancel(string number, long userId)
        {
            var request = await Get(number);

            if (
                request is not null && request.RequesterId == userId &&
                (request.Status == RequestStatusEnum.Pending || request.Status == RequestStatusEnum.Negotiation)
            )
            {
                await InvoiceService.Update(
                    InvoiceUpdateAreaEnum.Cancel,
                    new() { RequestId = request.Id }
                );

                await Update(
                    RequestUpdateAreaEnum.Status,
                    new() { Id = request.Id, Status = RequestStatusEnum.Cancelled }
                );
            }

            return GenericResultModel.Ok();
        }

        public async Task<GenericResultModel> Resolve(string number, long userId, decimal? rating)
        {
            var request = await Get(number);

            if (request is not null && request.RequesterId == userId && request.Status == RequestStatusEnum.Agreed)
            {
                var proposal = await ProposalService.Get(request.AgreementId ?? 0);
                if (proposal is null || proposal.CompanyId != request.ExecutorId || proposal.Status != RequestProposalStatusEnum.Accepted)
                {
                    return GenericResultModel.Fail("Something went wrong: no proposal found.");
                }

                await InvoiceService.Update(
                    InvoiceUpdateAreaEnum.Cancel,
                    new() { RequestId = request.Id }
                );

                var invoice = await InvoiceService.Create(request, proposal);

                await Update(
                    RequestUpdateAreaEnum.Status,
                    new() { Id = request.Id, Status = RequestStatusEnum.Resolved }
                );
                await ProposalService.Update(proposal.Id, invoice.Id);

                await UserService.Rate(proposal.CompanyId, userId, rating ?? 0);
            }

            return GenericResultModel.Ok();
        }

        public async Task<GenericResultModel<dynamic>> LoadGrid(long userId)
        {
            var user = await UserService.Get(userId,
                [nameof(UserEntity.Id), nameof(UserEntity.Role), nameof(UserEntity.Interests)]
            );
            var viewerId = user?.Id ?? 0;
            var viewerRole = user?.Role ?? "-";
            var viewerInterests = user?.Interests ?? [];
            var filter = new RequestFilter { ViewerId = viewerId, ViewerRole = viewerRole, ViewerInterests = viewerInterests };

            var total = await Count(filter);

            filter.ViewerFocus = RequestViewerFocusEnum.Potential;
            filter.Status = viewerRole == UserRoleEnum.Company ? null : RequestStatusEnum.Pending;
            var pending = await Count(filter);

            filter.ViewerFocus = null;
            filter.Status = RequestStatusEnum.Agreed;
            var agreed = await Count(filter);

            filter.ViewerFocus = null;
            filter.Status = RequestStatusEnum.Resolved;
            var resolved = await Count(filter);

            return GenericResultModel<dynamic>.Ok(new
            {
                viewerRole,
                viewerInterests,
                customerHasRequests = total > 0,
                total,
                pending,
                agreed,
                resolved
            });
        }

        public async Task<GridResultModel<object>> LoadGrid(RequestFilter filter, int draw, long userId, string baseUrl)
        {
            var user = await UserService.Get(userId,
                [nameof(UserEntity.Id), nameof(UserEntity.Role), nameof(UserEntity.Interests)]
            );
            filter.ViewerId = user?.Id ?? 0;
            filter.ViewerRole = user?.Role ?? "-";
            filter.ViewerInterests = user?.Interests ?? [];

            var viewerIsCompany = filter.ViewerRole == UserRoleEnum.Company;

            var requests = await Load(filter);
            var filtered = await Count(filter);
            var total = await Count(new RequestFilter
            {
                ViewerId = user?.Id ?? 0,
                ViewerRole = user?.Role ?? "-",
                ViewerInterests = user?.Interests ?? []
            });

            var files = await RequestFileRepository.Load(
                null,
                requests.Count == 0 ? [0] : [.. requests.Select(r => r.Id)],
                true
            );
            var proposals = await ProposalService.Load(
                requests.Count == 0 ? [0] : [.. requests.Select(r => r.Id)]
            );

            var requesters = requests.Select(r => r.RequesterId);
            var executors = viewerIsCompany ? [] : proposals.Select(p => p.CompanyId);
            var users = await UserService.Load(
                requests.Count == 0 ? [0] : requesters.Concat(executors),
                [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.Avatar)]
            );

            return new GridResultModel<object>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = requests.Select(r =>
                {
                    var proposal = proposals.Where(p => !viewerIsCompany || p.CompanyId == filter.ViewerId)
                                            .Where(p => p.Status != RequestProposalStatusEnum.Rejected)
                                            .Where(p => p.RequestId == r.Id)
                                            .OrderBy(p => p.Status).FirstOrDefault();

                    return new
                    {
                        id = r.Id,
                        number = r.Number,
                        status = r.Status,
                        service = r.Service,
                        title = r.Title,
                        createDate = r.CreateDate.ToString("MMM d, yyyy"),
                        currency = r.Currency,
                        asap = r.ASAP,
                        cost = r.Cost,
                        date = r.Date?.ToString("MMM d, yyyy"),
                        time = r.Time?.ToString("HH:mm"),
                        proposal = proposal == null ? null : new
                        {
                            cost = proposal.Cost,
                            date = proposal.Date?.ToString("MMM d, yyyy"),
                            time = proposal.Time?.ToString("HH:mm"),
                            status = proposal.Status
                        },
                        imageUrl = FileService.GetUrl(files.FirstOrDefault(f => f.RequestId == r.Id)?.File, baseUrl),
                        requester = UserService.GetAvatar(
                            users.FirstOrDefault(u => u.Id == (viewerIsCompany ? r.RequesterId : proposal?.CompanyId ?? r.RequesterId))
                        )
                    };
                })
            };
        }

        public async Task<GenericResultModel<RequestViewModel>> PrepareValidation(RequestViewModel model)
        {
            // load data
            var settings = await SettingService.GetCached();
            var requestFiles = await RequestFileRepository.Load(model.Id);

            // prepare ...
            model.SetValidationExternals(
                settings.RequestImageMaxCount,
                settings.RequestImageMaxSize,
                settings.RequestVideoMaxCount,
                settings.RequestVideoMaxSize,
                requestFiles.Count(i =>
                    i.Type == RequestFileTypeEnum.Image &&
                    model.KeepFileIds.Contains(i.Id)
                ),
                requestFiles.Count(i =>
                    i.Type == RequestFileTypeEnum.Video &&
                    model.KeepFileIds.Contains(i.Id)
                )
            );
            return GenericResultModel<RequestViewModel>.Ok(model, null);
        }

        private async Task<RequestEntity?> Get(RequestFilter filter, string[]? properties = null)
            => await RequestRepository.Get(filter, properties);

        private async Task<GenericResultModel<RequestModel>> Get(long userId, RequestEntity? request, bool edit)
        {
            if (request is null)
            {
                return GenericResultModel<RequestModel>.Fail();
            }

            if (edit && request.RequesterId != userId)
            {
                return GenericResultModel<RequestModel>.Fail();
            }

            var viewer = (await UserService.Get(userId, [nameof(UserEntity.Id), nameof(UserEntity.Role), nameof(UserEntity.Interests)]))!;

            if (viewer.Role == UserRoleEnum.Administrator || viewer.Id == request.RequesterId || viewer.Id == request.ExecutorId)
            {
                // ok, viewer is allowed ...
            }
            else
            {
                var potential = await RequestRepository.Load(new RequestFilter
                {
                    Id = request.Id,
                    ViewerId = viewer.Id,
                    ViewerRole = viewer.Role,
                    ViewerInterests = viewer.Interests,
                    ViewerFocus = RequestViewerFocusEnum.Potential
                });
                if (potential.Count == 0)
                {
                    return GenericResultModel<RequestModel>.Fail();
                }
            }

            var settings = await SettingService.GetCached();
            var files = await RequestFileRepository.Load(request.Id);
            var requester = await UserService.Get(request.RequesterId, [nameof(UserEntity.Name), nameof(UserEntity.Avatar)]);

            var proposals = edit == true ? [] : await ProposalService.Load([request.Id]);
            var proposal = proposals.OrderByDescending(p => p.Id).FirstOrDefault() ??
                           new RequestProposalEntity { Status = string.Empty };
            proposal?.ServiceTerms = FileService.GetUrl(proposal.ServiceTerms);
            var proposalCompany = await UserService.Get(
                proposal?.CompanyId ?? 0,
                [nameof(UserEntity.Id), nameof(UserEntity.Name), nameof(UserEntity.Avatar), nameof(UserEntity.Rating)]
            );

            return GenericResultModel<RequestModel>.Ok(new RequestModel
            {
                Data = request,
                Settings = settings,
                Requester = UserService.GetAvatar(requester),
                Proposal = proposal,
                ProposalCompany = proposalCompany is null ? null : UserService.GetAvatar(proposalCompany),
                Files = [.. files.Select(i => new RequestFileModel
                {
                    Id = i.Id,
                    Size = i.Size,
                    Type = i.Type,
                    IsMain = i.IsMain,
                    Url = FileService.GetUrl(i.File) ?? "undefined",
                    Name = FileService.GetName(i.File) ?? "undefined"
                }).OrderBy(i => i.Type).ThenByDescending(f => f.IsMain)]
            });
        }

        private async Task<GenericResultModel> UploadMedia(RequestViewModel model, List<RequestFileEntity> existingFiles)
        {
            model.Images ??= [];
            model.Videos ??= [];
            model.KeepFileIds ??= [];
            var fileEntities = new List<RequestFileEntity>();

            // seek existing files to be deleted
            var deletions = existingFiles.Where(i => !model.KeepFileIds.Contains(i.Id)).ToList();

            // delete request files not being kept and their storage
            await RequestFileRepository.Delete<RequestFileEntity>([.. deletions.Select(i => i.Id)]);
            foreach (var rf in deletions)
            {
                await FileService.Delete(rf.File);
            }

            // add new images ...
            for (var i = 0; i < model.Images.Length; i++)
            {
                var file = await FileService.Create(
                    model.Images[i],
                    null,
                    model.ImageMaxSize,
                    [FileTypeEnum.PNG, FileTypeEnum.JPEG]
                );
                if (file.Message is not null)
                {
                    return GenericResultModel.Fail(file.Message);
                }
                fileEntities.Add(new RequestFileEntity
                {
                    RequestId = model.Id,
                    File = file.Result ?? "-",
                    Type = RequestFileTypeEnum.Image,
                    Size = model.Images[i].Length,
                    IsMain = i == model.MainImageIndex
                });
            }

            // add new videos ...
            foreach (var vid in model.Videos)
            {
                var file = await FileService.Create(
                    vid,
                    null,
                    model.VideoMaxSize,
                    [FileTypeEnum.MP4, FileTypeEnum.MOV]
                );
                if (file.Message is not null)
                {
                    return GenericResultModel.Fail(file.Message);
                }
                fileEntities.Add(new RequestFileEntity
                {
                    RequestId = model.Id,
                    File = file.Result ?? "-",
                    Type = RequestFileTypeEnum.Video,
                    Size = vid.Length
                });
            }

            // save new request files
            await RequestFileRepository.Create(fileEntities);

            // set main file
            await RequestFileRepository.SetMain(
                model.Id,
                model.KeepMainFileId > 0 ? model.KeepMainFileId :
                model.MainImageIndex < fileEntities.Count ? fileEntities[model.MainImageIndex].Id :
                0
            );

            // ...
            return GenericResultModel.Ok();
        }

        private static RequestEntity BuildRequest(RequestEntity? entity, RequestViewModel request, long userId)
        {
            entity ??= new RequestEntity
            {
                Number = Guid.NewGuid().ToString("N"),
                CreateDate = DateTime.Now,
                RequesterId = userId
            };

            entity.Status = RequestStatusEnum.Pending;
            entity.Service = request.Service;
            entity.Title = request.Title.Trim();
            entity.Description = request.Description?.Trim() ?? "";
            entity.PickupAddress = request.PickupAddress?.Trim() ?? "";
            entity.PickupZipCode = request.PickupZipCode?.Trim() ?? "";
            entity.DeliveryAddress = request.DeliveryAddress?.Trim() ?? "";
            entity.DeliveryZipCode = request.DeliveryZipCode?.Trim() ?? "";
            entity.Cost = request.Cost;
            entity.Currency = "EUR";
            entity.ASAP = request.IsASAP;
            entity.Date = !request.IsASAP ? DateOnly.Parse(request.Date!) : null;
            entity.Time = !request.IsASAP ? TimeOnly.Parse(request.Time!) : null;

            if (request.Service == ServiceEnum.Transport)
            {
                entity.VehicleType = request.VehicleType?.Trim();
                entity.VehicleCondition = request.VehicleCondition?.Trim();
                entity.PresentElevator = false;
                entity.PresentParking = false;
            }
            else
            {
                entity.VehicleType = null;
                entity.VehicleCondition = null;
                entity.PresentElevator = request.PresentElevator;
                entity.PresentParking = request.PresentParking;
            }

            return entity;
        }

    }
}
