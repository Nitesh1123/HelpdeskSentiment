using System.Net;
using System.Security.Claims;
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

        public AccountController(IUserStore users, HelpdeskDbContext db)
        {
            _users = users;
            _db = db;
        }

        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Calls");
            }

            ViewData["Title"] = "Sign in";
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["Title"] = "Sign in";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AppUser? user = _users.Validate(model.Username, model.Password);
            if (user == null)
            {
                await RecordAsync(LoginEvent.SignInFailed, model.Username.Trim(), null, null);
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                _users.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme));
            await RecordAsync(LoginEvent.SignIn, user.Username, user.DisplayName, user.Role);

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
            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["Title"] = "Create Account";

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

            // Security: Registration strictly creates Support Agent accounts (prevents privilege escalation)
            const string assignedRole = Roles.Agent;

            // Ensure email is not already registered in the Agents database table
            bool emailExists = await _db.Agents.AnyAsync(a => a.Email.ToLower() == trimmedEmail);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "An agent profile with this email address already exists.");
                return View(model);
            }

            // Create user account in store as Agent
            var newUser = _users.CreateUser(
                username: trimmedUsername,
                password: model.Password,
                displayName: trimmedFullName,
                role: assignedRole);

            // Synchronize with database Agents table so the user is selectable in call logging
            var newAgent = new Agent
            {
                Name = trimmedFullName,
                Email = trimmedEmail
            };
            _db.Agents.Add(newAgent);
            await _db.SaveChangesAsync();

            // Immediately sign the user in with a secure session cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                _users.CreatePrincipal(newUser, CookieAuthenticationDefaults.AuthenticationScheme));
            await RecordAsync(LoginEvent.SignUp, newUser.Username, newUser.DisplayName, newUser.Role);

            TempData["Message"] = $"Welcome to Helpdesk Support Portal, {newUser.DisplayName}! Your account has been created successfully.";
            TempData["MessageType"] = "success";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Calls");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await RecordAsync(LoginEvent.SignOut,
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
                    User.Identity.Name,
                    User.FindFirstValue(ClaimTypes.Role));
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            ViewData["Title"] = "Access denied";
            return View();
        }

        // Saves one row to the LoginActivity table. Passwords are never stored.
        private async Task RecordAsync(LoginEvent loginEvent, string username, string? displayName, string? role)
        {
            _db.LoginActivities.Add(new LoginActivity
            {
                Event = loginEvent,
                Username = Truncate(username, 50) ?? "",
                DisplayName = Truncate(displayName, 100),
                Role = role,
                IpAddress = Truncate(GetClientIp(), 45),
                UserAgent = Truncate(Request.Headers.UserAgent.ToString(), 200),
                OccurredAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        // Behind the Cloudflare tunnel every request comes from this PC, so use the visitor's IP that Cloudflare forwards.
        private string? GetClientIp()
        {
            IPAddress? remote = HttpContext.Connection.RemoteIpAddress;
            if (remote != null && IPAddress.IsLoopback(remote) && Request.Headers.TryGetValue("CF-Connecting-IP", out var forwarded))
            {
                return forwarded.ToString();
            }

            return remote?.ToString();
        }

        private static string? Truncate(string? value, int maxLength) =>
            value == null || value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
