using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.MVC.Models;

namespace RaceDay.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Display login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Process login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.PostAsJsonAsync(
                "api/Auth/login", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var result = await response.Content
                .ReadFromJsonAsync<LoginResponse>();

            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                ModelState.AddModelError("", "Unable to process login.");
                return View(model);
            }

            // Store user information in the session
            HttpContext.Session.SetString(
                "AccessToken", result.AccessToken);

            HttpContext.Session.SetString(
                "TokenType", result.TokenType);

            HttpContext.Session.SetInt32(
                "UsersID", result.User.UsersID);

            HttpContext.Session.SetString(
                "FirstName", result.User.FirstName);

            HttpContext.Session.SetString(
                "Email", result.User.Email);

            HttpContext.Session.SetString(
                "Role", result.User.Role);

            // Redirect according to user role
            if (result.User.Role == "Organiser")
            {
                return RedirectToAction("Index", "Organiser");
            }

            return RedirectToAction("Index", "Participant");
        }

        // Display registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Process registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.PostAsJsonAsync(
                "api/Auth/register", model);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    "", "Registration failed. Please check your information.");

                return View(model);
            }

            TempData["Success"] =
                "Registration successful. You can now log in.";

            return RedirectToAction("Login");
        }

        // Log out
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}
