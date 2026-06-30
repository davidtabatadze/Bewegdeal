using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
using Bewegdeal.Services;

namespace Bewegdeal.Jobs
{
    public class InvoiceGenerationJob(IServiceScopeFactory ScopeFactory, ILogger<InvoiceGenerationJob> Logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("InvoiceGenerationJob: started");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Run();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task Run()
        {
            using var scope = ScopeFactory.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();
            var requestService = scope.ServiceProvider.GetRequiredService<RequestService>();
            var proposalService = scope.ServiceProvider.GetRequiredService<ProposalService>();

            var day = -1;
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(day));

            var proposals = await proposalService.Load(new RequestProposalFilter
            {
                Status = RequestProposalStatusEnum.Accepted,
                InvoiceId = 0,
                DateTo = date
            });

            var requests = await requestService.Load(new RequestFilter
            {
                Ids = proposals.Count == 0 ? [0] : [.. proposals.Select(p => p.RequestId)]
            });

            foreach (var proposal in proposals)
            {
                var request = requests.FirstOrDefault(r => r.Id == proposal.RequestId);
                if (request is not null)
                {
                    var invoice = await invoiceService.Create(request, proposal);
                    await proposalService.Update(proposal.Id, invoice.Id);
                }
            }
        }
    }
}
