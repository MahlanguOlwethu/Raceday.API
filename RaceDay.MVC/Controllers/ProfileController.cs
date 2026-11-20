using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.ViewModels;
using System.Text;
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
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                TempData["ErrorMessage"] = "First name and last name are required.";
                return RedirectToAction("Index");
            }

            var client = _apiService.GetClient();
            var payload = new
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                model.DateOfBirth
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PutAsync("api/users/profile", content);

            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "Profile updated successfully!";
            else
                TempData["ErrorMessage"] = "Your profile could not be updated. Please check your details and try again.";

            return RedirectToAction("Index");
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

            var imageError = ImageRules.Validate(model.ProfilePictureFile);
            if (imageError != null)
            {
                TempData["ErrorMessage"] = imageError;
                return RedirectToAction("Index");
            }

            var client = _apiService.GetClient();

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(model.ProfilePictureFile.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(model.ProfilePictureFile.ContentType) ? "application/octet-stream" : model.ProfilePictureFile.ContentType);

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