using Bewegdeal.Enums;
using System.Text;
using System.Text.Json;

namespace Bewegdeal.Tools
{
    /// <summary>
    /// Sends transactional emails via the Brevo v3 REST API.
    /// Call <see cref="Configure"/> once at startup before using <see cref="Send"/>.
    /// Uses a single shared <see cref="HttpClient"/> instance to avoid socket exhaustion.
    /// </summary>
    public static class BrevoTool
    {
        private const string ApiEndpoint = "https://api.brevo.com/v3/smtp/email";

        private static readonly HttpClient Http = new();

        private static string _apiKey = string.Empty;
        private static string _fromEmail = string.Empty;
        private static string _fromName = string.Empty;

        // ── Startup ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads Brevo settings from <c>appsettings.json</c> (section <c>Brevo</c>).
        /// Must be called once in <c>Program.cs</c> before the app starts handling requests.
        /// </summary>
        public static void Configure(IConfiguration configuration)
        {
            _apiKey = configuration["Brevo:ApiKey"] ?? throw new InvalidOperationException("Brevo:ApiKey is not configured.");
            _fromEmail = configuration["Brevo:FromEmail"] ?? throw new InvalidOperationException("Brevo:FromEmail is not configured.");
            _fromName = configuration["Brevo:FromName"] ?? throw new InvalidOperationException("Brevo:FromName is not configured.");
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a transactional email through Brevo.
        /// Sender credentials are taken from <c>appsettings.json</c> via <see cref="Configure"/>.
        /// await BrevoTool.Send("david.tabatadze@outlook.com", "Welcome", "<p>Hello!</p>");
        /// </summary>
        /// <param name="email">Recipient e-mail address.</param>
        /// <param name="subject">E-mail subject line.</param>
        /// <param name="content">HTML body of the message.</param>
        /// <param name="text"> textual alternative.</param>
        /// <returns>
        /// <see langword="EmailStatus.Sent"/> when Brevo accepted the message (HTTP 201);
        /// <see langword="EmailStatus.Failed"/> otherwise.
        /// </returns>
        public static async Task<string> Send(
            string email,
            string subject,
            string content,
            string? text = null)
        {
            try
            {
                var payload = BuildPayload(email, subject, content, text);
                var json = JsonSerializer.Serialize(payload);

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint);
                request.Headers.Add("api-key", _apiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await Http.SendAsync(request);

                return response.StatusCode == System.Net.HttpStatusCode.Created ?
                       EmailStatusEnum.Sent : EmailStatusEnum.Failed;
            }
            catch (Exception)
            {
                // TODO: do log here
                return EmailStatusEnum.Failed;
            }
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private static Dictionary<string, object> BuildPayload(
            string email,
            string subject,
            string content,
            string? text)
        {
            var payload = new Dictionary<string, object>
            {
                ["sender"] = new { name = _fromName, email = _fromEmail },
                ["to"] = new[] { new { email } },
                ["subject"] = subject,
                ["htmlContent"] = content,
            };

            if (text is not null)
            {
                payload["textContent"] = text;
            }

            return payload;
        }
    }
}
