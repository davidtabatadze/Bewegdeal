using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Bewegdeal.Models;
using Bewegdeal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireAdmin]
    public class FraudWordController(IFraudWordRepository fraudWordRepo, UserService userService) : Controller
    {
        private readonly IFraudWordRepository _fraudWordRepo = fraudWordRepo;

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoadWords([FromQuery] FraudWordFilter filter, [FromQuery] int draw = 1)
        {
            var total = await _fraudWordRepo.Count(new FraudWordFilter());
            var filtered = await _fraudWordRepo.Count(filter);
            var rows = await _fraudWordRepo.Load(filter);

            var data = rows.Select(w => new
            {
                id = w.Id,
                word = w.Word,
                description = w.Description,
                status = w.Status,
                createdAt = w.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                createdByName = w.CreatedByName
            });

            return Json(new GridResultViewModel<object> { }); //new GridResultViewModel<object>(draw, total, filtered, data.Cast<object>()));
        }

        [HttpPost]
        public async Task<IActionResult> Add(string word, string description)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                TempData["Error"] = "Fraud word is required.";
                return RedirectToAction(nameof(Index));
            }

            long.TryParse(HttpContext.Session.GetString(ConstantEnum.SessionUserId), out var userId);
            var user = await userService.Get(userId);

            await _fraudWordRepo.Create(new FraudWordEntity
            {
                Word = word.Trim(),
                Description = description?.Trim() ?? string.Empty,
                Status = FraudWordStatusEnum.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedByName = user?.Name ?? "Unknown"
            });

            TempData["Success"] = "Fraud word added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(long id, string word, string description)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return BadRequest("Word is required.");
            }

            await _fraudWordRepo.Update(id, word.Trim(), description?.Trim() ?? string.Empty);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var entry = await _fraudWordRepo.Get(new FraudWordFilter { Id = id });
            if (entry == null)
            {
                return NotFound();
            }

            var next = entry.Status == FraudWordStatusEnum.Active
                ? FraudWordStatusEnum.Disabled
                : FraudWordStatusEnum.Active;

            await _fraudWordRepo.SetStatus(id, next);
            return Json(new { status = next });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            // await _fraudWordRepo.Delete(id);
            return Ok();
        }
    }
}
