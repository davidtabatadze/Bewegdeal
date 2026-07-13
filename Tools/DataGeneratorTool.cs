using Bewegdeal.Data;
using Bewegdeal.Data.Entities;
using Bewegdeal.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bewegdeal.Tools
{
    public class DataGeneratorTool(SqlContext context, IConfiguration configuration)
    {
        private static readonly Random Rng = new();

        private static readonly string[] Titles =
        [
            "Umzug in neue Wohnung", "Möbeltransport", "Fahrzeug abholen",
            "Sperrmüll entsorgen", "Büroumzug", "Kleintransport",
            "Möbelabholung", "Fahrzeugtransport Wien–Graz", "Umzug ins Lager",
            "Express-Abholung", "Schwertransport", "Haushaltsauflösung"
        ];

        private static readonly string[] Descriptions =
        [
            "Wir brauchen Hilfe beim Umzug. Es gibt einige schwere Möbel.",
            "Transport von Wien nach Graz, ca. 3 Umzugskartons und ein Sofa.",
            "Fahrzeug muss abgeholt und transportiert werden.",
            "Sperrmüll muss abgeholt und ordnungsgemäß entsorgt werden.",
            "Kleiner Büroumzug: 5 Schreibtische und ein paar Kartons."
        ];

        private static readonly (string Address, string Zip)[] Locations =
        [
            ("Mariahilfer Str. 50, Wien", "1060"),
            ("Ringstraße 10, Wien", "1010"),
            ("Favoritenstraße 120, Wien", "1100"),
            ("Gürtel 80, Wien", "1150"),
            ("Hauptstraße 15, Graz", "8010"),
            ("Bahnhofgürtel 5, Graz", "8020"),
            ("Landstraße 30, Linz", "4020"),
            ("Mozartplatz 3, Salzburg", "5020"),
            ("Innstraße 20, Innsbruck", "6020"),
            ("Hauptplatz 1, Klagenfurt", "9020")
        ];

        private static readonly string[] ChatMessages =
        [
            "Hallo, ich bin interessiert.", "Können wir den Termin besprechen?",
            "Wann sind Sie verfügbar?", "Das klingt gut.", "Wie hoch sind die Kosten?",
            "Ich habe Ihren Vorschlag erhalten.", "Danke für die schnelle Antwort.",
            "Ich bin einverstanden.", "Können wir Details klären?", "Perfekt, danke!"
        ];

        private static readonly string[] Services =
        [
            ServiceEnum.Moving, ServiceEnum.Removal, ServiceEnum.Pickup, ServiceEnum.Transport
        ];

        private static readonly string[] VehicleConditions =
        [
            VehicleConditionEnum.WorksProperly, VehicleConditionEnum.CanRun, VehicleConditionEnum.Damaged
        ];

        private static readonly string[] RequestStatuses =
        [
            RequestStatusEnum.Pending, RequestStatusEnum.Pending,
            RequestStatusEnum.Negotiation, RequestStatusEnum.Negotiation,
            RequestStatusEnum.Agreed, RequestStatusEnum.Agreed,
            RequestStatusEnum.Resolved, RequestStatusEnum.Resolved,
            RequestStatusEnum.Cancelled,
            RequestStatusEnum.Declined
        ];

        public async Task Generate()
        {
            var section = configuration.GetSection("DataGenerator");
            if (!section.GetValue<bool>("Enabled") && false) { return; }

            var dayRange = section.GetValue("DayRange", 0);
            var dataRange = section.GetValue("DataRange", 0);

            var existingCount = 0; //await context.Requests.CountAsync();
            if (existingCount >= dataRange) { return; }

            var customers = await context.Users
                .Where(u => u.Role == UserRoleEnum.Customer && u.Status == UserStatusEnum.Active)
                .Where(u => u.Id >= 8)
                .Select(u => u.Id)
                .ToListAsync();

            var companies = await context.Users
                .Where(u => u.Role == UserRoleEnum.Company && u.Status == UserStatusEnum.Active)
                .Where(u => u.Id >= 8)
                .Select(u => u.Id)
                .ToListAsync();

            if (customers.Count == 0 || companies.Count == 0) { return; }

            var settings = await context.Settings.FirstOrDefaultAsync();
            var commissionPct = settings?.InvoiceCommissionPersent ?? 10;
            var taxPct = settings?.InvoiceTaxPersent ?? 20;
            var dueDays = settings?.InvoiceDueDays ?? 30;

            var now = DateTime.Now;
            var from = now.AddDays(-dayRange);
            var toGenerate = dataRange - existingCount;

            // ── Phase 1: requests ──────────────────────────────────────────────
            var requestMeta = new List<(RequestEntity Request, long CustomerId, long CompanyId)>(toGenerate);

            for (var i = 0; i < toGenerate; i++)
            {
                var customerId = Pick(customers);
                var companyId = Pick(companies);
                var service = Pick(Services);
                var status = Pick(RequestStatuses);
                var createDate = RandomDate(from, now);
                var pickup = Pick(Locations);
                var delivery = Pick(Locations);
                var asap = Rng.NextDouble() > 0.5;

                var request = new RequestEntity
                {
                    Number = Guid.NewGuid().ToString("N"),
                    Status = status,
                    Service = service,
                    Title = Pick(Titles),
                    Description = Pick(Descriptions),
                    PickupAddress = pickup.Address,
                    PickupZipCode = pickup.Zip,
                    DeliveryAddress = delivery.Address,
                    DeliveryZipCode = delivery.Zip,
                    RequesterId = customerId,
                    Cost = Math.Round((decimal)(Rng.NextDouble() * 900 + 100), 2),
                    Currency = "EUR",
                    ASAP = asap,
                    Date = asap ? null : DateOnly.FromDateTime(createDate.AddDays(Rng.Next(1, 30))),
                    Time = asap ? null : new TimeOnly(Rng.Next(8, 18), 0),
                    PresentElevator = Rng.NextDouble() > 0.5,
                    PresentParking = Rng.NextDouble() > 0.5,
                    CreateDate = createDate
                };

                if (service == ServiceEnum.Transport)
                {
                    request.VehicleType = "PKW";
                    request.VehicleCondition = Pick(VehicleConditions);
                }

                context.Requests.Add(request);
                requestMeta.Add((request, customerId, companyId));
            }

            await context.SaveChangesAsync();

            // ── Phase 2: chats ─────────────────────────────────────────────────
            var chatMeta = new List<(ChatEntity Chat, RequestEntity Request, long CustomerId, long CompanyId)>();

            foreach (var (request, customerId, companyId) in requestMeta)
            {
                if (request.Status != RequestStatusEnum.Negotiation &&
                    request.Status != RequestStatusEnum.Agreed &&
                    request.Status != RequestStatusEnum.Resolved)
                {
                    continue;
                }

                var chatStatus = request.Status == RequestStatusEnum.Negotiation
                    ? ChatStatusEnum.Ongoing
                    : ChatStatusEnum.Agreed;

                var fraud = Rng.NextDouble() < 0.15 ? ChatFraudEnum.Dubious : ChatFraudEnum.Safe;

                var chat = new ChatEntity
                {
                    Key = Guid.NewGuid().ToString("N"),
                    RequestId = request.Id,
                    RequestNumber = request.Number,
                    CustomerId = customerId,
                    CompanyId = companyId,
                    Fraud = fraud,
                    Status = chatStatus,
                    CreateDate = request.CreateDate
                };

                context.Chats.Add(chat);
                chatMeta.Add((chat, request, customerId, companyId));
            }

            await context.SaveChangesAsync();

            // ── Phase 3: chat messages ─────────────────────────────────────────
            foreach (var (chat, request, customerId, companyId) in chatMeta)
            {
                var count = Rng.Next(2, 7);
                var fraudMessageIndex = chat.Fraud == ChatFraudEnum.Dubious ? Rng.Next(0, count) : -1;
                for (var m = 0; m < count; m++)
                {
                    context.ChatMessages.Add(new ChatMessageEntity
                    {
                        ChatId = chat.Id,
                        SenderId = m % 2 == 0 ? customerId : companyId,
                        Content = Pick(ChatMessages),
                        SentDate = request.CreateDate.AddHours(m + 1),
                        IsRead = true,
                        IsFraud = m == fraudMessageIndex
                    });
                }
            }

            await context.SaveChangesAsync();

            // ── Phase 4: proposals ─────────────────────────────────────────────
            var proposalMeta = new List<(RequestProposalEntity Proposal, RequestEntity Request, long CompanyId)>();

            foreach (var (chat, request, customerId, companyId) in chatMeta)
            {
                if (request.Status != RequestStatusEnum.Agreed &&
                    request.Status != RequestStatusEnum.Resolved)
                {
                    continue;
                }

                var proposalDate = request.CreateDate.AddDays(Rng.Next(1, 10));

                var proposal = new RequestProposalEntity
                {
                    ChatId = chat.Id,
                    RequestId = request.Id,
                    CompanyId = companyId,
                    CustomerId = customerId,
                    Cost = request.Cost,
                    Currency = "EUR",
                    Date = DateOnly.FromDateTime(proposalDate.AddDays(Rng.Next(1, 14))),
                    Time = new TimeOnly(Rng.Next(8, 18), 0),
                    ServiceTerms = null, //"Standard service terms apply.",
                    Status = RequestProposalStatusEnum.Accepted,
                    Service = request.Service,
                    CreateDate = proposalDate,
                    ReactionDate = proposalDate.AddHours(Rng.Next(1, 24))
                };

                context.RequestProposals.Add(proposal);
                proposalMeta.Add((proposal, request, companyId));
            }

            await context.SaveChangesAsync();

            // set ExecutorId on agreed/resolved requests
            foreach (var (proposal, request, companyId) in proposalMeta)
            {
                request.ExecutorId = companyId;
            }

            await context.SaveChangesAsync();

            // ── Phase 5: invoices (resolved only) ─────────────────────────────
            var invoiceMeta = new List<(InvoiceEntity Invoice, RequestProposalEntity Proposal)>();

            foreach (var (proposal, request, companyId) in proposalMeta)
            {
                if (request.Status != RequestStatusEnum.Resolved) { continue; }

                var commission = proposal.Cost / 100 * commissionPct;
                var tax = commission / 100 * taxPct;
                var invoiceDate = proposal.CreateDate.AddDays(Rng.Next(1, 5));

                var invoice = new InvoiceEntity
                {
                    Number = Guid.NewGuid().ToString("N"),
                    Status = InvoiceStatusEnum.Paid,
                    Service = request.Service,
                    RequestId = request.Id,
                    RequestNumber = request.Number,
                    ProposalId = proposal.Id,
                    CompanyId = companyId,
                    CustomerId = request.RequesterId,
                    Currency = "EUR",
                    ServiceCost = proposal.Cost,
                    CommissionPersent = commissionPct,
                    CommissionCost = commission,
                    TaxPersent = taxPct,
                    TaxCost = tax,
                    TotalCost = commission + tax,
                    NotificationSent = true,
                    CreateDate = invoiceDate,
                    DueDate = invoiceDate.AddDays(dueDays),
                    PaymentDate = invoiceDate.AddDays(Rng.Next(1, dueDays))
                };

                context.Invoices.Add(invoice);
                invoiceMeta.Add((invoice, proposal));
            }

            await context.SaveChangesAsync();

            // link invoice back to proposal
            foreach (var (invoice, proposal) in invoiceMeta)
            {
                proposal.InvoiceId = invoice.Id;
            }

            await context.SaveChangesAsync();
        }

        private static T Pick<T>(IList<T> list) => list[Rng.Next(list.Count)];
        private static T Pick<T>(T[] arr) => arr[Rng.Next(arr.Length)];

        private static DateTime RandomDate(DateTime from, DateTime to)
        {
            var seconds = (to - from).TotalSeconds;
            return from.AddSeconds(Rng.NextDouble() * seconds);
        }
    }
}
