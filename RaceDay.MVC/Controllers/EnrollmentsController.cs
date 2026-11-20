using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class EnrolmentsController : BaseController
    {
        private readonly ApiService _apiService;

        public EnrolmentsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // --- PARTICIPANT ACTIONS ---

        [HttpPost]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();

            var dto = new { CategoryId = categoryId };
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"api/events/{eventId}/enrolments", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Successfully enrolled in the event!";
                return RedirectToAction("MyEvents");
            }

            TempData["ErrorMessage"] = $"Enrolment failed. You might already be enrolled in this event.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        public async Task<IActionResult> MyEvents()
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();
            var response = await client.GetAsync("api/enrolments/mine");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var enrolments = JsonSerializer.Deserialize<List<JsonElement>>(json);
                return View(enrolments);
            }

            return View(new List<JsonElement>());
        }

        // --- ORGANISER ACTIONS ---

        // GET: Enrolments/ManageEnrolments/5
        public async Task<IActionResult> ManageEnrolments(int eventId)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");

            var client = _apiService.GetClient();
            var response = await client.GetAsync($"api/events/{eventId}/enrolments");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var enrolments = JsonSerializer.Deserialize<List<JsonElement>>(json);
                ViewBag.EventId = eventId;
                return View(enrolments);
            }

            TempData["ErrorMessage"] = "Failed to load enrolments.";
            return RedirectToAction("MyEvents", "Events");
        }

        // POST: Enrolments/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, int eventId)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");

            var client = _apiService.GetClient();

            var dto = new { Status = status };
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/enrolments/{id}/status", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = $"Enrolment marked as {status}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update enrolment status.";
            }

            return RedirectToAction("ManageEnrolments", new { eventId = eventId });
        }
    }
}