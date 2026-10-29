using HelpdeskSentiment.Authentication;
using HelpdeskSentiment.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskSentiment.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IUserStore _users;

        public AccountController(IUserStore users)
        {
            _users = users;
        }

        public IActionResult Login(string? returnUrl)
        {
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
