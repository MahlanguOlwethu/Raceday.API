using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class EventsController : BaseController
    {
        private readonly ApiService _apiService;

        public EventsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");
            var client = _apiService.GetClient();
            var response = await client.GetAsync("api/events");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var events = JsonSerializer.Deserialize<List<EventViewModel>>(json, options);
                return View(events);
            }
            return View(new List<EventViewModel>());
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");
            var client = _apiService.GetClient();
            var response = await client.GetAsync($"api/events/{id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var eventDetails = JsonSerializer.Deserialize<EventDetailsViewModel>(json, options);
                return View(eventDetails);
            }
            return NotFound();
        }

        public async Task<IActionResult> MyEvents()
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");
            var client = _apiService.GetClient();
            var response = await client.GetAsync("api/events/mine");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var events = JsonSerializer.Deserialize<List<EventViewModel>>(json, options);
                return View(events);
            }
            return View(new List<EventViewModel>());
        }

        public IActionResult Create()
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEventViewModel model)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");
            if (!ModelState.IsValid) return View(model);

            var client = _apiService.GetClient();

            // We must use MultipartFormDataContent to send files to the API
            using var content = BuildEventContent(model);

            var response = await client.PostAsync("api/events", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction("MyEvents");
            }

            ModelState.AddModelError("", "Failed to create event. Please try again.");
            return View(model);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");

            var client = _apiService.GetClient();
            var response = await client.GetAsync($"api/events/{id}");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "That event could not be loaded.";
                return RedirectToAction("MyEvents");
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var ev = JsonSerializer.Deserialize<EventDetailsViewModel>(json, options);
            if (ev == null) return RedirectToAction("MyEvents");

            return View(new EditEventViewModel
            {
                EventId = ev.EventId,
                Name = ev.Name,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                DistanceKm = ev.DistanceKm,
                EventType = ev.EventType,
                ExistingBannerUrl = ev.BannerImageUrl
            });
        }

        // POST: Events/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditEventViewModel model)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");
            if (!ModelState.IsValid) return View(model);

            var client = _apiService.GetClient();
            using var content = BuildEventContent(model);

            var response = await client.PutAsync($"api/events/{model.EventId}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Event updated successfully!";
                return RedirectToAction("MyEvents");
            }

            ModelState.AddModelError("", "Failed to update event. Please try again.");
            return View(model);
        }

        // POST: Events/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");

            var client = _apiService.GetClient();
            var response = await client.DeleteAsync($"api/events/{id}");

            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "Event deleted.";
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                TempData["ErrorMessage"] = "This event can't be deleted because participants have already enrolled.";
            else
                TempData["ErrorMessage"] = "The event could not be deleted.";

            return RedirectToAction("MyEvents");
        }

        private static MultipartFormDataContent BuildEventContent(CreateEventViewModel model)
        {
            var content = new MultipartFormDataContent();

            content.Add(new StringContent(model.Name), "Name");
            content.Add(new StringContent(model.Description), "Description");
            content.Add(new StringContent(model.EventDate.ToString("yyyy-MM-ddTHH:mm:ss")), "EventDate");
            content.Add(new StringContent(model.Location), "Location");
            content.Add(new StringContent(model.DistanceKm.ToString(System.Globalization.CultureInfo.InvariantCulture)), "DistanceKm");
            content.Add(new StringContent(model.EventType), "EventType");

            if (model.BannerImage != null && model.BannerImage.Length > 0)
            {
                var fileContent = new StreamContent(model.BannerImage.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.BannerImage.ContentType);
                content.Add(fileContent, "BannerImage", model.BannerImage.FileName);
            }

            return content;
        }
    }
}