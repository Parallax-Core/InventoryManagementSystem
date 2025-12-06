using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace InventoryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoDbService _mongoDbService;

        public AccountController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // *** DATA TRIMMING ***
            if (username != null) username = username.Trim();

            var user = await _mongoDbService.Users.Find(u => u.Username == username).FirstOrDefaultAsync();

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.NameIdentifier, user.Username)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }

            ViewData["ErrorMessage"] = "Invalid username or password";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Register model)
        {
            // *** DATA TRIMMING ***
            if (model.FirstName != null) model.FirstName = model.FirstName.Trim();
            if (model.LastName != null) model.LastName = model.LastName.Trim();
            if (model.Username != null) model.Username = model.Username.Trim();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _mongoDbService.Users.Find(u => u.Username == model.Username).FirstOrDefaultAsync();
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Username", "Username already exists.");
                        return View(model);
                    }

                    var user = new User
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Username = model.Username,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
                    };

                    await _mongoDbService.Users.InsertOneAsync(user);

                    return RedirectToAction("Login", "Account");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the account. Please try again.");
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    return View(model);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}