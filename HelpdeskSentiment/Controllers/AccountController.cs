using HelpdeskSentiment.Authentication;
using HelpdeskSentiment.Data;
using HelpdeskSentiment.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpdeskSentiment.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IUserStore _users;
        private readonly HelpdeskDbContext _db;
        private readonly IConfiguration _configuration;

        public AccountController(IUserStore users, HelpdeskDbContext db, IConfiguration configuration)
        {
            _users = users;
            _db = db;
            _configuration = configuration;
        }

        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Calls");
            }

            ViewData["Title"] = "Sign in";
            ViewBag.ClerkPublishableKey = _configuration["Clerk:PublishableKey"] ?? "pk_test_Y2xlcmsuaGVscGRlc2suc2VudGltZW50LmRlbW8k";
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["Title"] = "Sign in";
            ViewBag.ClerkPublishableKey = _configuration["Clerk:PublishableKey"] ?? "pk_test_Y2xlcmsuaGVscGRlc2suc2VudGltZW50LmRlbW8k";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AppUser? user = _users.Validate(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                _users.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme));

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Calls");
        }

        public IActionResult Register(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Calls");
            }

            ViewData["Title"] = "Create Account";
            ViewBag.ClerkPublishableKey = _configuration["Clerk:PublishableKey"] ?? "pk_test_Y2xlcmsuaGVscGRlc2suc2VudGltZW50LmRlbW8k";
            return View(new RegisterViewModel { ReturnUrl = returnUrl, Role = Roles.Agent });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["Title"] = "Create Account";
            ViewBag.ClerkPublishableKey = _configuration["Clerk:PublishableKey"] ?? "pk_test_Y2xlcmsuaGVscGRlc2suc2VudGltZW50LmRlbW8k";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string trimmedUsername = model.Username.Trim();
            string trimmedFullName = model.FullName.Trim();
            string trimmedEmail = model.Email.Trim().ToLowerInvariant();

            // Edge case: check if username is already registered
            if (_users.UserExists(trimmedUsername))
            {
                ModelState.AddModelError("Username", "This username is already taken. Please choose another.");
                return View(model);
            }

            // Edge case: enforce role to permitted values
            string selectedRole = model.Role == Roles.Admin ? Roles.Admin : Roles.Agent;

            // If registering as Agent, ensure email is not already taken in the Agents database table
            if (selectedRole == Roles.Agent)
            {
                bool emailExists = await _db.Agents.AnyAsync(a => a.Email.ToLower() == trimmedEmail);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "An agent profile with this email address already exists.");
                    return View(model);
                }
            }

            // Create user account in store
            var newUser = _users.CreateUser(
                username: trimmedUsername,
                password: model.Password,
                displayName: trimmedFullName,
                role: selectedRole);

            // Synchronize with database Agents table so the user is selectable in call logging
            if (selectedRole == Roles.Agent)
            {
                var newAgent = new Agent
                {
                    Name = trimmedFullName,
                    Email = trimmedEmail
                };
                _db.Agents.Add(newAgent);
                await _db.SaveChangesAsync();
            }

            // Immediately sign the user in with a secure session cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                _users.CreatePrincipal(newUser, CookieAuthenticationDefaults.AuthenticationScheme));

            TempData["Message"] = $"Welcome to Helpdesk Support Portal, {newUser.DisplayName}! Your account has been created successfully.";
            TempData["MessageType"] = "success";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Calls");
        }

        /// <summary>
        /// Clerk Multi-Platform Authentication Callback (Google, GitHub, Microsoft, Clerk).
        /// Bridges Clerk SSO authentication into the ASP.NET Core Cookie authentication pipeline.
        /// </summary>
        [HttpPost]
        [Route("Account/ClerkCallback")]
        public async Task<IActionResult> ClerkCallback([FromBody] ClerkLoginRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return Json(new ClerkAuthResponse
                {
                    Success = false,
                    Message = "Valid email address is required for Clerk authentication."
                });
            }

            string email = request.Email.Trim().ToLowerInvariant();
            string displayName = !string.IsNullOrWhiteSpace(request.FullName)
                ? request.FullName.Trim()
                : email.Split('@')[0];
            string username = email.Split('@')[0];

            // 1. Check if user already exists in UserStore by email/username
            var existingUser = _users.GetAll().FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));

            AppUser user;
            if (existingUser != null)
            {
                user = existingUser;
            }
            else
            {
                // Ensure unique username
                string candidateUsername = username;
                int suffix = 1;
                while (_users.UserExists(candidateUsername))
                {
                    candidateUsername = $"{username}{suffix++}";
                }

                user = _users.CreateUser(
                    username: candidateUsername,
                    password: Guid.NewGuid().ToString("N"),
                    displayName: displayName,
                    role: Roles.Agent);
            }

            // 2. Synchronize with database Agents table if not exists
            if (user.Role == Roles.Agent)
            {
                var existingAgent = await _db.Agents.FirstOrDefaultAsync(a => a.Email.ToLower() == email);
                if (existingAgent == null)
                {
                    _db.Agents.Add(new Agent
                    {
                        Name = displayName,
                        Email = email
                    });
                    await _db.SaveChangesAsync();
                }
            }

            // 3. Issue ASP.NET Core cookie session
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                _users.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme));

            string provider = string.IsNullOrWhiteSpace(request.Provider) ? "Clerk" : request.Provider;
            TempData["Message"] = $"Successfully authenticated with {provider} as {user.DisplayName}!";
            TempData["MessageType"] = "success";

            string redirectUrl = !string.IsNullOrEmpty(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl)
                ? request.ReturnUrl
                : Url.Action("Index", "Calls") ?? "/Calls";

            return Json(new ClerkAuthResponse
            {
                Success = true,
                RedirectUrl = redirectUrl,
                Username = user.Username,
                Message = "Authentication successful"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            ViewData["Title"] = "Access denied";
            return View();
        }
    }
}
