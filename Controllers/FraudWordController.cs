using Bewegdeal.Enums;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize(Roles = UserRoleEnum.Administrator)]
    public class FraudWordController(FraudWordService FraudWordService) : XBaseController
    {

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await FraudWordService.Load());
        }

        [HttpPost]
        public async Task<IActionResult> Create(string word)
        {
            await FraudWordService.Create(word);
            return Json(GenericResultModel.Ok());
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string word)
        {
            await FraudWordService.Delete(word);
            return Json(GenericResultModel.Ok());
        }

    }
}
