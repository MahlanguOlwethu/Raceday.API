using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Dtos;
using RaceDay.MVC.Services;
using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class AuthController : BaseController
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (IsLoggedIn) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var client = _apiService.GetClient();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/Auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseString);

                // Store user info in MVC Session
                HttpContext.Session.SetString("UserRole", result.GetProperty("role").GetString() ?? "Unknown");
                HttpContext.Session.SetString("UserName", result.GetProperty("userName").GetString() ?? dto.Email);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid login attempt. Please check your email and password.");
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear MVC session
            HttpContext.Session.Clear();

            // Tell the API to clear its session
            var client = _apiService.GetClient();
            await client.PostAsync("api/Auth/logout", null);

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (IsLoggedIn) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var client = _apiService.GetClient();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/Auth/register", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Login");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Registration failed: {error}");
            return View(dto);
        }
    }
}