using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ParticipantController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display Participant Dashboard
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            if (role != "Participant")
                return RedirectToAction("Index", "Organiser");

            ViewBag.FirstName =
                HttpContext.Session.GetString("FirstName");

            var token =
                HttpContext.Session.GetString("AccessToken");

            var client =
                _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var dashboard = new ParticipantDashboardViewModel();

            // Retrieve events
            var eventsResponse =
                await client.GetAsync("api/Events");

            if (eventsResponse.IsSuccessStatusCode)
            {
                dashboard.Events =
                    await eventsResponse.Content
                    .ReadFromJsonAsync<List<EventViewModel>>()
                    ?? new List<EventViewModel>();
            }

            // Retrieve categories
            var categoriesResponse =
                await client.GetAsync("api/Categories");

            if (categoriesResponse.IsSuccessStatusCode)
            {
                dashboard.Categories =
                    await categoriesResponse.Content
                    .ReadFromJsonAsync<List<CategoryViewModel>>()
                    ?? new List<CategoryViewModel>();
            }

            return View(dashboard);
        }

        // Process event enrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(
            CreateEnrollmentViewModel model)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Participant")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Please check your enrollment information.";

                return RedirectToAction("Index");
            }

            var token =
                HttpContext.Session.GetString("AccessToken");

            var client =
                _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync(
                "api/Enrollments", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "You have successfully enrolled in this event!";
            }
            else
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    "Enrollment failed: " + error;
            }

            return RedirectToAction("Index");
        }
        // Display participant's enrollments
        public async Task<IActionResult> MyEnrollments()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Retrieve enrollments
            var enrollmentResponse =
                await client.GetAsync("api/Enrollments");

            if (!enrollmentResponse.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage =
                    "Unable to retrieve your enrollments.";

                return View(new List<MyEnrollmentViewModel>());
            }

            var enrollments = await enrollmentResponse.Content
                .ReadFromJsonAsync<List<MyEnrollmentViewModel>>()
                ?? new List<MyEnrollmentViewModel>();


            // Retrieve events
            var eventResponse = await client.GetAsync("api/Events");

            var events = new List<EventViewModel>();

            if (eventResponse.IsSuccessStatusCode)
            {
                events = await eventResponse.Content
                    .ReadFromJsonAsync<List<EventViewModel>>()
                    ?? new List<EventViewModel>();
            }

            // Retrieve categories
            var categoryResponse = await client.GetAsync("api/Categories");

            var categories = new List<CategoryViewModel>();

            if (categoryResponse.IsSuccessStatusCode)
            {
                categories = await categoryResponse.Content
                    .ReadFromJsonAsync<List<CategoryViewModel>>()
                    ?? new List<CategoryViewModel>();
            }

            // Match IDs with their names
            foreach (var enrollment in enrollments)
            {
                var eventItem = events.FirstOrDefault(
                    e => e.EventsID == enrollment.EventsID);

                var category = categories.FirstOrDefault(
                    c => c.CategoriesID == enrollment.CategoriesID);

                enrollment.EventName =
                    eventItem?.EventName ?? "Unknown Event";

                enrollment.CategoryName =
                    category?.CategoryName ?? "Unknown Category";

                enrollment.EntryFee =
                    category?.EntryFee ?? 0;
            }

            return View(enrollments);
        }


        // Deregister the logged-in participant from an event.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deregister(int enrollmentId)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            var token = HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.DeleteAsync(
                    $"api/Enrollments/{enrollmentId}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "You have successfully deregistered from the event.";
                }
                else if (response.StatusCode ==
                         System.Net.HttpStatusCode.Conflict)
                {
                    TempData["Error"] =
                        "You cannot deregister because a race result " +
                        "has already been recorded or the enrollment " +
                        "has related records.";
                }
                else if (response.StatusCode ==
                         System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] =
                        "Enrollment not found or it does not belong to you.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to deregister from this event. Please try again.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to connect to the RaceDay API.";
            }

            return RedirectToAction("MyEnrollments");
        }


        // Display results belonging to the logged-in participant
        public async Task<IActionResult> MyResults()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync("api/Results/my-results");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load your results.";
                return View(new List<ParticipantResultViewModel>());
            }

            var results = await response.Content
                .ReadFromJsonAsync<List<ResultViewModel>>();

            var participantResults = results?.Select(result =>
                new ParticipantResultViewModel
                {
                    ResultsID = result.ResultsID,
                    Position = result.Position ?? 0,
                    FinishTimeSeconds = result.FinishTimeSeconds,
                    ResultStatus = result.ResultStatus,
                    RecordedAt = result.RecordedAt,
                    EventName = result.EventName
                }).ToList();

            return View(participantResults ?? new List<ParticipantResultViewModel>());
        }
        // Display participant's profile
        public async Task<IActionResult> MyProfile()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var userId = HttpContext.Session.GetInt32("UsersID");

            if (userId == null)
            {
                TempData["Error"] = "Unable to identify your account.";
                return RedirectToAction("Index");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"api/Users/{userId}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Unable to load your profile.";
                return RedirectToAction("Index");
            }

            var profile = await response.Content
                .ReadFromJsonAsync<ProfileViewModel>();

            return View(profile);
        }

        // Update participant's profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UsersID");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View("MyProfile", model);

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var updateDto = new
            {
                model.FirstName,
                model.LastName,
                model.PhoneNumber
            };

            var response = await client.PutAsJsonAsync(
                "api/Users/profile", updateDto);

            if (response.IsSuccessStatusCode)
            {
                HttpContext.Session.SetString("FirstName", model.FirstName);

                TempData["Success"] = "Your profile was updated successfully.";

                return RedirectToAction("MyProfile");
            }

            TempData["Error"] = "Unable to update your profile. Please try again.";

            return RedirectToAction("MyProfile");
        }


        // Upload participant profile picture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePhoto(
            IFormFile image)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            if (image == null || image.Length == 0)
            {
                TempData["Error"] = "Please select a profile picture.";
                return RedirectToAction("MyProfile");
            }

            const long maxFileSize = 5 * 1024 * 1024;

            string extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (image.Length > maxFileSize ||
                !allowedExtensions.Contains(extension))
            {
                TempData["Error"] =
                    "Please upload a JPG, PNG or WEBP image no larger than 5 MB.";

                return RedirectToAction("MyProfile");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var formData = new MultipartFormDataContent();

            await using var imageStream = image.OpenReadStream();

            using var fileContent = new StreamContent(imageStream);

            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    image.ContentType);

            formData.Add(
                fileContent,
                "image",
                Path.GetFileName(image.FileName));

            try
            {
                var response = await client.PostAsync(
                    "api/Users/profile/photo",
                    formData);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Your profile picture was uploaded successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to upload your profile picture. " +
                        "Please check the image and try again.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to connect to the RaceDay API.";
            }

            return RedirectToAction("MyProfile");
        }


        // Display participant profile picture
        [HttpGet]
        public async Task<IActionResult> ProfilePhoto()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return Unauthorized();

            var token = HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized();

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.GetAsync(
                    "api/Users/profile/photo");

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var contentType =
                    response.Content.Headers.ContentType?.MediaType;

                if (contentType != "image/jpeg" &&
                    contentType != "image/png" &&
                    contentType != "image/webp")
                {
                    return StatusCode(415);
                }

                var imageBytes =
                    await response.Content.ReadAsByteArrayAsync();

                Response.Headers.CacheControl = "no-store";

                return File(imageBytes, contentType);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503);
            }
        }


        // Remove participant profile picture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePhoto()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.DeleteAsync(
                    "api/Users/profile/photo");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Your profile picture was removed successfully.";
                }
                else if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] =
                        "You do not have a profile picture to remove.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to remove your profile picture. Please try again.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to connect to the RaceDay API.";
            }

            return RedirectToAction("MyProfile");
        }


        // Display change password page
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            return View(new ChangePasswordViewModel());
        }

        // Process password change
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync(
                "api/Auth/change-password", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Your password was changed successfully.";

                return RedirectToAction("ChangePassword");
            }

            var error = await response.Content.ReadAsStringAsync();

            TempData["Error"] =
                "Password change failed. Please check your current password and try again.";

            return View(model);
        }

    }
}
