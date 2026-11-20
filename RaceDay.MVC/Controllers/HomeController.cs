using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Diagnostics;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApiService _apiService;

        public HomeController(ILogger<HomeController> logger, ApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        // Organisers see their dashboard; Participants see upcoming events
        public async Task<IActionResult> Index()
        {
            if (CurrentUserRole == "Participant") return RedirectToAction("Index", "Events");

            if (CurrentUserRole == "Organiser")
            {
                var response = await _apiService.GetClient().GetAsync("api/events/mine");
                var events = new List<EventViewModel>();
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    events = JsonSerializer.Deserialize<List<EventViewModel>>(json, options) ?? new();
                }
                return View("Dashboard", events);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
