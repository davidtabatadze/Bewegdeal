using Bewegdeal.Data.Filters;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [Authorize]
    public class InvoiceController(InvoiceService InvoiceService) : XBaseController
    {
        #region List

        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoadInvoices([FromQuery] InvoiceFilter filter, [FromQuery] int draw = 1)
        {
            return Json(await InvoiceService.LoadGrid(filter, draw, UserId));
        }

        #endregion
    }
}
