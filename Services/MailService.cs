using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Tools;

namespace Bewegdeal.Services
{
    public class MailService
    {
        public async Task<ResultModel> Send(string email, EmailEnum type, Dictionary<string, object>? parameters = null)
        {
            var title = type.Value!;
            var template = type.ToString()!;

            var path = Path.Combine(AppContext.BaseDirectory, "Jobs", template);
            if (!File.Exists(path))
            {
                return ResultModel.Fail($"The email template file not found: {path}");
            }

            var content = await File.ReadAllTextAsync(path);
            foreach (var param in parameters ?? [])
            {
                content = content.Replace($"{{{param.Key}}}", param.Value.ToString()!);
            }

            var result = await BrevoTool.Send(
                email,
                title,
                template
            );

            return result.Value == EmailStatusEnum.Sent.Value ? ResultModel.Ok() : ResultModel.Fail("");
        }
    }
}