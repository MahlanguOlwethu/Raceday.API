using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class CategoriesController : BaseController
    {
        private readonly ApiService _apiService;

        public CategoriesController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Categories/Manage/5
        public async Task<IActionResult> Manage(int eventId)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");

            var client = _apiService.GetClient();

            // 1. Fetch the categories for this event from the API
            var response = await client.GetAsync($"api/events/{eventId}/categories");
            var categories = new List<CategoryViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(json, options);
            }

            // Pass the eventId to the View so the "Add Category" form knows where to submit
            ViewBag.EventId = eventId;
            return View(categories);
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int eventId, CreateCategoryViewModel model)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in the category details correctly.";
                return RedirectToAction("Manage", new { eventId = eventId });
            }

            var client = _apiService.GetClient();

            // Serialize the ViewModel to JSON for the API
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"api/events/{eventId}/categories", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Category added successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add category. A category with this name might already exist.";
            }

            // Refresh the Manage page to show the new category
            return RedirectToAction("Manage", new { eventId = eventId });
        }
    }
}