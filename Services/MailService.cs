using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Tools;

namespace Bewegdeal.Services
{
    public class BrevoService
    {
        public async Task<ResultModel> SendSms(string mobile, Dictionary<string, object>? parameters = null)
        {
            var content = "Hello,\nYour Bewegdeal verification code is: {{otcode}}\nThis code expires in {{timeout}} minutes.\nIf you did not register on Bewegdeal, please ignore this sms.";
            foreach (var param in parameters ?? [])
            {
                content = content.Replace("{{" + param.Key + "}}", param.Value.ToString());
            }

            var result = await BrevoTool.SendSms(mobile, content);

            return result.Value == BrevoStatusEnum.Sent.Value ? ResultModel.Ok() : ResultModel.Fail("");
        }

        public async Task<ResultModel> SendEmail(string email, EmailEnum type, Dictionary<string, object>? parameters = null)
        {
            var parts = type.Value.Split(" # ");
            var title = parts[0];
            var template = parts[1] + ".html";

            var path = Path.Combine(AppContext.BaseDirectory, "MailTemplates", template);
            if (!File.Exists(path))
            {
                return ResultModel.Fail($"The email template file not found: {path}");
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

            return result.Value == BrevoStatusEnum.Sent.Value ? ResultModel.Ok() : ResultModel.Fail("");
        }
    }
}