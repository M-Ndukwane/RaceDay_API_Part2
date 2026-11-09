using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {

        // Display Organiser Dashboard
        // Redirect Organiser Dashboard to Manage Events
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            return RedirectToAction("ManageEvents");
        }

        private readonly IHttpClientFactory _httpClientFactory;

        public OrganiserController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        // Display organiser events and dashboard statistics
        public async Task<IActionResult> ManageEvents()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var events = new List<EventViewModel>();
            bool eventsLoaded = false;

            try
            {
                var response = await client.GetAsync("api/Events/my-events");
                if (response.IsSuccessStatusCode)
                {
                    events = await response.Content
                        .ReadFromJsonAsync<List<EventViewModel>>()
                        ?? new List<EventViewModel>();
                    eventsLoaded = true;
                }
                else
                {
                    ViewBag.ErrorMessage = "Unable to load events.";
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Unable to connect to the RaceDay API.";
            }

            ViewBag.TotalEvents = events.Count;
            ViewBag.UpcomingEvents = events.Count(
                e => e.EventDate.Date >= DateTime.Today);
            ViewBag.TotalEnrolled = null;

            if (eventsLoaded)
            {
                try
                {
                    var response = await client.GetAsync("api/Enrollments");
                    if (response.IsSuccessStatusCode)
                    {
                        var enrollments = await response.Content
                            .ReadFromJsonAsync<List<MyEnrollmentViewModel>>()
                            ?? new List<MyEnrollmentViewModel>();

                        var eventIds = events.Select(e => e.EventsID).ToHashSet();
                        ViewBag.TotalEnrolled = enrollments.Count(
                            e => eventIds.Contains(e.EventsID));
                    }
                }
                catch (HttpRequestException)
                {
                    ViewBag.TotalEnrolled = null;
                }
            }

            return View(events);
        }

        // Display all categories
        public async Task<IActionResult> ManageCategories()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync("api/Categories");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load categories.";
                return View(new List<CategoryViewModel>());
            }

            var categories = await response.Content
                .ReadFromJsonAsync<List<CategoryViewModel>>();

            return View(categories ?? new List<CategoryViewModel>());
        }

        // Display Create Category form
        [HttpGet]
        public async Task<IActionResult> CreateCategory()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var model = new ManageCategoryViewModel();

            await LoadEvents(model);

            return View(model);
        }

        // Load existing events for the dropdown
        private async Task LoadEvents(ManageCategoryViewModel model)
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.GetAsync("api/Events");

            if (response.IsSuccessStatusCode)
            {
                model.Events =
                    await response.Content.ReadFromJsonAsync<List<EventViewModel>>()
                    ?? new List<EventViewModel>();
            }
        }

        // Submit Create Category form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(ManageCategoryViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await LoadEvents(model);
                return View(model);
            }

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var request = new
            {
                model.EventsID,
                model.CategoryName,
                model.EntryFee,
                model.MaxParticipants
            };

            var response = await client.PostAsJsonAsync(
                "api/Categories", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category created successfully!";
                return RedirectToAction("ManageCategories");
            }

            var error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "", "Unable to create category. " + error);

            await LoadEvents(model);

            return View(model);
        }

        // Display Edit Category form
        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync($"api/Categories/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("ManageCategories");

            var category = await response.Content
                .ReadFromJsonAsync<ManageCategoryViewModel>();

            if (category == null)
                return RedirectToAction("ManageCategories");

            category.CategoriesID = id;

            await LoadEvents(category);

            return View(category);
        }


        // Submit edited category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(
            int id, ManageCategoryViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await LoadEvents(model);
                return View(model);
            }

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var request = new
            {
                model.EventsID,
                model.CategoryName,
                model.EntryFee,
                model.MaxParticipants
            };

            var response = await client.PutAsJsonAsync(
                $"api/Categories/{id}", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category updated successfully!";
                return RedirectToAction("ManageCategories");
            }

            var error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "", "Unable to update category. " + error);

            await LoadEvents(model);

            return View(model);
        }


        // Display Delete Category confirmation
        [HttpGet]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync($"api/Categories/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("ManageCategories");

            var category = await response.Content
                .ReadFromJsonAsync<ManageCategoryViewModel>();

            if (category == null)
                return RedirectToAction("ManageCategories");

            return View(category);
        }

        // Confirm and delete category
        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.DeleteAsync(
                $"api/Categories/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category deleted successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    "Unable to delete category. " + error;
            }

            return RedirectToAction("ManageCategories");
        }

        // Display all venues
        public async Task<IActionResult> ManageVenues()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync("api/Venues");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load venues.";
                return View(new List<VenueViewModel>());
            }

            var venues = await response.Content
                .ReadFromJsonAsync<List<VenueViewModel>>();

            return View(venues ?? new List<VenueViewModel>());
        }

        // Display Create Venue form
        [HttpGet]
        public IActionResult CreateVenue()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            return View(new ManageVenueViewModel());
        }

        // Submit Create Venue form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(ManageVenueViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var request = new
            {
                model.VenueName,
                model.AddressLine,
                model.City,
                model.PostalCode,
                model.Province
            };

            var response = await client.PostAsJsonAsync(
                "api/Venues", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Venue created successfully!";
                return RedirectToAction("ManageVenues");
            }

            var error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "", "Unable to create venue. " + error);

            return View(model);
        }

        // Display Edit Venue form
        [HttpGet]
        public async Task<IActionResult> EditVenue(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"api/Venues/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("ManageVenues");

            var venue = await response.Content.ReadFromJsonAsync<VenueViewModel>();

            if (venue == null)
                return RedirectToAction("ManageVenues");

            var model = new ManageVenueViewModel
            {
                VenuesID = venue.VenuesID,
                VenueName = venue.VenueName,
                AddressLine = venue.AddressLine,
                City = venue.City,
                PostalCode = venue.PostalCode,
                Province = venue.Province
            };

            return View(model);
        }

        // Submit Edit Venue form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(int id, ManageVenueViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("AccessToken");
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                model.VenueName,
                model.AddressLine,
                model.City,
                model.PostalCode,
                model.Province
            };

            var response = await client.PutAsJsonAsync($"api/Venues/{id}", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Venue updated successfully!";
                return RedirectToAction("ManageVenues");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Unable to update venue. " + error);

            return View(model);
        }

        // Display Delete Venue confirmation page
        [HttpGet]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"api/Venues/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("ManageVenues");

            var venue = await response.Content.ReadFromJsonAsync<VenueViewModel>();

            if (venue == null)
                return RedirectToAction("ManageVenues");

            return View(venue);
        }

        // Submit Delete Venue request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVenueConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"api/Venues/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Venue deleted successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Unable to delete venue. " + error;
            }

            return RedirectToAction("ManageVenues");
        }


        // Display all recorded results
        public async Task<IActionResult> ManageResults()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync("api/Results");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load results.";

                return View(new List<ResultViewModel>());
            }

            var results = await response.Content
                .ReadFromJsonAsync<List<ResultViewModel>>();

            return View(results ?? new List<ResultViewModel>());
        }


        // Display Create Result form
        [HttpGet]
        public async Task<IActionResult> CreateResult()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var model = new CreateResultViewModel();

            return View(model);
        }


        // Submit Create Result form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResult(CreateResultViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.PostAsJsonAsync("api/Results", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result recorded successfully!";
                return RedirectToAction("ManageResults");
            }

            var error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError("", "Unable to record result. " + error);

            return View(model);
        }

        // Display Edit Result form
        [HttpGet]
        public async Task<IActionResult> EditResult(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"api/Results/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("ManageResults");

            var result = await response.Content.ReadFromJsonAsync<ResultViewModel>();

            if (result == null)
                return RedirectToAction("ManageResults");

            var model = new CreateResultViewModel
            {
                EnrollmentsID = result.EnrollmentsID,
                Position = result.Position,
                FinishTimeSeconds = result.FinishTimeSeconds,
                ResultStatus = result.ResultStatus
            };

            ViewBag.ResultsID = id;

            return View(model);
        }

        // Save edited result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResult(int id, CreateResultViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.ResultsID = id;
                return View(model);
            }

            var token = HttpContext.Session.GetString("AccessToken");
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync($"api/Results/{id}", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result updated successfully!";
                return RedirectToAction("ManageResults");
            }

            var error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError("", "Unable to update result. " + error);
            ViewBag.ResultsID = id;

            return View(model);
        }

        // Delete Result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResult(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"api/Results/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Unable to delete result.";
            }

            return RedirectToAction("ManageResults");
        }

        // Display all participant enrollments
        public async Task<IActionResult> ManageEnrollments()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync("api/Enrollments/organiser");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Unable to load enrollments.";
                return View(new List<ManageEnrollmentViewModel>());
            }

            var enrollments = await response.Content
                .ReadFromJsonAsync<List<ManageEnrollmentViewModel>>();

            return View(enrollments ?? new List<ManageEnrollmentViewModel>());
        }


        // Display Create Event form
        [HttpGet]
        public async Task<IActionResult> CreateEvent()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var model = new ManageEventViewModel();

            await LoadVenues(model);

            return View(model);
        }

        // Display Edit Event form
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync($"api/Events/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("ManageEvents");

            var eventData = await response.Content
                .ReadFromJsonAsync<ManageEventViewModel>();

            if (eventData == null)
                return RedirectToAction("ManageEvents");

            eventData.EventsID = id;

            await LoadVenues(eventData);

            return View(eventData);
        }

        // Submit edited event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(
            int id, ManageEventViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await LoadVenues(model);
                return View(model);
            }

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            using var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(model.EventName ?? ""), "EventName");
            formData.Add(new StringContent(model.Description ?? ""), "Description");
            formData.Add(new StringContent(model.EventDate.ToString("O", System.Globalization.CultureInfo.InvariantCulture)), "EventDate");
            formData.Add(new StringContent(model.StartTime.ToString(@"hh\:mm\:ss")), "StartTime");
            formData.Add(new StringContent(model.RegistrationDeadline.ToString("O", System.Globalization.CultureInfo.InvariantCulture)), "RegistrationDeadline");
            formData.Add(new StringContent(model.VenuesID.ToString()), "VenuesID");

            var response = await client.PutAsync($"api/Events/{id}", formData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Event updated successfully!";
                return RedirectToAction("ManageEvents");
            }

            var error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "", "Unable to update event. " + error);

            await LoadVenues(model);

            return View(model);
        }

        // Display Delete Event confirmation
        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync($"api/Events/{id}");

            if (!response.IsSuccessStatusCode)
                return RedirectToAction("ManageEvents");

            var eventData = await response.Content
                .ReadFromJsonAsync<ManageEventViewModel>();

            if (eventData == null)
                return RedirectToAction("ManageEvents");

            return View(eventData);
        }

        // Confirm and delete event
        [HttpPost, ActionName("DeleteEvent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.DeleteAsync($"api/Events/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Event deleted successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    "Unable to delete event. " + error;
            }

            return RedirectToAction("ManageEvents");
        }

        // Submit Create Event form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(ManageEventViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await LoadVenues(model);
                return View(model);
            }

            var token = HttpContext.Session.GetString("AccessToken");

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            using var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(model.EventName ?? ""), "EventName");
            formData.Add(new StringContent(model.Description ?? ""), "Description");
            formData.Add(new StringContent(model.EventDate.ToString("O", System.Globalization.CultureInfo.InvariantCulture)), "EventDate");
            formData.Add(new StringContent(model.StartTime.ToString(@"hh\:mm\:ss")), "StartTime");
            formData.Add(new StringContent(model.RegistrationDeadline.ToString("O", System.Globalization.CultureInfo.InvariantCulture)), "RegistrationDeadline");
            formData.Add(new StringContent(model.VenuesID.ToString()), "VenuesID");

            var response = await client.PostAsync("api/Events", formData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Event created successfully!";
                return RedirectToAction("ManageEvents");
            }

            var error = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(
                "",
                "Unable to create event. " + error);

            await LoadVenues(model);

            return View(model);
        }

        // Load existing venues from the API
        private async Task LoadVenues(ManageEventViewModel model)
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.GetAsync("api/Venues");

            if (response.IsSuccessStatusCode)
            {
                model.Venues =
                    await response.Content.ReadFromJsonAsync<List<VenueViewModel>>()
                    ?? new List<VenueViewModel>();
            }
        }

        // Display the logged-in organiser's profile
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            var userId = HttpContext.Session.GetInt32("UsersID");
            if (string.IsNullOrWhiteSpace(token) || userId == null)
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.GetAsync($"api/Users/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Unable to load your profile.";
                    return RedirectToAction("ManageEvents");
                }

                var profile = await response.Content.ReadFromJsonAsync<ProfileViewModel>();
                if (profile == null)
                {
                    TempData["Error"] = "Unable to load your profile.";
                    return RedirectToAction("ManageEvents");
                }
                return View(profile);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the RaceDay API.";
                return RedirectToAction("ManageEvents");
            }
        }

        // Update organiser's own profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token) || HttpContext.Session.GetInt32("UsersID") == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View("MyProfile", model);

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.PutAsJsonAsync("api/Users/profile", new
                {
                    model.FirstName,
                    model.LastName,
                    model.PhoneNumber
                });
                if (response.IsSuccessStatusCode)
                {
                    HttpContext.Session.SetString("FirstName", model.FirstName);
                    TempData["Success"] = "Your profile was updated successfully.";
                }
                else
                    TempData["Error"] = "Unable to update your profile. Please try again.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the RaceDay API.";
            }
            return RedirectToAction("MyProfile");
        }

        // Upload organiser profile picture to Azure via the API
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile image)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            if (image == null || image.Length == 0)
            {
                TempData["Error"] = "Please select a profile picture.";
                return RedirectToAction("MyProfile");
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (image.Length > 5 * 1024 * 1024 ||
                !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension) ||
                !new[] { "image/jpeg", "image/png", "image/webp" }.Contains(image.ContentType))
            {
                TempData["Error"] = "Please upload a JPG, PNG or WEBP image no larger than 5 MB.";
                return RedirectToAction("MyProfile");
            }

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var formData = new MultipartFormDataContent();
                await using var imageStream = image.OpenReadStream();
                using var fileContent = new StreamContent(imageStream);
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                formData.Add(fileContent, "image", Path.GetFileName(image.FileName));

                using var response = await client.PostAsync("api/Users/profile/photo", formData);
                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                    response.IsSuccessStatusCode
                        ? "Your profile picture was uploaded successfully."
                        : "Unable to upload your profile picture. Please check the image and try again.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the RaceDay API.";
            }
            return RedirectToAction("MyProfile");
        }

        // Serve organiser profile picture to the browser
        [HttpGet]
        public async Task<IActionResult> ProfilePhoto()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return Unauthorized();

            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized();

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.GetAsync("api/Users/profile/photo");
                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != "image/jpeg" && contentType != "image/png" && contentType != "image/webp")
                    return StatusCode(415);

                var bytes = await response.Content.ReadAsByteArrayAsync();
                Response.Headers.CacheControl = "no-store";
                return File(bytes, contentType);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503);
            }
        }

        // Remove organiser profile picture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePhoto()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.DeleteAsync("api/Users/profile/photo");
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "Your profile picture was removed successfully.";
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    TempData["Error"] = "You do not have a profile picture to remove.";
                else
                    TempData["Error"] = "Unable to remove your profile picture. Please try again.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the RaceDay API.";
            }
            return RedirectToAction("MyProfile");
        }

        // Display organiser change-password page
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");
            return View(new ChangePasswordViewModel());
        }

        // Change organiser password using the existing API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.PostAsJsonAsync("api/Auth/change-password", model);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Your password was changed successfully.";
                    return RedirectToAction("ChangePassword");
                }
                TempData["Error"] = "Password change failed. Please check your current password and try again.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the RaceDay API.";
            }
            return View(model);
        }
    }
}
