using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class ResultsController : BaseController
    {
        private readonly ApiService _apiService;

        public ResultsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // --- PARTICIPANT ACTION ---
        public async Task<IActionResult> MyResults()
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();
            var response = await client.GetAsync("api/results/mine");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<JsonElement>>(json);
                return View(results);
            }

            return View(new List<JsonElement>());
        }

        // --- ORGANISER ACTIONS ---

        // GET: Results/ManageResults/5
        public async Task<IActionResult> ManageResults(int eventId)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");

            var client = _apiService.GetClient();

            // Fetch enrolments for this event to find Confirmed participants
            var enrolmentsResponse = await client.GetAsync($"api/events/{eventId}/enrolments");
            var enrolments = new List<ParticipantResultViewModel>();

            if (enrolmentsResponse.IsSuccessStatusCode)
            {
                var enrolmentsJson = await enrolmentsResponse.Content.ReadAsStringAsync();
                var enrolmentsData = JsonSerializer.Deserialize<List<JsonElement>>(enrolmentsJson);

                foreach (var item in enrolmentsData ?? new List<JsonElement>())
                {
                    // FIXED: Added '?? ""' to resolve the nullable warning
                    var status = item.GetProperty("status").GetString() ?? "";
                    if (status == "Confirmed")
                    {
                        enrolments.Add(new ParticipantResultViewModel
                        {
                            EnrolmentId = item.GetProperty("enrolmentId").GetInt32(),
                            ParticipantName = item.GetProperty("participantName").GetString() ?? "",
                            CategoryName = item.GetProperty("categoryName").GetString() ?? "",
                            Status = status
                        });
                    }
                }
            }

            ViewBag.EventId = eventId;
            return View(enrolments);
        }

        // POST: Results/CaptureResult
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(int eventId, int enrolmentId, string finishTimeString, int finishPosition)
        {
            if (CurrentUserRole != "Organiser") return RedirectToAction("Index", "Home");

            // Ensure the time string is in HH:mm:ss format for the API's TimeSpan parser
            if (finishTimeString.Length == 5)
            {
                finishTimeString += ":00";
            }

            var client = _apiService.GetClient();

            // The API's CreateResultDto expects EnrolmentId, FinishTime (as TimeSpan string), and FinishPosition
            var dto = new
            {
                EnrolmentId = enrolmentId,
                FinishTime = finishTimeString,
                FinishPosition = finishPosition
            };

            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"api/events/{eventId}/results", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Result captured successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to capture result. It might already exist.";
            }

            return RedirectToAction("ManageResults", new { eventId = eventId });
        }
    }
}