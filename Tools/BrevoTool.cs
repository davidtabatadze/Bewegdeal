using Bewegdeal.Enums;
using System.Text;
using System.Text.Json;

namespace Bewegdeal.Tools
{
    /// <summary>
    /// Provides static methods for configuring and sending transactional emails and SMS messages using the Brevo service.
    /// </summary>
    /// <remarks>This class is intended for use in applications that need to send transactional emails and SMS via
    /// Brevo. Before sending, call Configure to initialize the required settings from the application's
    /// configuration. All methods are thread-safe and can be used concurrently. This class cannot be
    /// instantiated.</remarks>
    public static class BrevoTool
    {
        private static readonly HttpClient Http = new();
        private static string _apiKey = string.Empty;
        private static string _fromName = string.Empty;
        private static string _fromEmail = string.Empty;

        /// <summary>
        /// Reads Brevo settings from <c>appsettings.json</c> (section <c>Brevo</c>).
        /// Must be called once in <c>Program.cs</c> before the app starts handling requests.
        /// </summary>
        public static void Configure(IConfiguration configuration)
        {
            _apiKey = configuration["Brevo:ApiKey"] ?? throw new InvalidOperationException("Brevo:ApiKey is not configured.");
            _fromName = configuration["Brevo:FromName"] ?? throw new InvalidOperationException("Brevo:FromName is not configured.");
            _fromEmail = configuration["Brevo:FromEmail"] ?? throw new InvalidOperationException("Brevo:FromEmail is not configured.");
        }

        /// <summary>
        /// Sends a transactional SMS message asynchronously.
        /// </summary>
        /// <param name="mobile">The recipient's phone number in E.164 format (e.g. <c>+43664XXXXXXX</c>).</param>
        /// <param name="content">The text content of the SMS. Cannot be null or empty.</param>
        /// <returns>
        /// <see cref="BrevoStatusEnum.Sent"/> if the message was accepted by Brevo;
        /// otherwise <see cref="BrevoStatusEnum.Failed"/>.
        /// </returns>
        public static async Task<BrevoStatusEnum> SendSms(string mobile, string content)
        {
            try
            {
                // ready the payload
                var payload = JsonSerializer.Serialize(BuildSmsPayload(mobile, content));

                // configure the request
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/transactionalSMS/sms");
                request.Headers.Add("api-key", _apiKey);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                // do send the request
                var response = await Http.SendAsync(request);

                // return the status
                return response.StatusCode == System.Net.HttpStatusCode.Created ?
                       BrevoStatusEnum.Sent : BrevoStatusEnum.Failed;
            }
            catch (Exception)
            {
                // TODO: do log here
                return BrevoStatusEnum.Failed;
            }
        }

        /// <summary>
        /// Sends an email message asynchronously using the configured SMTP API.
        /// </summary>
        /// <param name="email">The recipient's email address. Cannot be null or empty.</param>
        /// <param name="subject">The subject line of the email message. Cannot be null or empty.</param>
        /// <param name="content">The HTML content of the email message. Cannot be null or empty.</param>
        /// <param name="text">The plain text version of the email message. Optional; if not provided, only the HTML content will be sent.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is a string indicating the status of the
        /// email send operation. Returns "Sent" if the email was sent successfully; otherwise, returns "Failed".</returns>
        public static async Task<BrevoStatusEnum> SendEmail(string email, string subject, string content, string? text = null)
        {
            try
            {
                // ready the payload
                var payload = JsonSerializer.Serialize(BuildEmailPayload(email, subject, content, text));

                // configure the request
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", _apiKey);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                // do send the request
                var response = await Http.SendAsync(request);

                // return the status
                return response.StatusCode == System.Net.HttpStatusCode.Created ?
                       BrevoStatusEnum.Sent : BrevoStatusEnum.Failed;
            }
            catch (Exception)
            {
                // TODO: do log here
                return BrevoStatusEnum.Failed;
            }
        }

        /// <summary>
        /// Builds a payload dictionary for an SMS message with the specified recipient and content.
        /// </summary>
        /// <param name="mobile">The recipient's phone number in E.164 format.</param>
        /// <param name="content">The text content of the SMS.</param>
        /// <returns>A dictionary containing the SMS payload fields expected by the Brevo API.</returns>
        private static Dictionary<string, object> BuildSmsPayload(string mobile, string content)
        {
            return new Dictionary<string, object>
            {
                ["sender"] = _fromName,
                ["recipient"] = mobile,
                ["content"] = content,
                ["type"] = "transactional",
            };
        }

        /// <summary>
        /// Builds a payload dictionary for an email message with the specified sender, recipient, subject, and content.
        /// </summary>
        /// <param name="email">The email address of the recipient. Cannot be null.</param>
        /// <param name="subject">The subject line of the email message. Cannot be null.</param>
        /// <param name="content">The HTML content of the email message. Cannot be null.</param>
        /// <param name="text">The plain text content of the email message. If null, the payload will not include a plain text version.</param>
        /// <returns>A dictionary containing the email payload, including sender, recipient, subject, and content fields.</returns>
        private static Dictionary<string, object> BuildEmailPayload(string email, string subject, string content, string? text)
        {
            // set up mail
            var payload = new Dictionary<string, object>
            {
                ["sender"] = new { name = _fromName, email = _fromEmail },
                ["to"] = new[] { new { email } },
                ["subject"] = subject,
                ["htmlContent"] = content,
            };

            // add text content if provided
            if (text is not null)
            {
                payload["textContent"] = text;
            }

            // all good
            return payload;
        }
    }
}
