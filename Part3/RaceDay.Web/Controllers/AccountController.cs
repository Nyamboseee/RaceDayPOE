using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        if (expired)
            ViewData["Info"] = "Your session ended. Please log in again.";

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        ApiResult result;
        try
        {
            result = await _api.SendAsync(HttpMethod.Post, "api/auth/login",
                new { email = model.Email, password = model.Password }, throwWhenUnauthorized: false);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "We cannot reach RaceDay right now. Please try again in a moment.");
            return View(model);
        }

        if (result.Status == System.Net.HttpStatusCode.Unauthorized)
        {
            ModelState.AddModelError(string.Empty, "That email or password is not correct.");
            return View(model);
        }

        var user = ApiClient.Parse<LoginResponseDto>(result);
        if (!result.Ok || user == null || string.IsNullOrEmpty(result.SessionCookie))
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        // Keep the API's session cookie on the server, and sign the user in here too
        HttpContext.Session.SetString(ApiClient.SessionKey, result.SessionCookie);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role != "Organizer" && model.Role != "Participant")
            ModelState.AddModelError(nameof(model.Role), "Choose Organizer or Participant.");

        if (!ModelState.IsValid) return View(model);

        var isParticipant = model.Role == "Participant";
        var result = await _api.SendAsync(HttpMethod.Post, "api/auth/register", new
        {
            role = model.Role,
            firstName = model.FirstName,
            lastName = model.LastName,
            email = model.Email,
            password = model.Password,
            phoneNumber = model.PhoneNumber,
            gender = isParticipant ? model.Gender : null,
            dateOfBirth = isParticipant ? model.DateOfBirth : null
        }, throwWhenUnauthorized: false);

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty,
                result.Status == System.Net.HttpStatusCode.Conflict
                    ? "An account with that email already exists. Try logging in instead."
                    : result.Error);
            return View(model);
        }

        TempData["Success"] = "Your account is ready. Log in to get started.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _api.SendAsync(HttpMethod.Post, "api/auth/logout", throwWhenUnauthorized: false);
        }
        catch (HttpRequestException)
        {
            // The local session is ended either way
        }

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => RedirectToAction("Problem", "Home", new { kind = "forbidden" });
}
