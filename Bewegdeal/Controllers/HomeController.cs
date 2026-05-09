using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Filters;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Enums;
using Bewegdeal.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Bewegdeal.Controllers
{
    [RequireLogin]
    public class HomeController(
        IUserRepository userRepository,
        ITaskRepository taskRepository,
        IWebHostEnvironment env) : Controller
    {
        // ── Dashboard / My Requests ───────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var userId = long.Parse(HttpContext.Session.GetString("UserId")!);
            var tasks  = await taskRepository.GetAll(new TaskFilter { UserId = userId });

            ViewBag.TotalCount     = tasks.Count;
            ViewBag.ActiveCount    = tasks.Count(t => t.Status == TaskStatusEnum.Active);
            ViewBag.PendingCount   = tasks.Count(t => t.Status == TaskStatusEnum.Pending);
            ViewBag.CompletedCount = tasks.Count(t => t.Status == TaskStatusEnum.Completed);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var userId = long.Parse(HttpContext.Session.GetString("UserId")!);
            var tasks  = await taskRepository.GetAll(new TaskFilter { UserId = userId });

            var data = tasks.Select(t => new
            {
                id        = t.Id,
                name      = t.Name,
                type      = t.Type,
                image     = t.Image,
                cost      = t.Cost,
                currency  = t.Currency ?? "EUR",
                status    = t.Status,
                views     = t.Views,
                createdAt = t.CreatedAt.ToString("dd.MM.yyyy")
            });

            return Json(new { data });
        }

        // ── New Request ───────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult NewRequest() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewRequest(
            string? type,
            string? name,
            string? description,
            string? pickupAddress,
            string? deliveryAddress,
            decimal? cost,
            string? currency,
            IFormFileCollection photos,
            IFormFile? video,
            int mainPhotoIndex)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error = "Service type and request title are required.";
                return View();
            }

            var userId   = long.Parse(HttpContext.Session.GetString("UserId")!);
            var uploadId = Guid.NewGuid().ToString("N")[..12];
            var uploadDir = Path.Combine(env.WebRootPath, "uploads", "tasks");
            Directory.CreateDirectory(uploadDir);

            string?       mainImagePath = null;
            List<string>  mediaPaths    = [];

            // Save photos (main photo first in the list)
            for (int i = 0; i < photos.Count; i++)
            {
                var photo = photos[i];
                if (photo.Length == 0) { continue; }

                var ext      = Path.GetExtension(photo.FileName).ToLowerInvariant();
                var fileName = $"{uploadId}_p{i}{ext}";
                var filePath = Path.Combine(uploadDir, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await photo.CopyToAsync(stream);

                var relativePath = $"/uploads/tasks/{fileName}";

                // The first photo in the submitted list is always the main one
                // (JS reorders so mainPhotoIndex=0 before submit)
                if (i == 0)
                {
                    mainImagePath = relativePath;
                }
                else
                {
                    mediaPaths.Add(relativePath);
                }
            }

            // Save video
            if (video is { Length: > 0 })
            {
                var ext      = Path.GetExtension(video.FileName).ToLowerInvariant();
                var fileName = $"{uploadId}_v{ext}";
                var filePath = Path.Combine(uploadDir, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await video.CopyToAsync(stream);

                mediaPaths.Add($"/uploads/tasks/{fileName}");
            }

            var task = await taskRepository.Create(new TaskEntity
            {
                UserId          = userId,
                Type            = type,
                Name            = name.Trim(),
                Description     = description?.Trim(),
                Image           = mainImagePath,
                Media           = mediaPaths.Count > 0 ? string.Join(',', mediaPaths) : null,
                Cost            = cost,
                Currency        = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency,
                PickupAddress   = pickupAddress?.Trim(),
                DeliveryAddress = deliveryAddress?.Trim(),
                Status          = TaskStatusEnum.Active,
                Views           = 0,
                CreatedAt       = DateTime.UtcNow
            });

            return RedirectToAction(nameof(Index));
        }

        // ── Edit Task ────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> EditTask(long id)
        {
            var userId = long.Parse(HttpContext.Session.GetString("UserId")!);
            var task   = await taskRepository.Get(new TaskFilter { Id = id, UserId = userId });

            if (task is null)                            { return NotFound(); }
            if (task.Status != TaskStatusEnum.Active)   { return RedirectToAction(nameof(Index)); }

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTask(
            long id,
            string? type,
            string? name,
            string? description,
            string? pickupAddress,
            string? deliveryAddress,
            decimal? cost,
            string? currency,
            string? removeMedia,
            bool    removeMainPhoto,
            IFormFileCollection photos,
            IFormFile? video)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(name))
            {
                var bad = await taskRepository.Get(new TaskFilter { Id = id });
                ViewBag.Error = "Service type and request title are required.";
                return View(bad);
            }

            var userId = long.Parse(HttpContext.Session.GetString("UserId")!);
            var task   = await taskRepository.Get(new TaskFilter { Id = id, UserId = userId });

            if (task is null)                          { return NotFound(); }
            if (task.Status != TaskStatusEnum.Active) { return RedirectToAction(nameof(Index)); }

            // ── Text fields ───────────────────────────────────────────────────────
            task.Type            = type;
            task.Name            = name.Trim();
            task.Description     = description?.Trim();
            task.PickupAddress   = pickupAddress?.Trim();
            task.DeliveryAddress = deliveryAddress?.Trim();
            task.Cost            = cost;
            task.Currency        = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency;

            // ── Remove existing media ─────────────────────────────────────────────
            if (removeMainPhoto && task.Image is not null)
            {
                DeleteUpload(task.Image);
                task.Image = null;
            }

            var currentMedia = (task.Media ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            foreach (var path in (removeMedia ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                DeleteUpload(path);
                currentMedia.Remove(path);
            }

            // ── Save new photos ───────────────────────────────────────────────────
            if (photos.Count > 0)
            {
                var uploadId  = Guid.NewGuid().ToString("N")[..12];
                var uploadDir = Path.Combine(env.WebRootPath, "uploads", "tasks");
                Directory.CreateDirectory(uploadDir);

                for (int i = 0; i < photos.Count; i++)
                {
                    var photo = photos[i];
                    if (photo.Length == 0) { continue; }

                    var ext      = Path.GetExtension(photo.FileName).ToLowerInvariant();
                    var fileName = $"{uploadId}_p{i}{ext}";
                    var filePath = Path.Combine(uploadDir, fileName);

                    await using var stream = System.IO.File.Create(filePath);
                    await photo.CopyToAsync(stream);

                    var rel = $"/uploads/tasks/{fileName}";

                    if (task.Image is null && i == 0) { task.Image = rel; }
                    else                              { currentMedia.Add(rel); }
                }
            }

            // ── Save new video ────────────────────────────────────────────────────
            if (video is { Length: > 0 })
            {
                // Remove old video entry from media list if present
                var oldVideo = currentMedia.FirstOrDefault(IsVideoPath);
                if (oldVideo is not null) { DeleteUpload(oldVideo); currentMedia.Remove(oldVideo); }

                var uploadId  = Guid.NewGuid().ToString("N")[..12];
                var uploadDir = Path.Combine(env.WebRootPath, "uploads", "tasks");
                Directory.CreateDirectory(uploadDir);

                var ext      = Path.GetExtension(video.FileName).ToLowerInvariant();
                var fileName = $"{uploadId}_v{ext}";
                var filePath = Path.Combine(uploadDir, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await video.CopyToAsync(stream);

                currentMedia.Add($"/uploads/tasks/{fileName}");
            }

            task.Media = currentMedia.Count > 0 ? string.Join(',', currentMedia) : null;

            await taskRepository.Update(task);
            return RedirectToAction(nameof(Index));
        }

        private void DeleteUpload(string relativePath)
        {
            var full = Path.Combine(
                env.WebRootPath,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) { System.IO.File.Delete(full); }
        }

        private static bool IsVideoPath(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".mp4" or ".mov" or ".avi" or ".webm" or ".mkv";
        }

        // ── Users ─────────────────────────────────────────────────────────────────

        public async Task<IActionResult> Users()
        {
            var users = await userRepository.GetAll(new UserFilter());
            ViewBag.TotalCount    = users.Count;
            ViewBag.CustomerCount = users.Count(u => u.Role == UserRoleEnum.Customer);
            ViewBag.CompanyCount  = users.Count(u => u.Role == UserRoleEnum.Company);
            ViewBag.PendingCount  = users.Count(u => u.Status == UserStatusEnum.Pending);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await userRepository.GetAll(new UserFilter());
            var data = users.Select(u => new
            {
                id     = u.Id,
                name   = u.Name,
                email  = u.Email,
                mobile = u.Mobile,
                role   = u.Role,
                status = u.Status
            });
            return Json(new { data });
        }

        public IActionResult Settings() => View();
    }
}
