using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Tools;

namespace Bewegdeal.Services
{
    public class BrevoService
    {
        public async Task<GenericResultModel> SendSms(string mobile, Dictionary<string, object>? parameters = null)
        {
            var content = "Hallo,\nIhr Bewegdeal-bestätigungscode lautet: {{otcode}}\nDieser Code ist {{timeout}} Minuten gültig.\nFalls sie sich nicht bei Bewegdeal registriert haben, ignorieren sie diese SMS bitte.";
            foreach (var param in parameters ?? [])
            {
                content = content.Replace("{{" + param.Key + "}}", param.Value.ToString());
            }

            var result = await BrevoTool.SendSms(mobile, content);

            return result.Value == BrevoStatusEnum.Sent.Value ? GenericResultModel.Ok() : GenericResultModel.Fail("");
        }

        public async Task<GenericResultModel> SendEmail(string email, EmailEnum type, Dictionary<string, object>? parameters = null)
        {
            var parts = type.Value.Split(" # ");
            var title = parts[0];
            var template = parts[1] + ".html";

            var path = Path.Combine(AppContext.BaseDirectory, "MailTemplates", template);
            if (!File.Exists(path))
            {
                return GenericResultModel.Fail($"The email template file not found: {path}");
            }

            var content = await File.ReadAllTextAsync(path);
            foreach (var param in parameters ?? [])
            {
                content = content.Replace("{{" + param.Key + "}}", param.Value.ToString());
            }

            var result = await BrevoTool.SendEmail(
                email,
                title,
                content
            );

            return result.Value == BrevoStatusEnum.Sent.Value ? GenericResultModel.Ok() : GenericResultModel.Fail("");
        }
    }
}