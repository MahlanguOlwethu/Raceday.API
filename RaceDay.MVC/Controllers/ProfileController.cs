using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class ProfileController : BaseController
    {
        private readonly ApiService _apiService;

        public ProfileController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();
            var response = await client.GetAsync("api/users/profile");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var user = JsonSerializer.Deserialize<ProfileViewModel>(json, options);
                return View(user);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(ProfileViewModel model)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            if (model.ProfilePictureFile == null || model.ProfilePictureFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image to upload.";
                return RedirectToAction("Index");
            }

            var client = _apiService.GetClient();

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(model.ProfilePictureFile.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ProfilePictureFile.ContentType);

            content.Add(fileContent, "file", model.ProfilePictureFile.FileName);

            var response = await client.PostAsync("api/users/profile-picture", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Profile picture updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to upload profile picture.";
            }

            return RedirectToAction("Index");
        }
    }
}