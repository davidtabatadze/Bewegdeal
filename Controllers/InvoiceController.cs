using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Enums;
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
            var data = await InvoiceService.LoadGrid(UserId, UserRole);
            ViewBag.Total = data.Result?.total ?? 0;
            ViewBag.Paid = (int)(data.Result?.paid ?? 0);
            ViewBag.Pending = (int)(data.Result?.pending ?? 0);
            ViewBag.Users = data.Result?.users ?? 0;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoadInvoices([FromQuery] InvoiceFilter filter, [FromQuery] int draw = 1)
        {
            return Json(await InvoiceService.LoadGrid(filter, draw, UserId, UserRole));
        }

        [HttpGet]
        public async Task<IActionResult> Print(string number)
        {
            var result = await InvoiceService.Get(number, UserId, UserRole);

            if (!result.Success || result.Result is null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Invoice = result.Result;
            return View();
        }

        [Authorize(Roles = UserRoleEnum.Administrator)]
        [HttpPost]
        public async Task<IActionResult> UpdateInvoiceStatus(long id, string status)
        {
            var invoice = await InvoiceService.Get(id, [nameof(InvoiceEntity.Id), nameof(InvoiceEntity.Status)]);

            if (invoice is null || (status != InvoiceStatusEnum.Paid && status != InvoiceStatusEnum.Cancelled))
            {
                return BadRequest();
            }

            await InvoiceService.Update(InvoiceUpdateAreaEnum.Status, new InvoiceEntity
            {
                Id = id,
                Status = status,
                PaymentDate = status == InvoiceStatusEnum.Paid ? DateTime.Now : null
            });

            return Json(new { status });
        }

        #endregion
    }
}
