using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApiClient _api;

    public ProfileController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var profile = await _api.GetAsync<ProfileDto>("api/profile/me");
        return View(ToViewModel(profile));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        var isOrganizer = User.IsInRole("Organizer");

        if (isOrganizer && string.IsNullOrWhiteSpace(model.FullName))
            ModelState.AddModelError(nameof(model.FullName), "Enter your full name.");
        if (!isOrganizer && string.IsNullOrWhiteSpace(model.FirstName))
            ModelState.AddModelError(nameof(model.FirstName), "Enter your first name.");
        if (!isOrganizer && string.IsNullOrWhiteSpace(model.LastName))
            ModelState.AddModelError(nameof(model.LastName), "Enter your last name.");

        if (!ModelState.IsValid)
        {
            // Keep what the user typed, but reload the read-only parts
            var current = await _api.GetAsync<ProfileDto>("api/profile/me");
            model.Role = current.Role;
            model.Email = current.Email;
            model.ProfilePictureUrl = current.ProfilePictureUrl;
            return View("Index", model);
        }

        var result = await _api.SendAsync(HttpMethod.Put, "api/profile/me", new
        {
            fullName = model.FullName,
            firstName = model.FirstName,
            lastName = model.LastName,
            phoneNumber = model.PhoneNumber,
            gender = model.Gender,
            dateOfBirth = model.DateOfBirth
        });

        if (result.Ok) TempData["Success"] = "Your details were saved.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Participant")]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            TempData["Error"] = "Choose an image first.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadAsync("api/profile/me/picture", picture);

        if (result.Ok) TempData["Success"] = "Your profile picture was updated.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(ProfileDto p) => new()
    {
        Role = p.Role,
        Email = p.Email,
        ProfilePictureUrl = p.ProfilePictureUrl,
        FullName = p.FullName,
        FirstName = p.FirstName,
        LastName = p.LastName,
        PhoneNumber = p.Role == "Organizer" ? p.ContactNumber : p.Phone,
        Gender = p.Gender,
        DateOfBirth = p.DateOfBirth
    };
}
